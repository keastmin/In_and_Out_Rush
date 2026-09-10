using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public static class TerritoryExpansionCalculator
    {
        private const float Epsilon = TerritorySpatialIndex.Epsilon;
        private const float BoundarySampleDistance = 0.01f;

        private struct Intersection
        {
            public Vector2 Point;
            public int PolygonEdgeIndex;
            public int PathIndex;
            public float PolygonT;
            public float PathT;
            public bool IsExit;
            public bool IsEnter;
            public float PathOrder => PathIndex + PathT;
        }

        public static bool TryCalculate(
            IReadOnlyList<Vector2> sourceVertices,
            IReadOnlyList<Vector2> trailPoints,
            out TerritoryMeshData meshData)
        {
            meshData = null;
            if (sourceVertices == null || sourceVertices.Count < 3 ||
                trailPoints == null || trailPoints.Count < 2)
            {
                return false;
            }

            List<Vector2> source = TerritoryPolygonTriangulator.NormalizePolygon(sourceVertices);
            List<Vector2> path = TerritoryPolygonTriangulator.NormalizePath(trailPoints);
            if (source.Count < 3 || path.Count < 2)
                return false;

            var spatialIndex = new TerritorySpatialIndex();
            if (!spatialIndex.Rebuild(source))
                return false;

            var intersections = new List<Intersection>();
            if (!FindIntersections(source, path, spatialIndex, intersections))
                return false;

            intersections.Sort((left, right) => left.PathOrder.CompareTo(right.PathOrder));
            if (intersections.Count != 2)
            {
                Debug.LogWarning(
                    $"Territory expansion skipped. Expected 2 boundary crossings, found {intersections.Count}.");
                return false;
            }

            Intersection exit = intersections[0];
            Intersection enter = intersections[1];
            if (!exit.IsExit || !enter.IsEnter)
            {
                Debug.LogWarning("Territory expansion skipped. Boundary crossings are not an exit-enter pair.");
                return false;
            }

            return TryBuildExpandedPolygon(source, path, exit, enter, out meshData);
        }

        private static bool FindIntersections(
            IReadOnlyList<Vector2> polygon,
            IReadOnlyList<Vector2> path,
            TerritorySpatialIndex spatialIndex,
            List<Intersection> intersections)
        {
            var rawIntersections = new List<Intersection>();
            var candidateEdges = new List<int>();
            var uniqueEdges = new HashSet<int>();
            for (int pathIndex = 0; pathIndex < path.Count - 1; pathIndex++)
            {
                Vector2 pathStart = path[pathIndex];
                Vector2 pathEnd = path[pathIndex + 1];
                if (Vector2.SqrMagnitude(pathEnd - pathStart) <= Epsilon * Epsilon)
                    continue;

                if (!spatialIndex.TryCollectSegmentCandidates(
                        pathStart,
                        pathEnd,
                        candidateEdges,
                        uniqueEdges))
                {
                    return false;
                }

                for (int candidateOffset = 0; candidateOffset < candidateEdges.Count; candidateOffset++)
                {
                    int edgeIndex = candidateEdges[candidateOffset];
                    TerritorySpatialEdge edge = spatialIndex.GetEdge(edgeIndex);
                    if (!TryGetSegmentIntersection(
                            edge.Start,
                            edge.End,
                            pathStart,
                            pathEnd,
                            out Vector2 point,
                            out float polygonT,
                            out float pathT,
                            out bool overlaps))
                    {
                        if (overlaps)
                        {
                            Debug.LogWarning(
                                "Territory expansion skipped. Expansion path overlaps a territory boundary edge.");
                            return false;
                        }

                        continue;
                    }

                    var intersection = new Intersection
                    {
                        Point = point,
                        PolygonEdgeIndex = edgeIndex,
                        PathIndex = pathIndex,
                        PolygonT = polygonT,
                        PathT = pathT
                    };
                    NormalizeIntersection(ref intersection, polygon, path.Count);
                    rawIntersections.Add(intersection);
                }
            }

            rawIntersections.Sort((left, right) => left.PathOrder.CompareTo(right.PathOrder));
            for (int i = 0; i < rawIntersections.Count; i++)
            {
                Intersection intersection = rawIntersections[i];
                if (IsDuplicateIntersection(intersection, intersections))
                    continue;
                if (!TryClassifyIntersection(path, spatialIndex, ref intersection))
                    continue;
                intersections.Add(intersection);
            }

            return true;
        }

        private static bool TryGetSegmentIntersection(
            Vector2 edgeStart,
            Vector2 edgeEnd,
            Vector2 pathStart,
            Vector2 pathEnd,
            out Vector2 intersection,
            out float edgeT,
            out float pathT,
            out bool overlaps)
        {
            intersection = Vector2.zero;
            edgeT = 0f;
            pathT = 0f;
            overlaps = false;
            Vector2 edge = edgeEnd - edgeStart;
            Vector2 path = pathEnd - pathStart;
            float denominator = Cross(edge, path);
            Vector2 offset = pathStart - edgeStart;
            if (Mathf.Abs(denominator) <= Epsilon)
            {
                if (Mathf.Abs(Cross(offset, edge)) > Epsilon)
                    return false;

                float edgeLengthSqr = Vector2.Dot(edge, edge);
                if (edgeLengthSqr <= Epsilon * Epsilon)
                    return false;

                float firstT = Vector2.Dot(pathStart - edgeStart, edge) / edgeLengthSqr;
                float secondT = Vector2.Dot(pathEnd - edgeStart, edge) / edgeLengthSqr;
                float minimumT = Mathf.Max(0f, Mathf.Min(firstT, secondT));
                float maximumT = Mathf.Min(1f, Mathf.Max(firstT, secondT));
                if (maximumT < minimumT - Epsilon)
                    return false;
                if (maximumT - minimumT > Epsilon)
                {
                    overlaps = true;
                    return false;
                }

                edgeT = Mathf.Clamp01((minimumT + maximumT) * 0.5f);
                intersection = edgeStart + edge * edgeT;
                float pathLengthSqr = Vector2.Dot(path, path);
                pathT = pathLengthSqr > Epsilon * Epsilon
                    ? Mathf.Clamp01(Vector2.Dot(intersection - pathStart, path) / pathLengthSqr)
                    : 0f;
                return true;
            }

            edgeT = Cross(offset, path) / denominator;
            pathT = Cross(offset, edge) / denominator;
            if (edgeT < -Epsilon || edgeT > 1f + Epsilon ||
                pathT < -Epsilon || pathT > 1f + Epsilon)
            {
                return false;
            }

            edgeT = Mathf.Clamp01(edgeT);
            pathT = Mathf.Clamp01(pathT);
            intersection = edgeStart + edge * edgeT;
            return true;
        }

        private static void NormalizeIntersection(
            ref Intersection intersection,
            IReadOnlyList<Vector2> polygon,
            int pathCount)
        {
            if (intersection.PolygonT >= 1f - Epsilon)
            {
                intersection.PolygonEdgeIndex =
                    (intersection.PolygonEdgeIndex + 1) % polygon.Count;
                intersection.PolygonT = 0f;
                intersection.Point = polygon[intersection.PolygonEdgeIndex];
            }
            else if (intersection.PolygonT <= Epsilon)
            {
                intersection.PolygonT = 0f;
                intersection.Point = polygon[intersection.PolygonEdgeIndex];
            }

            if (intersection.PathT >= 1f - Epsilon && intersection.PathIndex < pathCount - 2)
            {
                intersection.PathIndex++;
                intersection.PathT = 0f;
            }
            else if (intersection.PathT <= Epsilon)
            {
                intersection.PathT = 0f;
            }
        }

        private static bool IsDuplicateIntersection(
            Intersection candidate,
            IReadOnlyList<Intersection> intersections)
        {
            for (int i = 0; i < intersections.Count; i++)
            {
                Intersection existing = intersections[i];
                if (Vector2.SqrMagnitude(existing.Point - candidate.Point) <= Epsilon * Epsilon &&
                    Mathf.Abs(existing.PathOrder - candidate.PathOrder) <= Epsilon)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryClassifyIntersection(
            IReadOnlyList<Vector2> path,
            TerritorySpatialIndex spatialIndex,
            ref Intersection intersection)
        {
            Vector2 before = SamplePathNearIntersection(path, intersection, -1);
            Vector2 after = SamplePathNearIntersection(path, intersection, 1);
            bool beforeInside = spatialIndex.Contains(before);
            bool afterInside = spatialIndex.Contains(after);
            intersection.IsExit = beforeInside && !afterInside;
            intersection.IsEnter = !beforeInside && afterInside;
            return intersection.IsExit || intersection.IsEnter;
        }

        private static Vector2 SamplePathNearIntersection(
            IReadOnlyList<Vector2> path,
            Intersection intersection,
            int direction)
        {
            float remainingDistance = BoundarySampleDistance;
            if (direction < 0)
            {
                int segmentIndex = intersection.PathIndex;
                float localT = intersection.PathT;
                while (segmentIndex >= 0)
                {
                    Vector2 start = path[segmentIndex];
                    Vector2 end = path[segmentIndex + 1];
                    float length = Vector2.Distance(start, end);
                    if (length > Epsilon)
                    {
                        float available = localT * length;
                        if (available >= remainingDistance)
                            return Vector2.Lerp(start, end, (available - remainingDistance) / length);
                        remainingDistance -= available;
                    }

                    segmentIndex--;
                    localT = 1f;
                }

                return path[0];
            }

            int forwardSegmentIndex = intersection.PathIndex;
            float forwardT = intersection.PathT;
            while (forwardSegmentIndex < path.Count - 1)
            {
                Vector2 start = path[forwardSegmentIndex];
                Vector2 end = path[forwardSegmentIndex + 1];
                float length = Vector2.Distance(start, end);
                if (length > Epsilon)
                {
                    float available = (1f - forwardT) * length;
                    if (available >= remainingDistance)
                        return Vector2.Lerp(start, end, forwardT + remainingDistance / length);
                    remainingDistance -= available;
                }

                forwardSegmentIndex++;
                forwardT = 0f;
            }

            return path[^1];
        }

        private static bool TryBuildExpandedPolygon(
            IReadOnlyList<Vector2> polygon,
            IReadOnlyList<Vector2> path,
            Intersection exit,
            Intersection enter,
            out TerritoryMeshData meshData)
        {
            List<Vector2> pathSegment = BuildPathSegment(path, exit, enter);
            List<Vector2> boundaryExitToEnter = BuildBoundaryArc(polygon, exit, enter);
            List<Vector2> boundaryEnterToExit = BuildBoundaryArc(polygon, enter, exit);
            List<Vector2> reversedPathSegment = new(pathSegment);
            reversedPathSegment.Reverse();
            List<Vector2> firstCandidate = CombinePolygonParts(pathSegment, boundaryEnterToExit);
            List<Vector2> secondCandidate = CombinePolygonParts(boundaryExitToEnter, reversedPathSegment);
            meshData = SelectExpandedCandidate(polygon, firstCandidate, secondCandidate);
            if (meshData == null)
                Debug.LogWarning("Territory expansion skipped. No valid expanded polygon candidate was found.");
            return meshData != null;
        }

        private static List<Vector2> BuildPathSegment(
            IReadOnlyList<Vector2> path,
            Intersection from,
            Intersection to)
        {
            var result = new List<Vector2>();
            TerritoryPolygonTriangulator.AddPoint(result, from.Point);
            for (int i = from.PathIndex + 1; i <= to.PathIndex && i < path.Count; i++)
                TerritoryPolygonTriangulator.AddPoint(result, path[i]);
            TerritoryPolygonTriangulator.AddPoint(result, to.Point);
            return result;
        }

        private static List<Vector2> BuildBoundaryArc(
            IReadOnlyList<Vector2> polygon,
            Intersection from,
            Intersection to)
        {
            var result = new List<Vector2>();
            TerritoryPolygonTriangulator.AddPoint(result, from.Point);
            if (from.PolygonEdgeIndex == to.PolygonEdgeIndex &&
                from.PolygonT <= to.PolygonT + Epsilon)
            {
                TerritoryPolygonTriangulator.AddPoint(result, to.Point);
                return result;
            }

            int edgeIndex = from.PolygonEdgeIndex;
            int guard = 0;
            do
            {
                int nextVertex = (edgeIndex + 1) % polygon.Count;
                TerritoryPolygonTriangulator.AddPoint(result, polygon[nextVertex]);
                edgeIndex = nextVertex;
            }
            while (edgeIndex != to.PolygonEdgeIndex && ++guard <= polygon.Count);
            TerritoryPolygonTriangulator.AddPoint(result, to.Point);
            return result;
        }

        private static TerritoryMeshData SelectExpandedCandidate(
            IReadOnlyList<Vector2> source,
            IReadOnlyList<Vector2> first,
            IReadOnlyList<Vector2> second)
        {
            float oldArea = Mathf.Abs(TerritoryPolygonTriangulator.Area(source));
            TerritoryMeshData best = null;
            float bestArea = float.MinValue;
            TryUse(first);
            TryUse(second);
            return best;

            void TryUse(IReadOnlyList<Vector2> candidate)
            {
                float candidateArea = Mathf.Abs(TerritoryPolygonTriangulator.Area(candidate));
                if (candidateArea <= oldArea + Epsilon ||
                    !TerritoryPolygonTriangulator.TryBuildMeshData(
                        candidate,
                        out List<Vector2> normalized,
                        out List<int> triangles,
                        out _))
                {
                    return;
                }

                candidateArea = Mathf.Abs(TerritoryPolygonTriangulator.Area(normalized));
                if (candidateArea <= oldArea + Epsilon ||
                    best != null && candidateArea <= bestArea)
                {
                    return;
                }

                best = new TerritoryMeshData(normalized, triangles);
                bestArea = candidateArea;
            }
        }

        private static List<Vector2> CombinePolygonParts(params IReadOnlyList<Vector2>[] parts)
        {
            var combined = new List<Vector2>();
            for (int i = 0; i < parts.Length; i++)
            {
                IReadOnlyList<Vector2> part = parts[i];
                if (part == null)
                    continue;
                for (int j = 0; j < part.Count; j++)
                    TerritoryPolygonTriangulator.AddPoint(combined, part[j]);
            }

            if (combined.Count > 1 &&
                Vector2.SqrMagnitude(combined[0] - combined[^1]) <= Epsilon * Epsilon)
            {
                combined.RemoveAt(combined.Count - 1);
            }

            return TerritoryPolygonTriangulator.NormalizePolygon(combined);
        }

        private static float Cross(Vector2 first, Vector2 second)
            => first.x * second.y - first.y * second.x;
    }
}
