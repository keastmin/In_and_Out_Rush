using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritoryTrailSegmentIndex
    {
        private const float ChunkSize = 8f;

        private readonly Dictionary<Vector2Int, List<Vector4>> _segmentsByChunk = new();
        private readonly HashSet<Vector4> _querySegments = new();

        public void Clear()
        {
            _segmentsByChunk.Clear();
            _querySegments.Clear();
        }

        public void Add(Vector2 start, Vector2 end)
        {
            if (Vector2.SqrMagnitude(end - start) <= 0.0001f)
                return;

            Vector4 segment = new(start.x, start.y, end.x, end.y);
            foreach (Vector2Int key in GetCoveredChunks(start, end))
            {
                if (!_segmentsByChunk.TryGetValue(key, out List<Vector4> segments))
                {
                    segments = new List<Vector4>();
                    _segmentsByChunk.Add(key, segments);
                }

                segments.Add(segment);
            }
        }

        public bool Intersects(Vector2 start, Vector2 end, Vector2 ignoredStart, Vector2 ignoredEnd)
        {
            _querySegments.Clear();
            foreach (Vector2Int key in GetCoveredChunks(start, end))
            {
                if (_segmentsByChunk.TryGetValue(key, out List<Vector4> segments))
                    _querySegments.UnionWith(segments);
            }

            Vector4 ignored = new(ignoredStart.x, ignoredStart.y, ignoredEnd.x, ignoredEnd.y);
            foreach (Vector4 segment in _querySegments)
            {
                if (segment == ignored)
                    continue;

                if (Geometry.SegmentIntersection(
                        start,
                        end,
                        new Vector2(segment.x, segment.y),
                        new Vector2(segment.z, segment.w),
                        true,
                        out _))
                    return true;
            }

            return false;
        }

        private static IEnumerable<Vector2Int> GetCoveredChunks(Vector2 start, Vector2 end)
        {
            int minimumX = Mathf.FloorToInt(Mathf.Min(start.x, end.x) / ChunkSize);
            int maximumX = Mathf.FloorToInt(Mathf.Max(start.x, end.x) / ChunkSize);
            int minimumY = Mathf.FloorToInt(Mathf.Min(start.y, end.y) / ChunkSize);
            int maximumY = Mathf.FloorToInt(Mathf.Max(start.y, end.y) / ChunkSize);

            for (int y = minimumY; y <= maximumY; y++)
            {
                for (int x = minimumX; x <= maximumX; x++)
                    yield return new Vector2Int(x, y);
            }
        }
    }
}
