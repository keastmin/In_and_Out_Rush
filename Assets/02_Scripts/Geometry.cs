using System.Collections.Generic;
using UnityEngine;

public static class Geometry
{
    const float EPS = 1e-6f;

    static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    public static bool SegmentIntersection(
        Vector2 A,
        Vector2 B,
        Vector2 C,
        Vector2 D,
        bool includeEndpoints,
        out Vector2 intersection)
    {
        intersection = default;

        Vector2 r = B - A;
        Vector2 s = D - C;
        float rr = Vector2.Dot(r, r);
        float ss = Vector2.Dot(s, s);

        if (rr <= EPS * EPS && ss <= EPS * EPS)
        {
            if (!includeEndpoints || !AreSamePoint(A, C))
                return false;

            intersection = A;
            return true;
        }

        if (rr <= EPS * EPS)
        {
            if (!includeEndpoints || !PointOnSegment(A, C, D))
                return false;

            intersection = A;
            return true;
        }

        if (ss <= EPS * EPS)
        {
            if (!includeEndpoints || !PointOnSegment(C, A, B))
                return false;

            intersection = C;
            return true;
        }

        float rxs = Cross(r, s);
        Vector2 AC = C - A;
        float ACxr = Cross(AC, r);

        if (Mathf.Abs(rxs) <= EPS)
        {
            if (Mathf.Abs(ACxr) > EPS)
                return false;

            float t0 = Vector2.Dot(AC, r) / rr;
            float t1 = t0 + Vector2.Dot(s, r) / rr;
            float tmin = Mathf.Min(t0, t1);
            float tmax = Mathf.Max(t0, t1);
            float lower = includeEndpoints ? Mathf.Max(0f, tmin) : Mathf.Max(EPS, tmin);
            float upper = includeEndpoints ? Mathf.Min(1f, tmax) : Mathf.Min(1f - EPS, tmax);

            if (lower > upper)
                return false;

            intersection = A + Mathf.Clamp01(lower) * r;
            return true;
        }

        float t = Cross(AC, s) / rxs;
        float u = Cross(AC, r) / rxs;
        bool intersects = includeEndpoints
            ? t >= -EPS && t <= 1f + EPS && u >= -EPS && u <= 1f + EPS
            : t > EPS && t < 1f - EPS && u > EPS && u < 1f - EPS;

        if (!intersects)
            return false;

        intersection = A + Mathf.Clamp01(t) * r;
        return true;
    }

    public static bool PointOnSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        if (Vector2.SqrMagnitude(segment) <= EPS * EPS)
            return AreSamePoint(point, start);

        if (Mathf.Abs(Cross(segment, point - start)) > EPS)
            return false;

        return point.x >= Mathf.Min(start.x, end.x) - EPS &&
               point.x <= Mathf.Max(start.x, end.x) + EPS &&
               point.y >= Mathf.Min(start.y, end.y) - EPS &&
               point.y <= Mathf.Max(start.y, end.y) + EPS;
    }

    public static bool IsCircleOverlappingPolygon(
        Vector2 center,
        float radius,
        IReadOnlyList<Vector2> polygon)
    {
        if (polygon == null || polygon.Count < 3)
            return false;

        if (IsPointInPolygon(center, polygon))
            return true;

        float safeRadius = Mathf.Max(0f, radius);
        float radiusSqr = safeRadius * safeRadius;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 start = polygon[i];
            Vector2 end = polygon[(i + 1) % polygon.Count];
            if (GetPointToSegmentDistanceSqr(center, start, end) <= radiusSqr)
                return true;
        }

        return false;
    }

    public static bool IsPointInPolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
    {
        if (polygon == null || polygon.Count < 3)
            return false;

        bool inside = false;
        for (int i = 0, previous = polygon.Count - 1; i < polygon.Count; previous = i++)
        {
            Vector2 currentVertex = polygon[i];
            Vector2 previousVertex = polygon[previous];
            if (PointOnSegment(point, previousVertex, currentVertex))
                return true;

            bool crossesPointHeight =
                (currentVertex.y > point.y) != (previousVertex.y > point.y);
            if (!crossesPointHeight)
                continue;

            float intersectionX =
                (previousVertex.x - currentVertex.x) *
                (point.y - currentVertex.y) /
                (previousVertex.y - currentVertex.y) +
                currentVertex.x;
            if (point.x < intersectionX)
                inside = !inside;
        }

        return inside;
    }

    public static float GetPointToSegmentDistanceSqr(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float segmentLengthSqr = Vector2.Dot(segment, segment);
        if (segmentLengthSqr <= EPS * EPS)
            return Vector2.SqrMagnitude(point - start);

        float projection = Vector2.Dot(point - start, segment) / segmentLengthSqr;
        Vector2 closestPoint = start + segment * Mathf.Clamp01(projection);
        return Vector2.SqrMagnitude(point - closestPoint);
    }

    static bool AreSamePoint(Vector2 a, Vector2 b)
    {
        return Vector2.SqrMagnitude(a - b) <= EPS * EPS;
    }
}
