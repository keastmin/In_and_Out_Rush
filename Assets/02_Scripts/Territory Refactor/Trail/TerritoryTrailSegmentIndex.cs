using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritoryTrailSegmentIndex
    {
        private readonly Dictionary<Vector2Int, List<Vector4>> _segmentsByChunk = new();
        private readonly HashSet<Vector4> _querySegments = new();
        private readonly List<Vector2Int> _coveredChunks = new();
        private readonly HashSet<Vector2Int> _uniqueChunks = new();

        public void Clear()
        {
            _segmentsByChunk.Clear();
            _querySegments.Clear();
            _coveredChunks.Clear();
            _uniqueChunks.Clear();
        }

        public void Add(Vector2 start, Vector2 end)
        {
            if (Vector2.SqrMagnitude(end - start) <= 0.0001f)
                return;

            Vector4 segment = new(start.x, start.y, end.x, end.y);
            if (!TerritorySegmentTraversal.TryCollectChunks(
                    start,
                    end,
                    _coveredChunks,
                    _uniqueChunks))
                return;

            for (int index = 0; index < _coveredChunks.Count; index++)
            {
                Vector2Int key = _coveredChunks[index];
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
            if (!TerritorySegmentTraversal.TryCollectChunks(
                    start,
                    end,
                    _coveredChunks,
                    _uniqueChunks))
                return false;

            for (int index = 0; index < _coveredChunks.Count; index++)
            {
                Vector2Int key = _coveredChunks[index];
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
    }
}
