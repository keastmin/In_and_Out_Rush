using System.Collections.Generic;
using ProjectIO.Territory;
using UnityEngine;

public class Territory
{
    const float EPS = 0.0001f;
    const float BoundarySampleDistance = 0.01f;
    const int PrototypeVertexBudget = 256;
    const float PrototypeSimplificationTolerance = 0.1f;

    public List<Vector2> Vertices = new();
    readonly TerritoryBoundsIndex boundsIndex = new();

    struct Intersection
    {
        public Vector2 point;
        public int polyEdgeIndex;
        public int pathIndex;
        public float polyT;
        public float pathT;
        public bool isExit;
        public bool isEnter;

        public float PathOrder => pathIndex + pathT;
    }

    readonly List<Intersection> intersections = new();

    public void Expand(List<Vector2> path)
    {
        TryExpand(path);
    }

    public bool TryExpand(List<Vector2> path)
    {
        return ExpandInternal(path, out _);
    }

    public bool TryExpand(List<Vector2> path, out TerritoryMeshData meshData)
    {
        return ExpandInternal(path, out meshData);
    }

    bool ExpandInternal(List<Vector2> path, out TerritoryMeshData meshData)
    {
        meshData = null;

        if (Vertices == null || Vertices.Count < 3)
            return false;

        List<Vector2> normalizedPath = NormalizePath(path);
        if (normalizedPath.Count < 2)
            return false;

        if (!FindIntersections(normalizedPath))
            return false;

        SortIntersectionsByPath();
        if (intersections.Count != 2)
        {
            Debug.LogWarning($"Territory expansion skipped. Expected 2 boundary crossings, found {intersections.Count}.");
            return false;
        }

        Intersection exit = intersections[0];
        Intersection enter = intersections[1];
        if (!exit.isExit || !enter.isEnter)
        {
            Debug.LogWarning("Territory expansion skipped. Boundary crossings are not an exit-enter pair.");
            return false;
        }

        if (!TryBuildExpandedPolygon(exit, enter, normalizedPath, out meshData))
            return false;

        ApplyNewPolygon(meshData.Vertices);
        return true;
    }

    bool FindIntersections(List<Vector2> playerPath)
    {
        intersections.Clear();

        var rawIntersections = new List<Intersection>();
        int polyCount = Vertices.Count;
        int pathCount = playerPath.Count;

        for (int pathIndex = 0; pathIndex < pathCount - 1; pathIndex++)
        {
            Vector2 pathStart = playerPath[pathIndex];
            Vector2 pathEnd = playerPath[pathIndex + 1];
            if (Vector2.SqrMagnitude(pathEnd - pathStart) <= EPS * EPS)
                continue;

            for (int edgeIndex = 0; edgeIndex < polyCount; edgeIndex++)
            {
                Vector2 edgeStart = Vertices[edgeIndex];
                Vector2 edgeEnd = Vertices[(edgeIndex + 1) % polyCount];
                if (!TryGetSegmentIntersection(
                        edgeStart,
                        edgeEnd,
                        pathStart,
                        pathEnd,
                        out Vector2 point,
                        out float polyT,
                        out float pathT,
                        out bool overlaps))
                {
                    if (overlaps)
                    {
                        Debug.LogWarning("Territory expansion skipped. Expansion path overlaps a territory boundary edge.");
                        return false;
                    }

                    continue;
                }

                var intersection = new Intersection
                {
                    point = point,
                    polyEdgeIndex = edgeIndex,
                    pathIndex = pathIndex,
                    polyT = polyT,
                    pathT = pathT
                };
                NormalizeIntersection(ref intersection, pathCount);
                rawIntersections.Add(intersection);
            }
        }

        rawIntersections.Sort((a, b) => a.PathOrder.CompareTo(b.PathOrder));
        for (int i = 0; i < rawIntersections.Count; i++)
        {
            Intersection intersection = rawIntersections[i];
            if (IsDuplicateIntersection(intersection))
                continue;

            if (!TryClassifyIntersection(playerPath, ref intersection))
                continue;

            intersections.Add(intersection);
        }

        return true;
    }

