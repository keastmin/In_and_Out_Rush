using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public static class TerritoryPolygonTriangulator
    {
        private const float Epsilon = TerritorySpatialIndex.Epsilon;

        private sealed class VertexChunk
        {
            public Vector2Int Coordinate;
            public readonly List<int> VertexIndices = new();
        }

        private sealed class VertexIndex
        {
            private readonly Dictionary<int, List<VertexChunk>> _rows = new();

            public VertexIndex(IReadOnlyList<Vector2> vertices)
            {
                var chunks = new Dictionary<Vector2Int, VertexChunk>();
                for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
                {
                    Vector2Int coordinate = TerritorySegmentTraversal.FromPoint(vertices[vertexIndex]);
                    if (!chunks.TryGetValue(coordinate, out VertexChunk chunk))
                    {
                        chunk = new VertexChunk { Coordinate = coordinate };
                        chunks.Add(coordinate, chunk);
                        if (!_rows.TryGetValue(coordinate.y, out List<VertexChunk> row))
                        {
                            row = new List<VertexChunk>();
                            _rows.Add(coordinate.y, row);
                        }

                        row.Add(chunk);
                    }

                    chunk.VertexIndices.Add(vertexIndex);
                }

                foreach (List<VertexChunk> row in _rows.Values)
                    row.Sort((left, right) => left.Coordinate.x.CompareTo(right.Coordinate.x));
            }

            public void Query(Vector2 minimum, Vector2 maximum, List<int> results)
            {
                results.Clear();
                Vector2Int first = TerritorySegmentTraversal.FromPoint(minimum);
                Vector2Int last = TerritorySegmentTraversal.FromPoint(maximum);
                for (int y = first.y; y <= last.y; y++)
                {
                    if (!_rows.TryGetValue(y, out List<VertexChunk> row))
                        continue;

                    int offset = LowerBound(row, first.x);
                    for (; offset < row.Count && row[offset].Coordinate.x <= last.x; offset++)
                        results.AddRange(row[offset].VertexIndices);
                }
            }

            private static int LowerBound(List<VertexChunk> row, int x)
            {
                int lower = 0;
                int upper = row.Count;
                while (lower < upper)
                {
                    int middle = lower + ((upper - lower) >> 1);
                    if (row[middle].Coordinate.x < x)
                        lower = middle + 1;
                    else
                        upper = middle;
                }

                return lower;
            }
        }

        public static bool TryBuildMeshData(
            IReadOnlyList<Vector2> source,
            out List<Vector2> polygon,
            out List<int> triangles,
            out string reason)
        {
            polygon = NormalizePolygon(source);
            triangles = null;
            reason = null;
            if (polygon.Count < 3)
                return Fail("A territory polygon needs at least 3 valid points.", out reason);

            for (int i = 0; i < polygon.Count; i++)
            {
                if (!IsFinite(polygon[i]))
                    return Fail($"Territory polygon contains an invalid point at index {i}.", out reason);
            }

            float area = Area(polygon);
            if (!IsFinite(area) || Mathf.Abs(area) <= Epsilon)
                return Fail("Territory polygon area is too small to generate a mesh.", out reason);

            if (!TerritorySpatialIndex.IsSimplePolygon(polygon))
                return Fail("Territory polygon is self-intersecting or has invalid edges.", out reason);

            if (!TryTriangulate(polygon, area, out triangles))
                return Fail("Territory triangulation could not find a valid ear.", out reason);

            int expectedIndexCount = (polygon.Count - 2) * 3;
            if (triangles.Count != expectedIndexCount)
                return Fail($"Territory triangulation failed. Expected {expectedIndexCount} indices, got {triangles.Count}.", out reason);

            return true;
        }

        public static List<Vector2> NormalizePath(IReadOnlyList<Vector2> source)
        {
            var result = new List<Vector2>();
            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
                AddPoint(result, source[i]);
            return result;
        }

        public static List<Vector2> NormalizePolygon(IReadOnlyList<Vector2> source)
        {
            List<Vector2> result = NormalizePath(source);
            if (result.Count > 1 && AreSamePoint(result[0], result[^1]))
                result.RemoveAt(result.Count - 1);

            bool changed;
            do
            {
                changed = false;
                if (result.Count < 3)
                    break;

                for (int i = 0; i < result.Count; i++)
                {
                    Vector2 previous = result[(i - 1 + result.Count) % result.Count];
                    Vector2 current = result[i];
                    Vector2 next = result[(i + 1) % result.Count];
                    if (!IsPointBetweenOnLine(current, previous, next))
                        continue;

                    result.RemoveAt(i);
                    changed = true;
                    break;
                }
            }
            while (changed);

            return result;
        }

        public static float Area(IReadOnlyList<Vector2> polygon)
        {
            float area = 0f;
            for (int previous = polygon.Count - 1, current = 0;
                 current < polygon.Count;
                 previous = current++)
            {
                area += polygon[previous].x * polygon[current].y -
                        polygon[current].x * polygon[previous].y;
            }

            return area * 0.5f;
        }

        public static void AddPoint(List<Vector2> points, Vector2 point)
        {
            if (points.Count == 0 || !AreSamePoint(points[^1], point))
                points.Add(point);
        }

        private static bool TryTriangulate(
            IReadOnlyList<Vector2> polygon,
            float signedArea,
            out List<int> triangles)
        {
            int count = polygon.Count;
            triangles = new List<int>((count - 2) * 3);
            int[] previous = new int[count];
            int[] next = new int[count];
            bool[] active = new bool[count];
            int[] order = new int[count];
            for (int i = 0; i < count; i++)
            {
                order[i] = signedArea > 0f ? i : count - 1 - i;
                active[i] = true;
            }

            for (int i = 0; i < count; i++)
            {
                int vertex = order[i];
                previous[vertex] = order[(i - 1 + count) % count];
                next[vertex] = order[(i + 1) % count];
            }

            var vertexIndex = new VertexIndex(polygon);
            var candidates = new List<int>();
            int remaining = count;
            int current = order[0];
            int failedAttempts = 0;
            while (remaining > 2)
            {
                int a = previous[current];
                int b = current;
                int c = next[current];
                if (IsEar(a, b, c, polygon, active, vertexIndex, candidates))
                {
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(a);
                    next[a] = c;
                    previous[c] = a;
                    active[b] = false;
                    remaining--;
                    current = c;
                    failedAttempts = 0;
                    continue;
                }

                current = next[current];
                failedAttempts++;
                if (failedAttempts > remaining * 2)
                    return false;
            }

            return true;
        }

        private static bool IsEar(
            int a,
            int b,
            int c,
            IReadOnlyList<Vector2> polygon,
            IReadOnlyList<bool> active,
            VertexIndex vertexIndex,
            List<int> candidates)
        {
            Vector2 pointA = polygon[a];
            Vector2 pointB = polygon[b];
            Vector2 pointC = polygon[c];
            if (Cross(pointB - pointA, pointC - pointA) <= Epsilon)
                return false;

            Vector2 minimum = Vector2.Min(pointA, Vector2.Min(pointB, pointC));
            Vector2 maximum = Vector2.Max(pointA, Vector2.Max(pointB, pointC));
            vertexIndex.Query(minimum, maximum, candidates);
            for (int i = 0; i < candidates.Count; i++)
            {
                int candidate = candidates[i];
                if (!active[candidate] || candidate == a || candidate == b || candidate == c)
                    continue;
                if (PointInTriangle(polygon[candidate], pointA, pointB, pointC))
                    return false;
            }

            return true;
        }

        private static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float first = Cross(c - b, point - b);
            float second = Cross(a - c, point - c);
            float third = Cross(b - a, point - a);
            return first >= 0f && second >= 0f && third >= 0f;
        }

        private static bool IsPointBetweenOnLine(Vector2 point, Vector2 a, Vector2 b)
            => Mathf.Abs(Cross(b - a, point - a)) <= Epsilon &&
               Vector2.Dot(point - a, point - b) <= Epsilon;

        private static bool AreSamePoint(Vector2 a, Vector2 b)
            => Vector2.SqrMagnitude(a - b) <= Epsilon * Epsilon;

        private static float Cross(Vector2 first, Vector2 second)
            => first.x * second.y - first.y * second.x;

        private static bool IsFinite(Vector2 point)
            => IsFinite(point.x) && IsFinite(point.y);

        private static bool IsFinite(float value)
            => !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool Fail(string message, out string reason)
        {
            reason = message;
            return false;
        }
    }
}
