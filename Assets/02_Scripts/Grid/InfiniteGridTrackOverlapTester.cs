using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class InfiniteGridTrackOverlapTester
    {
        public bool IsOverlapping(
            Vector3 cellCenter,
            IReadOnlyList<Vector3> trackVertices,
            float overlapRadius)
        {
            if (trackVertices == null || trackVertices.Count < 2)
            {
                return false;
            }

            Vector2 center = new Vector2(cellCenter.x, cellCenter.z);
            float safeRadius = Mathf.Max(0f, overlapRadius);
            float overlapRadiusSqr = safeRadius * safeRadius;

            for (int i = 0; i < trackVertices.Count; i++)
            {
                Vector3 startVertex = trackVertices[i];
                Vector3 endVertex = trackVertices[(i + 1) % trackVertices.Count];
                Vector2 start = new Vector2(startVertex.x, startVertex.z);
                Vector2 end = new Vector2(endVertex.x, endVertex.z);

                if (GetDistanceToSegmentSqr(center, start, end) <= overlapRadiusSqr)
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetDistanceToSegmentSqr(
            Vector2 point,
            Vector2 start,
            Vector2 end)
        {
            Vector2 segment = end - start;
            float segmentLengthSqr = segment.sqrMagnitude;
            if (segmentLengthSqr <= Mathf.Epsilon)
            {
                return (point - start).sqrMagnitude;
            }

            float t = Mathf.Clamp01(
                Vector2.Dot(point - start, segment) / segmentLengthSqr);
            Vector2 closestPoint = start + segment * t;
            return (point - closestPoint).sqrMagnitude;
        }
    }
}