    static bool TryGetSegmentIntersection(
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

        if (Mathf.Abs(denominator) <= EPS)
        {
            if (Mathf.Abs(Cross(offset, edge)) > EPS)
                return false;

            float edgeLengthSqr = Vector2.Dot(edge, edge);
            if (edgeLengthSqr <= EPS * EPS)
                return false;

            float t0 = Vector2.Dot(pathStart - edgeStart, edge) / edgeLengthSqr;
            float t1 = Vector2.Dot(pathEnd - edgeStart, edge) / edgeLengthSqr;
            float minT = Mathf.Max(0f, Mathf.Min(t0, t1));
            float maxT = Mathf.Min(1f, Mathf.Max(t0, t1));

            if (maxT < minT - EPS)
                return false;

            if (maxT - minT > EPS)
            {
                overlaps = true;
                return false;
            }

            edgeT = Mathf.Clamp01((minT + maxT) * 0.5f);
            intersection = edgeStart + edge * edgeT;
            float pathLengthSqr = Vector2.Dot(path, path);
            pathT = pathLengthSqr > EPS * EPS
                ? Mathf.Clamp01(Vector2.Dot(intersection - pathStart, path) / pathLengthSqr)
                : 0f;
            return true;
        }

        edgeT = Cross(offset, path) / denominator;
        pathT = Cross(offset, edge) / denominator;
        if (edgeT < -EPS || edgeT > 1f + EPS || pathT < -EPS || pathT > 1f + EPS)
            return false;

        edgeT = Mathf.Clamp01(edgeT);
        pathT = Mathf.Clamp01(pathT);
        intersection = edgeStart + edge * edgeT;
        return true;
    }

    void NormalizeIntersection(ref Intersection intersection, int pathCount)
    {
        int polyCount = Vertices.Count;
        if (intersection.polyT >= 1f - EPS)
        {
            intersection.polyEdgeIndex = (intersection.polyEdgeIndex + 1) % polyCount;
            intersection.polyT = 0f;
            intersection.point = Vertices[intersection.polyEdgeIndex];
        }
        else if (intersection.polyT <= EPS)
        {
            intersection.polyT = 0f;
            intersection.point = Vertices[intersection.polyEdgeIndex];
        }

        if (intersection.pathT >= 1f - EPS && intersection.pathIndex < pathCount - 2)
        {
            intersection.pathIndex++;
            intersection.pathT = 0f;
        }
        else if (intersection.pathT <= EPS)
        {
            intersection.pathT = 0f;
        }
    }

    bool IsDuplicateIntersection(Intersection candidate)
    {
        for (int i = 0; i < intersections.Count; i++)
        {
            Intersection existing = intersections[i];
            if (Vector2.SqrMagnitude(existing.point - candidate.point) <= EPS * EPS &&
                Mathf.Abs(existing.PathOrder - candidate.PathOrder) <= EPS)
            {
                return true;
            }
        }

        return false;
    }

    bool TryClassifyIntersection(List<Vector2> playerPath, ref Intersection intersection)
    {
        Vector2 before = SamplePathNearIntersection(playerPath, intersection, -1);
        Vector2 after = SamplePathNearIntersection(playerPath, intersection, 1);
        bool beforeInside = IsPointInPolygon(before);
        bool afterInside = IsPointInPolygon(after);

        intersection.isExit = beforeInside && !afterInside;
        intersection.isEnter = !beforeInside && afterInside;
        return intersection.isExit || intersection.isEnter;
    }

