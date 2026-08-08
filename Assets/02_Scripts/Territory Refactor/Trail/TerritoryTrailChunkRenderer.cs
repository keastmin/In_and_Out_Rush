using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritoryTrailChunkRenderer : MonoBehaviour
    {
        private const float ChunkSize = 8f;

        private readonly List<LineRenderer> _segments = new();

        private LineRenderer _template;
        private LineRenderer _activeSegment;
        private Vector2Int _activeChunk;
        private bool _hasActiveChunk;
        private Vector2 _lastPoint;
        private bool _hasLastPoint;

        public void Initialize(LineRenderer template)
        {
            _template = template;
            if (_template != null)
            {
                _template.positionCount = 0;
                _template.enabled = false;
            }
        }

        public void Begin()
        {
            Clear();
        }

        public void Append(Vector2 point)
        {
            if (_template == null)
                return;

            Vector2Int chunk = ToChunk(point);
            if (!_hasActiveChunk || chunk != _activeChunk)
            {
                Vector2 boundaryPoint = _hasLastPoint
                    ? GetChunkBoundaryPoint(_lastPoint, point, _activeChunk)
                    : point;
                if (_hasLastPoint)
                    AppendToActiveSegment(boundaryPoint);

                StartSegment(chunk);
                if (_hasLastPoint)
                    AppendToActiveSegment(boundaryPoint);
            }

            AppendToActiveSegment(point);
            _lastPoint = point;
            _hasLastPoint = true;
        }

        public void Clear()
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i] != null)
                    Destroy(_segments[i].gameObject);
            }

            _segments.Clear();
            _activeSegment = null;
            _hasActiveChunk = false;
            _hasLastPoint = false;
        }

        private void StartSegment(Vector2Int chunk)
        {
            var segmentObject = new GameObject($"Trail Chunk {chunk.x}, {chunk.y}");
            segmentObject.transform.SetParent(transform, false);
            LineRenderer segment = segmentObject.AddComponent<LineRenderer>();
            segment.positionCount = 0;
            CopyStyle(_template, segment);
            _segments.Add(segment);
            _activeSegment = segment;
            _activeChunk = chunk;
            _hasActiveChunk = true;
        }

        private void AppendToActiveSegment(Vector2 point)
        {
            int index = _activeSegment.positionCount;
            _activeSegment.positionCount = index + 1;
            _activeSegment.SetPosition(index, new Vector3(point.x, 0f, point.y));
        }

        private static Vector2Int ToChunk(Vector2 point)
        {
            return new Vector2Int(
                Mathf.FloorToInt(point.x / ChunkSize),
                Mathf.FloorToInt(point.y / ChunkSize));
        }

        private static Vector2 GetChunkBoundaryPoint(
            Vector2 start,
            Vector2 end,
            Vector2Int chunk)
        {
            Vector2 direction = end - start;
            float boundaryT = 1f;

            if (Mathf.Abs(direction.x) > Mathf.Epsilon)
            {
                float boundaryX = direction.x > 0f
                    ? (chunk.x + 1) * ChunkSize
                    : chunk.x * ChunkSize;
                float t = (boundaryX - start.x) / direction.x;
                if (t > 0f && t < boundaryT)
                    boundaryT = t;
            }

            if (Mathf.Abs(direction.y) > Mathf.Epsilon)
            {
                float boundaryY = direction.y > 0f
                    ? (chunk.y + 1) * ChunkSize
                    : chunk.y * ChunkSize;
                float t = (boundaryY - start.y) / direction.y;
                if (t > 0f && t < boundaryT)
                    boundaryT = t;
            }

            return Vector2.Lerp(start, end, boundaryT);
        }

        private static void CopyStyle(LineRenderer source, LineRenderer target)
        {
            target.sharedMaterial = source.sharedMaterial;
            target.widthCurve = source.widthCurve;
            target.widthMultiplier = source.widthMultiplier;
            target.colorGradient = source.colorGradient;
            target.numCapVertices = source.numCapVertices;
            target.numCornerVertices = source.numCornerVertices;
            target.alignment = source.alignment;
            target.textureMode = source.textureMode;
            target.useWorldSpace = true;
            target.shadowCastingMode = source.shadowCastingMode;
            target.receiveShadows = source.receiveShadows;
        }
    }
}
