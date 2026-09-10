using System.Collections.Generic;
using ProjectIO.Tracks;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class InfiniteGridTrackOverlapTester
    {
        private const float Sqrt3 = 1.7320508075688772f;
        private const float GeometryTolerance = 0.00001f;

        private static readonly Vector2[] NormalizedHexVertices =
        {
            new(1f, 0f),
            new(0.5f, Sqrt3 * 0.5f),
            new(-0.5f, Sqrt3 * 0.5f),
            new(-1f, 0f),
            new(-0.5f, -Sqrt3 * 0.5f),
            new(0.5f, -Sqrt3 * 0.5f)
        };

        public bool IsOverlapping(
            Vector3 cellCenter,
            float cellSize,
            IReadOnlyList<TrackSegment> trackSegments,
            float overlapRadius)
        {
            if (trackSegments == null || trackSegments.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < trackSegments.Count; i++)
            {
                if (IsSegmentOverlapping(
                        cellCenter,
                        cellSize,
                        trackSegments[i].Start,
                        trackSegments[i].End,
                        overlapRadius))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsSegmentOverlapping(
            Vector3 cellCenter,
            float cellSize,
            Vector3 trackStart,
            Vector3 trackEnd,
            float overlapRadius)
        {
            float safeCellSize = Mathf.Max(0f, cellSize);
            float safeRadius = Mathf.Max(0f, overlapRadius);
            float scale = Mathf.Max(1f, safeCellSize + safeRadius);
            float linearTolerance = GeometryTolerance * scale;
            float maximumDistance = safeRadius + linearTolerance;
            float maximumDistanceSqr = maximumDistance * maximumDistance;
            Vector2 center = new Vector2(0f, 0f);
            Vector2 start = new Vector2(
                trackStart.x - cellCenter.x,
                trackStart.z - cellCenter.z);
            Vector2 end = new Vector2(
                trackEnd.x - cellCenter.x,
                trackEnd.z - cellCenter.z);

            if (!CanSegmentReachHex(
                    center,
                    safeCellSize,
                    start,
                    end,
                    maximumDistance))
            {
                return false;
            }

            if (safeCellSize <= Mathf.Epsilon)
            {
                return GetPointToSegmentDistanceSqr(center, start, end) <=
                       maximumDistanceSqr;
            }

            if (IsPointInsideHex(start, center, safeCellSize) ||
                IsPointInsideHex(end, center, safeCellSize))
            {
                return true;
            }

            for (int i = 0; i < NormalizedHexVertices.Length; i++)
            {
                Vector2 edgeStart =
                    center + NormalizedHexVertices[i] * safeCellSize;
                Vector2 edgeEnd =
                    center +
                    NormalizedHexVertices[(i + 1) % NormalizedHexVertices.Length] *
                    safeCellSize;

                if (GetSegmentToSegmentDistanceSqr(
                        start,
                        end,
                        edgeStart,
                        edgeEnd) <= maximumDistanceSqr)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanSegmentReachHex(
            Vector2 center,
            float cellSize,
            Vector2 segmentStart,
            Vector2 segmentEnd,
            float overlapRadius)
        {
            float halfHeight = cellSize * (Sqrt3 * 0.5f);
            float minimumX = center.x - cellSize - overlapRadius;
            float maximumX = center.x + cellSize + overlapRadius;
            float minimumY = center.y - halfHeight - overlapRadius;
            float maximumY = center.y + halfHeight + overlapRadius;

            return Mathf.Max(segmentStart.x, segmentEnd.x) >= minimumX &&
                   Mathf.Min(segmentStart.x, segmentEnd.x) <= maximumX &&
                   Mathf.Max(segmentStart.y, segmentEnd.y) >= minimumY &&
                   Mathf.Min(segmentStart.y, segmentEnd.y) <= maximumY;
        }

        private static bool IsPointInsideHex(
            Vector2 point,
            Vector2 center,
            float cellSize)
        {
            Vector2 local = point - center;
            float absoluteX = Mathf.Abs(local.x);
            float absoluteY = Mathf.Abs(local.y);
            float halfHeight = cellSize * (Sqrt3 * 0.5f);
            float tolerance = GeometryTolerance * Mathf.Max(1f, cellSize);

            return absoluteX <= cellSize + tolerance &&
                   absoluteY <= halfHeight + tolerance &&
                   absoluteY + Sqrt3 * absoluteX <=
                   Sqrt3 * cellSize + tolerance;
        }

        private static float GetPointToSegmentDistanceSqr(
            Vector2 point,
            Vector2 segmentStart,
            Vector2 segmentEnd)
        {
            Vector2 segment = segmentEnd - segmentStart;
            float segmentLengthSqr = segment.sqrMagnitude;
            if (segmentLengthSqr <= Mathf.Epsilon)
            {
                return (point - segmentStart).sqrMagnitude;
            }

            float t = Mathf.Clamp01(
                Vector2.Dot(point - segmentStart, segment) / segmentLengthSqr);
            Vector2 closestPoint = segmentStart + segment * t;
            return (point - closestPoint).sqrMagnitude;
        }

        private static float GetSegmentToSegmentDistanceSqr(
            Vector2 firstStart,
            Vector2 firstEnd,
            Vector2 secondStart,
            Vector2 secondEnd)
        {
            Vector2 firstDirection = firstEnd - firstStart;
            Vector2 secondDirection = secondEnd - secondStart;
            Vector2 offset = firstStart - secondStart;
            float firstLengthSqr = firstDirection.sqrMagnitude;
            float secondLengthSqr = secondDirection.sqrMagnitude;
            float secondProjection = Vector2.Dot(secondDirection, offset);
            float firstT;
            float secondT;

            if (firstLengthSqr <= Mathf.Epsilon &&
                secondLengthSqr <= Mathf.Epsilon)
            {
                return offset.sqrMagnitude;
            }

            if (firstLengthSqr <= Mathf.Epsilon)
            {
                firstT = 0f;
                secondT = Mathf.Clamp01(secondProjection / secondLengthSqr);
            }
            else
            {
                float firstProjection = Vector2.Dot(firstDirection, offset);
                if (secondLengthSqr <= Mathf.Epsilon)
                {
                    secondT = 0f;
                    firstT = Mathf.Clamp01(-firstProjection / firstLengthSqr);
                }
                else
                {
                    float directionDot =
                        Vector2.Dot(firstDirection, secondDirection);
                    float denominator =
                        firstLengthSqr * secondLengthSqr -
                        directionDot * directionDot;

                    firstT = denominator > Mathf.Epsilon
                        ? Mathf.Clamp01(
                            (directionDot * secondProjection -
                             firstProjection * secondLengthSqr) /
                            denominator)
                        : 0f;

                    float secondNumerator =
                        directionDot * firstT + secondProjection;
                    if (secondNumerator < 0f)
                    {
                        secondT = 0f;
                        firstT =
                            Mathf.Clamp01(-firstProjection / firstLengthSqr);
                    }
                    else if (secondNumerator > secondLengthSqr)
                    {
                        secondT = 1f;
                        firstT = Mathf.Clamp01(
                            (directionDot - firstProjection) /
                            firstLengthSqr);
                    }
                    else
                    {
                        secondT = secondNumerator / secondLengthSqr;
                    }
                }
            }

            Vector2 firstClosest = firstStart + firstDirection * firstT;
            Vector2 secondClosest = secondStart + secondDirection * secondT;
            return (firstClosest - secondClosest).sqrMagnitude;
        }
    }
}