    static Vector2 SamplePathNearIntersection(List<Vector2> playerPath, Intersection intersection, int direction)
    {
        float remainingDistance = BoundarySampleDistance;
        if (direction < 0)
        {
            int segmentIndex = intersection.pathIndex;
            float localT = intersection.pathT;
            while (segmentIndex >= 0)
            {
                Vector2 start = playerPath[segmentIndex];
                Vector2 end = playerPath[segmentIndex + 1];
                float length = Vector2.Distance(start, end);
                if (length > EPS)
                {
                    float available = localT * length;
                    if (available >= remainingDistance)
                    {
                        float nextT = (available - remainingDistance) / length;
                        return Vector2.Lerp(start, end, nextT);
                    }

                    remainingDistance -= available;
                }

                segmentIndex--;
                localT = 1f;
            }

            return playerPath[0];
        }

        int forwardSegmentIndex = intersection.pathIndex;
        float forwardT = intersection.pathT;
        while (forwardSegmentIndex < playerPath.Count - 1)
        {
            Vector2 start = playerPath[forwardSegmentIndex];
            Vector2 end = playerPath[forwardSegmentIndex + 1];
            float length = Vector2.Distance(start, end);
            if (length > EPS)
            {
                float available = (1f - forwardT) * length;
                if (available >= remainingDistance)
                {
                    float nextT = forwardT + remainingDistance / length;
                    return Vector2.Lerp(start, end, nextT);
                }

                remainingDistance -= available;
            }

            forwardSegmentIndex++;
            forwardT = 0f;
        }

        return playerPath[^1];
    }

    void SortIntersectionsByPath()
    {
        intersections.Sort((a, b) => a.PathOrder.CompareTo(b.PathOrder));
    }

    bool TryBuildExpandedPolygon(
        Intersection exit,
        Intersection enter,
        List<Vector2> playerPath,
        out TerritoryMeshData meshData)
    {
        meshData = null;

        List<Vector2> pathSegment = BuildPathSegment(exit, enter, playerPath);
        List<Vector2> boundaryExitToEnter = BuildBoundaryArc(exit, enter);
        List<Vector2> boundaryEnterToExit = BuildBoundaryArc(enter, exit);
        List<Vector2> reversedPathSegment = new List<Vector2>(pathSegment);
        reversedPathSegment.Reverse();

        List<Vector2> candidateFromPathThenBoundary = CombinePolygonParts(pathSegment, boundaryEnterToExit);
        List<Vector2> candidateFromBoundaryThenPath = CombinePolygonParts(boundaryExitToEnter, reversedPathSegment);

        meshData = SelectExpandedCandidate(candidateFromPathThenBoundary, candidateFromBoundaryThenPath);
        if (meshData == null)
            Debug.LogWarning("Territory expansion skipped. No valid expanded polygon candidate was found.");

        return meshData != null;
    }

    List<Vector2> BuildPathSegment(Intersection from, Intersection to, List<Vector2> playerPath)
    {
        var segment = new List<Vector2>();
        AddPoint(segment, from.point);

        for (int i = from.pathIndex + 1; i <= to.pathIndex && i < playerPath.Count; i++)
            AddPoint(segment, playerPath[i]);

        AddPoint(segment, to.point);
        return segment;
    }

    List<Vector2> BuildBoundaryArc(Intersection from, Intersection to)
    {
        var arc = new List<Vector2>();
        AddPoint(arc, from.point);

        if (from.polyEdgeIndex == to.polyEdgeIndex && from.polyT <= to.polyT + EPS)
        {
            AddPoint(arc, to.point);
            return arc;
        }

        int edgeIndex = from.polyEdgeIndex;
        int guard = 0;
        do
        {
            int nextVertexIndex = (edgeIndex + 1) % Vertices.Count;
            AddPoint(arc, Vertices[nextVertexIndex]);
            edgeIndex = nextVertexIndex;
        }
        while (edgeIndex != to.polyEdgeIndex && ++guard <= Vertices.Count);

        AddPoint(arc, to.point);
        return arc;
    }

    TerritoryMeshData SelectExpandedCandidate(List<Vector2> candidateA, List<Vector2> candidateB)
    {
        float oldArea = Mathf.Abs(Area(Vertices));
        TerritoryMeshData best = null;
        float bestArea = float.MinValue;

        TryUseCandidate(candidateA);
        TryUseCandidate(candidateB);

        return best;

        void TryUseCandidate(List<Vector2> candidate)
        {
            if (!TryGetExpandedCandidateInfo(candidate, oldArea, out float candidateArea, out TerritoryMeshData candidateData))
                return;

            if (best == null || candidateArea > bestArea)
            {
                best = candidateData;
                bestArea = candidateArea;
            }
        }
    }

