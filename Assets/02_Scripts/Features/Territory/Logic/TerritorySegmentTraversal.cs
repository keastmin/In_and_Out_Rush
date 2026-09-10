using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public static class TerritorySegmentTraversal
    {
        public const float ChunkSize = 8f;
        private const float BoundaryOffset = 0.0001f;

        public static bool TryCollectChunks(
            Vector2 start,
            Vector2 end,
            List<Vector2Int> results,
            HashSet<Vector2Int> unique)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));
            if (unique == null)
                throw new ArgumentNullException(nameof(unique));

            results.Clear();
            unique.Clear();
            if (!IsFinite(start) || !IsFinite(end))
                return false;

            Traverse(start, end, results, unique);
            Vector2 delta = end - start;
            if (delta.sqrMagnitude > BoundaryOffset * BoundaryOffset)
            {
                Vector2 normal = new(-delta.y, delta.x);
                normal = normal.normalized * BoundaryOffset;
                Traverse(start + normal, end + normal, results, unique);
                Traverse(start - normal, end - normal, results, unique);
            }

            return true;
        }

        public static Vector2Int FromPoint(Vector2 point)
            => new(FloorToChunk(point.x), FloorToChunk(point.y));

        private static void Traverse(
            Vector2 start,
            Vector2 end,
            List<Vector2Int> results,
            HashSet<Vector2Int> unique)
        {
            Vector2Int current = FromPoint(start);
            Vector2Int target = FromPoint(end);
            Add(current, results, unique);
            if (current == target)
                return;

            double dx = end.x - start.x;
            double dy = end.y - start.y;
            int stepX = Math.Sign(dx);
            int stepY = Math.Sign(dy);
            double nextBoundaryX = stepX > 0
                ? (current.x + 1d) * ChunkSize
                : current.x * (double)ChunkSize;
            double nextBoundaryY = stepY > 0
                ? (current.y + 1d) * ChunkSize
                : current.y * (double)ChunkSize;
            double tMaxX = stepX == 0 ? double.PositiveInfinity : (nextBoundaryX - start.x) / dx;
            double tMaxY = stepY == 0 ? double.PositiveInfinity : (nextBoundaryY - start.y) / dy;
            double tDeltaX = stepX == 0 ? double.PositiveInfinity : ChunkSize / Math.Abs(dx);
            double tDeltaY = stepY == 0 ? double.PositiveInfinity : ChunkSize / Math.Abs(dy);
            long maximumSteps = Math.Abs((long)target.x - current.x) +
                                Math.Abs((long)target.y - current.y) + 2L;

            for (long step = 0; current != target && step <= maximumSteps; step++)
            {
                if (tMaxX < tMaxY)
                {
                    current.x += stepX;
                    tMaxX += tDeltaX;
                }
                else if (tMaxY < tMaxX)
                {
                    current.y += stepY;
                    tMaxY += tDeltaY;
                }
                else
                {
                    current.x += stepX;
                    current.y += stepY;
                    tMaxX += tDeltaX;
                    tMaxY += tDeltaY;
                }

                Add(current, results, unique);
            }
        }

        private static void Add(
            Vector2Int coordinate,
            List<Vector2Int> results,
            HashSet<Vector2Int> unique)
        {
            if (unique.Add(coordinate))
                results.Add(coordinate);
        }

        private static int FloorToChunk(float value)
        {
            double scaled = value / ChunkSize;
            if (double.IsNaN(scaled) || double.IsInfinity(scaled) ||
                scaled < int.MinValue || scaled >= int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return (int)Math.Floor(scaled);
        }

        private static bool IsFinite(Vector2 point)
            => !float.IsNaN(point.x) && !float.IsInfinity(point.x) &&
               !float.IsNaN(point.y) && !float.IsInfinity(point.y);
    }
}
