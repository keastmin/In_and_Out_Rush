using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Tracks
{
    public static class TrackGeometryGenerator
    {
        public static IReadOnlyList<TrackPath> CreatePaths(
            TrackStage stage,
            Vector3 center,
            int ellipseVertexCount,
            float horizontalRadius,
            float verticalRadius,
            float ellipseNoise,
            int ellipseSeed,
            float lineLength,
            TrackAxis primaryAxis,
            bool reversePrimary,
            bool reverseSecondary)
        {
            if (stage == TrackStage.InitialEllipse)
            {
                return new[]
                {
                    CreateEllipse(
                        center,
                        ellipseVertexCount,
                        horizontalRadius,
                        verticalRadius,
                        ellipseNoise,
                        ellipseSeed)
                };
            }

            TrackPath primaryPath = CreateLine(center, lineLength, primaryAxis, reversePrimary);
            if (stage == TrackStage.PrimaryLine)
            {
                return new[] { primaryPath };
            }

            TrackPath secondaryPath = CreateLine(
                center,
                lineLength,
                GetPerpendicularAxis(primaryAxis),
                reverseSecondary);
            return new[] { primaryPath, secondaryPath };
        }

        public static TrackPath CreateEllipse(
            Vector3 center,
            int vertexCount,
            float horizontalRadius,
            float verticalRadius,
            float noise,
            int seed)
        {
            int safeVertexCount = Mathf.Max(3, vertexCount);
            float safeHorizontalRadius = Mathf.Max(0f, horizontalRadius);
            float safeVerticalRadius = Mathf.Max(0f, verticalRadius);
            float safeNoise = Mathf.Max(0f, noise);
            var random = new global::System.Random(seed);
            var vertices = new Vector3[safeVertexCount];

            for (int i = 0; i < safeVertexCount; i++)
            {
                float angle = Mathf.PI + 2f * Mathf.PI * i / safeVertexCount;
                vertices[i] = center + new Vector3(
                    safeHorizontalRadius * Mathf.Cos(angle) + RandomRange(random, -safeNoise, safeNoise),
                    0f,
                    safeVerticalRadius * Mathf.Sin(angle) + RandomRange(random, -safeNoise, safeNoise));
            }

            return new TrackPath(vertices, true);
        }

        public static TrackPath CreateLine(Vector3 center, float length, TrackAxis axis, bool reversed)
        {
            Vector3 halfOffset = GetDirection(axis) * (Mathf.Max(0f, length) * 0.5f);
            Vector3 start = center - halfOffset;
            Vector3 end = center + halfOffset;
            return reversed
                ? new TrackPath(new[] { end, start }, false)
                : new TrackPath(new[] { start, end }, false);
        }

        public static IReadOnlyList<TrackSegment> CreateSegments(IReadOnlyList<TrackPath> paths)
        {
            var segments = new List<TrackSegment>();
            if (paths == null)
            {
                return segments;
            }

            for (int pathIndex = 0; pathIndex < paths.Count; pathIndex++)
            {
                TrackPath path = paths[pathIndex];
                if (path == null || path.Vertices == null || path.Vertices.Length < 2)
                {
                    continue;
                }

                for (int vertexIndex = 0; vertexIndex < path.Vertices.Length - 1; vertexIndex++)
                {
                    segments.Add(new TrackSegment(path.Vertices[vertexIndex], path.Vertices[vertexIndex + 1]));
                }

                if (path.IsClosed)
                {
                    segments.Add(new TrackSegment(path.Vertices[path.Vertices.Length - 1], path.Vertices[0]));
                }
            }

            return segments;
        }

        public static TrackAxis GetPerpendicularAxis(TrackAxis axis)
        {
            switch (axis)
            {
                case TrackAxis.Horizontal:
                    return TrackAxis.Vertical;
                case TrackAxis.Vertical:
                    return TrackAxis.Horizontal;
                case TrackAxis.DiagonalPositive:
                    return TrackAxis.DiagonalNegative;
                default:
                    return TrackAxis.DiagonalPositive;
            }
        }

        public static Vector3 GetDirection(TrackAxis axis)
        {
            switch (axis)
            {
                case TrackAxis.Horizontal:
                    return Vector3.right;
                case TrackAxis.Vertical:
                    return Vector3.forward;
                case TrackAxis.DiagonalPositive:
                    return new Vector3(1f, 0f, 1f).normalized;
                default:
                    return new Vector3(1f, 0f, -1f).normalized;
            }
        }

        private static float RandomRange(global::System.Random random, float minInclusive, float maxInclusive)
        {
            return minInclusive + (float)random.NextDouble() * (maxInclusive - minInclusive);
        }
    }
}