    bool TryGetExpandedCandidateInfo(
        List<Vector2> candidate,
        float oldArea,
        out float candidateArea,
        out TerritoryMeshData meshData)
    {
        candidateArea = 0f;
        meshData = null;

        if (candidate == null || candidate.Count < 3)
            return false;

        candidateArea = Mathf.Abs(Area(candidate));
        if (candidateArea <= oldArea + EPS)
            return false;

        if (!TryBuildMeshData(candidate, false, out List<Vector2> normalizedPolygon, out List<int> triangles))
            return false;

        candidateArea = Mathf.Abs(Area(normalizedPolygon));
        if (candidateArea <= oldArea + EPS)
            return false;

        meshData = new TerritoryMeshData(normalizedPolygon, triangles);
        return true;
    }

    void ApplyNewPolygon(List<Vector2> newPoly)
    {
        Vertices.Clear();
        Vertices.AddRange(newPoly);
        boundsIndex.Rebuild(Vertices);
    }

    public void ReplaceVertices(IReadOnlyList<Vector2> vertices)
    {
        Vertices.Clear();
        if (vertices != null)
        {
            for (int i = 0; i < vertices.Count; i++)
                Vertices.Add(vertices[i]);
        }

        boundsIndex.Rebuild(Vertices);
    }

    bool PointInPolygon(Vector2 point, List<Vector2> poly)
    {
        if (poly == null || poly.Count < 3)
            return false;

        if (PointOnPolygonBoundary(point, poly))
            return true;

        bool isInside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            Vector2 pi = poly[i];
            Vector2 pj = poly[j];
            if ((pi.y > point.y) != (pj.y > point.y))
            {
                float atX = (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x;
                if (point.x < atX)
                    isInside = !isInside;
            }
        }

        return isInside;
    }

    public static Mesh GenerateMesh(List<Vector2> polygon)
    {
        if (!TryBuildMeshData(polygon, true, out List<Vector2> normalizedPolygon, out List<int> trianglesList))
            return null;

        Vector3[] vertices = new Vector3[normalizedPolygon.Count];
        for (int i = 0; i < normalizedPolygon.Count; i++)
            vertices[i] = new Vector3(normalizedPolygon[i].x, 0, normalizedPolygon[i].y);

        var mesh = new Mesh
        {
            name = "PolygonMesh",
            vertices = vertices
        };

        if (vertices.Length > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.triangles = trianglesList.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    public static Mesh GenerateMesh(TerritoryMeshData meshData)
    {
        if (meshData == null ||
            meshData.Vertices == null ||
            meshData.Vertices.Count < 3 ||
            meshData.Triangles == null ||
            meshData.Triangles.Count < 3)
        {
            return null;
        }

        Vector3[] vertices = new Vector3[meshData.Vertices.Count];
        for (int i = 0; i < meshData.Vertices.Count; i++)
            vertices[i] = new Vector3(meshData.Vertices[i].x, 0f, meshData.Vertices[i].y);

        var mesh = new Mesh
        {
            name = "PolygonMesh",
            vertices = vertices
        };

        if (vertices.Length > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.triangles = meshData.Triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static bool TryBuildMeshData(
        List<Vector2> source,
        bool logErrors,
        out List<Vector2> polygon,
        out List<int> triangles)
    {
        polygon = NormalizePolygon(source);
        triangles = null;

        if (polygon.Count < 3)
            return FailMeshValidation(logErrors, "A territory polygon needs at least 3 valid points.");

        if (!HasFinitePoints(polygon, out int invalidPointIndex))
            return FailMeshValidation(logErrors, $"Territory polygon contains an invalid point at index {invalidPointIndex}.");

        float area = Area(polygon);
        if (!IsFinite(area) || Mathf.Abs(area) <= EPS)
            return FailMeshValidation(logErrors, "Territory polygon area is too small to generate a mesh.");

        if (!IsSimplePolygon(polygon))
            return FailMeshValidation(logErrors, "Territory polygon is self-intersecting or has invalid edges.");

        triangles = EarClippingTriangulate(polygon);
        if (!IsTriangleIndexBufferValid(polygon, triangles, out string reason))
            return FailMeshValidation(logErrors, reason);

        return true;
    }

    static bool FailMeshValidation(bool logErrors, string message)
    {
        if (logErrors)
            Debug.LogError(message);

        return false;
    }

    static bool HasFinitePoints(List<Vector2> polygon, out int invalidPointIndex)
    {
        for (int i = 0; i < polygon.Count; i++)
        {
            if (!IsFinite(polygon[i]))
            {
                invalidPointIndex = i;
                return false;
            }
        }

        invalidPointIndex = -1;
        return true;
    }

    static bool IsTriangleIndexBufferValid(List<Vector2> polygon, List<int> triangles, out string reason)
    {
        reason = null;

        if (triangles == null)
        {
            reason = "Territory triangulation failed because the triangle buffer is null.";
            return false;
        }

        int expectedIndexCount = (polygon.Count - 2) * 3;
        if (triangles.Count != expectedIndexCount)
        {
            reason = $"Territory triangulation failed. Expected {expectedIndexCount} indices, got {triangles.Count}.";
            return false;
        }

        if (triangles.Count % 3 != 0)
        {
            reason = "Territory triangulation produced a malformed index buffer.";
            return false;
        }

        for (int i = 0; i < triangles.Count; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];
            if (!IsTriangleIndexValid(a, polygon.Count) ||
                !IsTriangleIndexValid(b, polygon.Count) ||
                !IsTriangleIndexValid(c, polygon.Count))
            {
                reason = $"Territory triangulation produced an out-of-range index at triangle {i / 3}.";
                return false;
            }
        }

        return true;
    }

    static bool IsTriangleIndexValid(int index, int vertexCount)
    {
        return index >= 0 && index < vertexCount;
    }

    static List<Vector2> NormalizePath(List<Vector2> source)
    {
        var result = new List<Vector2>();
        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
            AddPoint(result, source[i]);

        return result;
    }

    static List<Vector2> NormalizePolygon(List<Vector2> source)
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
                Vector2 prev = result[(i - 1 + result.Count) % result.Count];
                Vector2 current = result[i];
                Vector2 next = result[(i + 1) % result.Count];
                if (!IsPointBetweenOnLine(current, prev, next))
                    continue;

                result.RemoveAt(i);
                changed = true;
                break;
            }
        }
        while (changed);

        return SimplifyForPrototype(result);
    }

