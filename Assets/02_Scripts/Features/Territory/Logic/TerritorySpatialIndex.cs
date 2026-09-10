using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritorySpatialIndex
    {
        public const float Epsilon = 0.0001f;

        private static readonly ProfilerMarker RebuildMarker =
            new("TerritorySpatialIndex.Rebuild");
        private static readonly ProfilerMarker QueryMarker =
            new("TerritorySpatialIndex.Contains");
        private static readonly ProfilerMarker SegmentMarker =
            new("TerritorySpatialIndex.SegmentCandidates");

        private readonly Dictionary<Vector2Int, TerritorySpatialChunk> _chunks = new();
        private readonly Dictionary<int, List<TerritorySpatialChunk>> _chunksByRow = new();
        private readonly List<Vector2Int> _traversedChunks = new();
        private readonly HashSet<Vector2Int> _uniqueChunks = new();
        private readonly List<int> _candidateEdges = new();
        private readonly HashSet<int> _uniqueEdges = new();
        private TerritorySpatialEdge[] _edges = Array.Empty<TerritorySpatialEdge>();
        private Vector2 _minimum;
        private Vector2 _maximum;

        public bool IsValid { get; private set; }
        public int EdgeCount => _edges.Length;
        public int ChunkCount => _chunks.Count;
        public int NodeCount { get; private set; }
        public int LastCandidateEdgeCount { get; private set; }
        public long QueryCount { get; private set; }
        public long CandidateEdgeInspectionCount { get; private set; }

        public bool Rebuild(IReadOnlyList<Vector2> vertices)
        {
            using (RebuildMarker.Auto())
            {
                Reset();
                int count = vertices?.Count ?? 0;
                if (count < 3)
                    return false;

                _edges = new TerritorySpatialEdge[count];
                _minimum = vertices[0];
                _maximum = vertices[0];
                var edgeIndicesByChunk = new Dictionary<Vector2Int, List<int>>();

                for (int edgeIndex = 0; edgeIndex < count; edgeIndex++)
                {
                    Vector2 start = vertices[edgeIndex];
                    Vector2 end = vertices[(edgeIndex + 1) % count];
                    if (!IsFinite(start) || !IsFinite(end) ||
                        Vector2.SqrMagnitude(end - start) <= Epsilon * Epsilon)
                    {
                        Reset();
                        return false;
                    }

                    _minimum = Vector2.Min(_minimum, start);
                    _maximum = Vector2.Max(_maximum, start);
                    _edges[edgeIndex] = new TerritorySpatialEdge(edgeIndex, start, end);
                    if (!TerritorySegmentTraversal.TryCollectChunks(
                            start,
                            end,
                            _traversedChunks,
                            _uniqueChunks))
                    {
                        Reset();
                        return false;
                    }

                    for (int chunkOffset = 0; chunkOffset < _traversedChunks.Count; chunkOffset++)
                    {
                        Vector2Int coordinate = _traversedChunks[chunkOffset];
                        if (!edgeIndicesByChunk.TryGetValue(coordinate, out List<int> edgeIndices))
                        {
                            edgeIndices = new List<int>();
                            edgeIndicesByChunk.Add(coordinate, edgeIndices);
                        }

                        edgeIndices.Add(edgeIndex);
                    }
                }

                foreach (KeyValuePair<Vector2Int, List<int>> pair in edgeIndicesByChunk)
                {
                    var chunk = new TerritorySpatialChunk(pair.Key, pair.Value, _edges);
                    _chunks.Add(pair.Key, chunk);
                    NodeCount += chunk.Quadtree.NodeCount;
                    if (!_chunksByRow.TryGetValue(pair.Key.y, out List<TerritorySpatialChunk> row))
                    {
                        row = new List<TerritorySpatialChunk>();
                        _chunksByRow.Add(pair.Key.y, row);
                    }

                    row.Add(chunk);
                }

                foreach (List<TerritorySpatialChunk> row in _chunksByRow.Values)
                    row.Sort((left, right) => left.Coordinate.x.CompareTo(right.Coordinate.x));

                IsValid = true;
                return true;
            }
        }

        public bool Contains(Vector2 point)
        {
            using (QueryMarker.Auto())
            {
                QueryCount++;
                LastCandidateEdgeCount = 0;
                if (!IsValid || !IsFinite(point) ||
                    point.x < _minimum.x - Epsilon || point.x > _maximum.x + Epsilon ||
                    point.y < _minimum.y - Epsilon || point.y > _maximum.y + Epsilon)
                {
                    return false;
                }

                CollectRectangleCandidates(
                    point - Vector2.one * Epsilon,
                    point + Vector2.one * Epsilon,
                    _candidateEdges,
                    _uniqueEdges);
                for (int i = 0; i < _candidateEdges.Count; i++)
                {
                    CandidateEdgeInspectionCount++;
                    TerritorySpatialEdge edge = _edges[_candidateEdges[i]];
                    if (PointOnSegment(point, edge.Start, edge.End))
                    {
                        LastCandidateEdgeCount = _candidateEdges.Count;
                        return true;
                    }
                }

                _candidateEdges.Clear();
                _uniqueEdges.Clear();
                int rowCoordinate = TerritorySegmentTraversal.FromPoint(point).y;
                if (!_chunksByRow.TryGetValue(rowCoordinate, out List<TerritorySpatialChunk> row))
                    return false;

                int firstChunk = FindFirstRayChunk(row, point.x);
                Vector2 rayMinimum = new(point.x, point.y - Epsilon);
                Vector2 rayMaximum = new(_maximum.x + Epsilon, point.y + Epsilon);
                for (int i = firstChunk; i < row.Count; i++)
                    row[i].Quadtree.Query(rayMinimum, rayMaximum, _candidateEdges, _uniqueEdges);

                LastCandidateEdgeCount = _candidateEdges.Count;
                bool inside = false;
                for (int i = 0; i < _candidateEdges.Count; i++)
                {
                    CandidateEdgeInspectionCount++;
                    TerritorySpatialEdge edge = _edges[_candidateEdges[i]];
                    Vector2 start = edge.Start;
                    Vector2 end = edge.End;
                    if ((start.y > point.y) != (end.y > point.y))
                    {
                        float atX = (end.x - start.x) * (point.y - start.y) /
                                    (end.y - start.y) + start.x;
                        if (point.x < atX)
                            inside = !inside;
                    }
                }

                return inside;
            }
        }

        public bool IsPointOnBoundary(Vector2 point)
        {
            if (!IsValid || !IsFinite(point))
                return false;

            CollectRectangleCandidates(
                point - Vector2.one * Epsilon,
                point + Vector2.one * Epsilon,
                _candidateEdges,
                _uniqueEdges);
            LastCandidateEdgeCount = _candidateEdges.Count;
            for (int i = 0; i < _candidateEdges.Count; i++)
            {
                CandidateEdgeInspectionCount++;
                TerritorySpatialEdge edge = _edges[_candidateEdges[i]];
                if (PointOnSegment(point, edge.Start, edge.End))
                    return true;
            }

            return false;
        }

        public bool TryCollectSegmentCandidates(
            Vector2 start,
            Vector2 end,
            List<int> results,
            HashSet<int> unique)
        {
            using (SegmentMarker.Auto())
            {
                if (results == null)
                    throw new ArgumentNullException(nameof(results));
                if (unique == null)
                    throw new ArgumentNullException(nameof(unique));

                results.Clear();
                unique.Clear();
                if (!IsValid || !TerritorySegmentTraversal.TryCollectChunks(
                        start,
                        end,
                        _traversedChunks,
                        _uniqueChunks))
                {
                    return false;
                }

                Vector2 minimum = Vector2.Min(start, end) - Vector2.one * Epsilon;
                Vector2 maximum = Vector2.Max(start, end) + Vector2.one * Epsilon;
                for (int i = 0; i < _traversedChunks.Count; i++)
                {
                    if (_chunks.TryGetValue(_traversedChunks[i], out TerritorySpatialChunk chunk))
                        chunk.Quadtree.Query(minimum, maximum, results, unique);
                }

                LastCandidateEdgeCount = results.Count;
                return true;
            }
        }

        public TerritorySpatialEdge GetEdge(int index) => _edges[index];

        public static bool IsSimplePolygon(IReadOnlyList<Vector2> vertices)
        {
            var index = new TerritorySpatialIndex();
            if (!index.Rebuild(vertices))
                return false;

            var candidates = new List<int>();
            var unique = new HashSet<int>();
            int count = vertices.Count;
            for (int edgeIndex = 0; edgeIndex < count; edgeIndex++)
            {
                Vector2 start = vertices[edgeIndex];
                Vector2 end = vertices[(edgeIndex + 1) % count];
                if (!index.TryCollectSegmentCandidates(start, end, candidates, unique))
                    return false;

                for (int i = 0; i < candidates.Count; i++)
                {
                    int candidateIndex = candidates[i];
                    if (AreAdjacentEdges(edgeIndex, candidateIndex, count) || candidateIndex < edgeIndex)
                        continue;

                    TerritorySpatialEdge candidate = index.GetEdge(candidateIndex);
                    if (SegmentsIntersect(start, end, candidate.Start, candidate.End))
                        return false;
                }
            }

            return true;
        }

        public static bool PointOnSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            if (Vector2.SqrMagnitude(segment) <= Epsilon * Epsilon)
                return Vector2.SqrMagnitude(point - start) <= Epsilon * Epsilon;

            float cross = Cross(segment, point - start);
            if (Mathf.Abs(cross) > Epsilon)
                return false;

            return point.x >= Mathf.Min(start.x, end.x) - Epsilon &&
                   point.x <= Mathf.Max(start.x, end.x) + Epsilon &&
                   point.y >= Mathf.Min(start.y, end.y) - Epsilon &&
                   point.y <= Mathf.Max(start.y, end.y) + Epsilon;
        }

        public static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float o1 = Cross(b - a, c - a);
            float o2 = Cross(b - a, d - a);
            float o3 = Cross(d - c, a - c);
            float o4 = Cross(d - c, b - c);
            if (((o1 > Epsilon && o2 < -Epsilon) || (o1 < -Epsilon && o2 > Epsilon)) &&
                ((o3 > Epsilon && o4 < -Epsilon) || (o3 < -Epsilon && o4 > Epsilon)))
            {
                return true;
            }

            return Mathf.Abs(o1) <= Epsilon && PointOnSegment(c, a, b) ||
                   Mathf.Abs(o2) <= Epsilon && PointOnSegment(d, a, b) ||
                   Mathf.Abs(o3) <= Epsilon && PointOnSegment(a, c, d) ||
                   Mathf.Abs(o4) <= Epsilon && PointOnSegment(b, c, d);
        }

        private void CollectRectangleCandidates(
            Vector2 minimum,
            Vector2 maximum,
            List<int> results,
            HashSet<int> unique)
        {
            results.Clear();
            unique.Clear();
            Vector2Int first = TerritorySegmentTraversal.FromPoint(minimum);
            Vector2Int last = TerritorySegmentTraversal.FromPoint(maximum);
            for (int y = first.y; y <= last.y; y++)
            {
                for (int x = first.x; x <= last.x; x++)
                {
                    if (_chunks.TryGetValue(new Vector2Int(x, y), out TerritorySpatialChunk chunk))
                        chunk.Quadtree.Query(minimum, maximum, results, unique);
                }
            }
        }

        private static int FindFirstRayChunk(List<TerritorySpatialChunk> row, float pointX)
        {
            int target = Mathf.FloorToInt((pointX - Epsilon) / TerritorySegmentTraversal.ChunkSize);
            int lower = 0;
            int upper = row.Count;
            while (lower < upper)
            {
                int middle = lower + ((upper - lower) >> 1);
                if (row[middle].Coordinate.x < target)
                    lower = middle + 1;
                else
                    upper = middle;
            }

            return lower;
        }

        private static bool AreAdjacentEdges(int first, int second, int count)
            => first == second || (first + 1) % count == second || (second + 1) % count == first;

        private static float Cross(Vector2 first, Vector2 second)
            => first.x * second.y - first.y * second.x;

        private static bool IsFinite(Vector2 point)
            => !float.IsNaN(point.x) && !float.IsInfinity(point.x) &&
               !float.IsNaN(point.y) && !float.IsInfinity(point.y);

        private void Reset()
        {
            IsValid = false;
            NodeCount = 0;
            LastCandidateEdgeCount = 0;
            _edges = Array.Empty<TerritorySpatialEdge>();
            _chunks.Clear();
            _chunksByRow.Clear();
            _traversedChunks.Clear();
            _uniqueChunks.Clear();
            _candidateEdges.Clear();
            _uniqueEdges.Clear();
        }
    }
}
