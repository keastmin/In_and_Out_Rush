using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritoryContainmentIndex
    {
        public const float BucketSize = 8f;

        private const float Epsilon = 0.0001f;
        private const int MaximumAverageReferencesPerEdge = 16;

        private static readonly ProfilerMarker RebuildMarker =
            new("TerritoryContainmentIndex.Rebuild");
        private static readonly ProfilerMarker QueryMarker =
            new("TerritoryContainmentIndex.Query");
        private static readonly ProfilerMarker CandidateEdgeMarker =
            new("TerritoryContainmentIndex.CandidateEdges");
        private static readonly ProfilerMarker ReferenceFallbackMarker =
            new("TerritoryContainmentIndex.ReferenceFallback");

        private readonly Dictionary<int, List<int>> _buildBuckets = new();
        private readonly Dictionary<int, int[]> _buckets = new();
        private int _vertexCount;
        private float _minimumY;
        private float _maximumY;

        public bool IsValid { get; private set; }
        public int EdgeCount => _vertexCount;
        public int LastRebuildEdgeReferenceCount { get; private set; }
        public int LastCandidateEdgeCount { get; private set; }
        public long QueryCount { get; private set; }
        public long CandidateEdgeCount { get; private set; }
        public long CandidateEdgeInspectionCount { get; private set; }
        public long ReferenceFallbackCount { get; private set; }

        public void Rebuild(IReadOnlyList<Vector2> vertices)
        {
            using (RebuildMarker.Auto())
            {
                IsValid = false;
                _vertexCount = vertices?.Count ?? 0;
                LastRebuildEdgeReferenceCount = 0;
                LastCandidateEdgeCount = 0;
                _buildBuckets.Clear();
                _buckets.Clear();

                if (_vertexCount < 3)
                    return;

                _minimumY = vertices[0].y;
                _maximumY = vertices[0].y;

                int maximumReferences = _vertexCount * MaximumAverageReferencesPerEdge;
                if (maximumReferences < _vertexCount)
                    return;

                for (int edgeIndex = 0; edgeIndex < _vertexCount; edgeIndex++)
                {
                    Vector2 a = vertices[edgeIndex];
                    Vector2 b = vertices[(edgeIndex + 1) % _vertexCount];
                    if (!IsFinite(a) || !IsFinite(b))
                        return;

                    _minimumY = Mathf.Min(_minimumY, a.y);
                    _maximumY = Mathf.Max(_maximumY, a.y);
                    if (!TryGetBucketRange(a.y, b.y, out int firstBucket, out int lastBucket))
                        return;

                    for (int bucket = firstBucket; bucket <= lastBucket; bucket++)
                    {
                        if (!_buildBuckets.TryGetValue(bucket, out List<int> edgeIndices))
                        {
                            edgeIndices = new List<int>();
                            _buildBuckets.Add(bucket, edgeIndices);
                        }

                        edgeIndices.Add(edgeIndex);
                        LastRebuildEdgeReferenceCount++;
                        if (LastRebuildEdgeReferenceCount > maximumReferences || bucket == int.MaxValue)
                        {
                            _buildBuckets.Clear();
                            return;
                        }
                    }
                }

                foreach (KeyValuePair<int, List<int>> pair in _buildBuckets)
                    _buckets.Add(pair.Key, pair.Value.ToArray());

                _buildBuckets.Clear();
                IsValid = true;
            }
        }

        public bool TryContains(Vector2 point, IReadOnlyList<Vector2> vertices, out bool contains)
        {
            using (QueryMarker.Auto())
            {
                contains = false;
                LastCandidateEdgeCount = 0;
                QueryCount++;

                if (!IsValid || vertices == null || vertices.Count != _vertexCount ||
                    !TryGetBucket(point.y, out int bucket))
                {
                    ReferenceFallbackCount++;
                    using (ReferenceFallbackMarker.Auto()) { }
                    return false;
                }

                if (!_buckets.TryGetValue(bucket, out int[] edgeIndices))
                {
                    if (point.y < _minimumY - Epsilon || point.y > _maximumY + Epsilon)
                        return true;

                    ReferenceFallbackCount++;
                    using (ReferenceFallbackMarker.Auto()) { }
                    return false;
                }

                LastCandidateEdgeCount = edgeIndices.Length;
                CandidateEdgeCount += edgeIndices.Length;
                using (CandidateEdgeMarker.Auto())
                {
                    for (int i = 0; i < edgeIndices.Length; i++)
                    {
                        CandidateEdgeInspectionCount++;
                        Vector2 a = vertices[edgeIndices[i]];
                        Vector2 b = vertices[(edgeIndices[i] + 1) % _vertexCount];
                        if (PointOnSegment(point, a, b))
                        {
                            contains = true;
                            return true;
                        }
                    }

                    bool isInside = false;
                    for (int i = 0; i < edgeIndices.Length; i++)
                    {
                        CandidateEdgeInspectionCount++;
                        Vector2 pi = vertices[(edgeIndices[i] + 1) % _vertexCount];
                        Vector2 pj = vertices[edgeIndices[i]];
                        if ((pi.y > point.y) != (pj.y > point.y))
                        {
                            float atX = (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x;
                            if (point.x < atX)
                                isInside = !isInside;
                        }
                    }

                    contains = isInside;
                    return true;
                }
            }
        }

        private static bool TryGetBucketRange(float aY, float bY, out int firstBucket, out int lastBucket)
        {
            firstBucket = 0;
            lastBucket = 0;
            float minimum = Mathf.Min(aY, bY) - Epsilon;
            float maximum = Mathf.Max(aY, bY) + Epsilon;
            return TryGetBucket(minimum, out firstBucket) &&
                   TryGetBucket(maximum, out lastBucket) &&
                   firstBucket <= lastBucket;
        }

        private static bool TryGetBucket(float y, out int bucket)
        {
            bucket = 0;
            float scaled = y / BucketSize;
            if (float.IsNaN(scaled) || float.IsInfinity(scaled) ||
                scaled < int.MinValue || scaled >= int.MaxValue)
            {
                return false;
            }

            bucket = (int)Mathf.Floor(scaled);
            return true;
        }

        private static bool IsFinite(Vector2 point)
        {
            return !float.IsNaN(point.x) && !float.IsInfinity(point.x) &&
                   !float.IsNaN(point.y) && !float.IsInfinity(point.y);
        }

        private static bool PointOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 segment = b - a;
            if (Vector2.SqrMagnitude(segment) <= Epsilon * Epsilon)
                return Vector2.SqrMagnitude(point - a) <= Epsilon * Epsilon;

            float cross = segment.x * (point.y - a.y) - segment.y * (point.x - a.x);
            if (Mathf.Abs(cross) > Epsilon)
                return false;

            return point.x >= Mathf.Min(a.x, b.x) - Epsilon &&
                   point.x <= Mathf.Max(a.x, b.x) + Epsilon &&
                   point.y >= Mathf.Min(a.y, b.y) - Epsilon &&
                   point.y <= Mathf.Max(a.y, b.y) + Epsilon;
        }
    }
}