    static List<Vector2> SimplifyForPrototype(List<Vector2> polygon)
    {
        if (polygon.Count <= PrototypeVertexBudget)
            return polygon;

        float tolerance = PrototypeSimplificationTolerance;
        List<Vector2> simplified = polygon;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            int splitIndex = polygon.Count / 2;
            var firstArc = new List<Vector2>(splitIndex + 1);
            var secondArc = new List<Vector2>(polygon.Count - splitIndex + 1);
            for (int i = 0; i <= splitIndex; i++)
                firstArc.Add(polygon[i]);
            for (int i = splitIndex; i < polygon.Count; i++)
                secondArc.Add(polygon[i]);
            secondArc.Add(polygon[0]);

            simplified = SimplifyOpenPath(firstArc, tolerance);
            List<Vector2> secondHalf = SimplifyOpenPath(secondArc, tolerance);
            for (int i = 1; i < secondHalf.Count; i++)
                AddPoint(simplified, secondHalf[i]);

            if (simplified.Count > 1 && AreSamePoint(simplified[0], simplified[^1]))
                simplified.RemoveAt(simplified.Count - 1);

            if (simplified.Count <= PrototypeVertexBudget)
                return simplified;

            tolerance *= 2f;
        }

        return simplified;
    }

    static List<Vector2> SimplifyOpenPath(List<Vector2> points, float tolerance)
    {
        int count = points.Count;
        if (count <= 2)
            return points;

        var keep = new bool[count];
        keep[0] = true;
        keep[count - 1] = true;
        float toleranceSqr = tolerance * tolerance;
        var ranges = new Stack<Vector2Int>();
        ranges.Push(new Vector2Int(0, count - 1));

        while (ranges.Count > 0)
        {
            Vector2Int range = ranges.Pop();
            float greatestDistanceSqr = 0f;
            int greatestDistanceIndex = -1;
            Vector2 start = points[range.x];
            Vector2 end = points[range.y];
            for (int i = range.x + 1; i < range.y; i++)
            {
                float distanceSqr = PointToSegmentDistanceSqr(points[i], start, end);
                if (distanceSqr > greatestDistanceSqr)
                {
                    greatestDistanceSqr = distanceSqr;
                    greatestDistanceIndex = i;
                }
            }

            if (greatestDistanceIndex < 0 || greatestDistanceSqr <= toleranceSqr)
                continue;

            keep[greatestDistanceIndex] = true;
            ranges.Push(new Vector2Int(range.x, greatestDistanceIndex));
            ranges.Push(new Vector2Int(greatestDistanceIndex, range.y));
        }

        var result = new List<Vector2>();
        for (int i = 0; i < count; i++)
        {
            if (keep[i])
                result.Add(points[i]);
        }

        return result;
    }

    static float PointToSegmentDistanceSqr(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float segmentLengthSqr = Vector2.Dot(segment, segment);
        if (segmentLengthSqr <= EPS * EPS)
            return Vector2.SqrMagnitude(point - start);

        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segmentLengthSqr);
        return Vector2.SqrMagnitude(point - (start + segment * t));
    }

    static List<Vector2> CombinePolygonParts(params List<Vector2>[] parts)
    {
        var combined = new List<Vector2>();
        for (int i = 0; i < parts.Length; i++)
        {
            List<Vector2> part = parts[i];
            if (part == null)
                continue;

            for (int j = 0; j < part.Count; j++)
                AddPoint(combined, part[j]);
        }

        if (combined.Count > 1 && AreSamePoint(combined[0], combined[^1]))
            combined.RemoveAt(combined.Count - 1);

        return NormalizePolygon(combined);
    }

    static void AddPoint(List<Vector2> points, Vector2 point)
    {
        if (points.Count == 0 || !AreSamePoint(points[^1], point))
            points.Add(point);
    }

    static bool AreSamePoint(Vector2 a, Vector2 b)
    {
        return Vector2.SqrMagnitude(a - b) <= EPS * EPS;
    }

    static bool IsFinite(Vector2 point)
    {
        return IsFinite(point.x) && IsFinite(point.y);
    }

    static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    static bool PointOnPolygonBoundary(Vector2 point, List<Vector2> poly)
    {
        for (int i = 0; i < poly.Count; i++)
        {
            if (PointOnSegment(point, poly[i], poly[(i + 1) % poly.Count]))
                return true;
        }

        return false;
    }

    static bool PointOnSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 segment = b - a;
        if (Vector2.SqrMagnitude(segment) <= EPS * EPS)
            return AreSamePoint(point, a);

        if (Mathf.Abs(Cross(segment, point - a)) > EPS)
            return false;

        return point.x >= Mathf.Min(a.x, b.x) - EPS &&
               point.x <= Mathf.Max(a.x, b.x) + EPS &&
               point.y >= Mathf.Min(a.y, b.y) - EPS &&
               point.y <= Mathf.Max(a.y, b.y) + EPS;
    }

    static bool IsPointBetweenOnLine(Vector2 point, Vector2 a, Vector2 b)
    {
        if (Mathf.Abs(Cross(b - a, point - a)) > EPS)
            return false;

        return Vector2.Dot(point - a, point - b) <= EPS;
    }

    static bool IsSimplePolygon(List<Vector2> poly)
    {
        int count = poly.Count;
        if (count < 3)
            return false;

        for (int i = 0; i < count; i++)
        {
            Vector2 a1 = poly[i];
            Vector2 a2 = poly[(i + 1) % count];
            if (Vector2.SqrMagnitude(a2 - a1) <= EPS * EPS)
                return false;

            for (int j = i + 1; j < count; j++)
            {
                if (AreAdjacentEdges(i, j, count))
                    continue;

                Vector2 b1 = poly[j];
                Vector2 b2 = poly[(j + 1) % count];
                if (SegmentsIntersect(a1, a2, b1, b2))
                    return false;
            }
        }

        return true;
    }

    static bool AreAdjacentEdges(int firstEdgeIndex, int secondEdgeIndex, int edgeCount)
    {
        return firstEdgeIndex == secondEdgeIndex ||
               (firstEdgeIndex + 1) % edgeCount == secondEdgeIndex ||
               (secondEdgeIndex + 1) % edgeCount == firstEdgeIndex;
    }

    static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        float o1 = Cross(b - a, c - a);
        float o2 = Cross(b - a, d - a);
        float o3 = Cross(d - c, a - c);
        float o4 = Cross(d - c, b - c);

        if (((o1 > EPS && o2 < -EPS) || (o1 < -EPS && o2 > EPS)) &&
            ((o3 > EPS && o4 < -EPS) || (o3 < -EPS && o4 > EPS)))
        {
            return true;
        }

        return Mathf.Abs(o1) <= EPS && PointOnSegment(c, a, b) ||
               Mathf.Abs(o2) <= EPS && PointOnSegment(d, a, b) ||
               Mathf.Abs(o3) <= EPS && PointOnSegment(a, c, d) ||
               Mathf.Abs(o4) <= EPS && PointOnSegment(b, c, d);
    }

    static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    static List<int> EarClippingTriangulate(List<Vector2> poly)
    {
        List<int> indices = new List<int>();
        int n = poly.Count;
        if (n < 3) return indices;

        List<int> V = new List<int>();
        if (Area(poly) > 0)
        {
            for (int v = 0; v < n; v++) V.Add(v);
        }
        else
        {
            for (int v = 0; v < n; v++) V.Add((n - 1) - v);
        }

        int nv = n;
        int count = 2 * nv;
        for (int v = nv - 1; nv > 2;)
        {
            if ((count--) <= 0)
                break;

            int u = v; if (nv <= u) u = 0;
            v = u + 1; if (nv <= v) v = 0;
            int w = v + 1; if (nv <= w) w = 0;

            if (Snip(poly, V, u, v, w, nv))
            {
                int a = V[u], b = V[v], c = V[w];
                indices.Add(c);
                indices.Add(b);
                indices.Add(a);
                V.RemoveAt(v);
                nv--;
                count = 2 * nv;
            }
        }
        return indices;
    }

    static float Area(List<Vector2> poly)
    {
        int n = poly.Count;
        float area = 0f;
        for (int p = n - 1, q = 0; q < n; p = q++)
            area += poly[p].x * poly[q].y - poly[q].x * poly[p].y;

        return area * 0.5f;
    }

    static bool Snip(List<Vector2> poly, List<int> V, int u, int v, int w, int nv)
    {
        Vector2 A = poly[V[u]];
        Vector2 B = poly[V[v]];
        Vector2 C = poly[V[w]];
        if (Cross(B - A, C - A) <= EPS)
            return false;

        for (int p = 0; p < nv; p++)
        {
            if (p == u || p == v || p == w) continue;
            Vector2 P = poly[V[p]];
            if (PointInTriangle(P, A, B, C))
                return false;
        }
        return true;
    }

    static bool PointInTriangle(Vector2 P, Vector2 A, Vector2 B, Vector2 C)
    {
        float ax = C.x - B.x, ay = C.y - B.y;
        float bx = A.x - C.x, by = A.y - C.y;
        float cx = B.x - A.x, cy = B.y - A.y;
        float apx = P.x - A.x, apy = P.y - A.y;
        float bpx = P.x - B.x, bpy = P.y - B.y;
        float cpx = P.x - C.x, cpy = P.y - C.y;
        float aCROSSbp = ax * bpy - ay * bpx;
        float cCROSSap = cx * apy - cy * apx;
        float bCROSScp = bx * cpy - by * cpx;
        return (aCROSSbp >= 0f) && (bCROSScp >= 0f) && (cCROSSap >= 0f);
    }

    public bool IsPointOnBoundary(Vector2 point)
    {
        return Vertices != null && PointOnPolygonBoundary(point, Vertices);
    }

    public bool IsPointInPolygon(Vector2 point)
    {
        if (!boundsIndex.IsValid)
            boundsIndex.Rebuild(Vertices);

        if (!boundsIndex.Contains(point))
            return false;

        return PointInPolygon(point, Vertices);
    }
}
