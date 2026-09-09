using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritoryTrailChunkRenderer : MonoBehaviour
    {
        private const int MaximumPointsPerSegment = 256;

        private readonly TerritoryAppendOnlyBlockList<LineRenderer> _segments = new();
        private readonly List<TerritorySegmentChunkTraversal.SegmentPart> _segmentParts = new();

        private LineRenderer _template;
        private LineRenderer _activeSegment;
        private LineRenderer _liveHeadRenderer;
        private int _activeSegmentCount;
        private TerritoryChunkCoordinate _activeChunk;
        private FixedTerritoryPoint _lastPoint;
        private FixedTerritoryPoint _lastSegmentPoint;
        private FixedTerritoryPoint _liveHeadPoint;
        private bool _hasActiveChunk;
        private bool _hasLastPoint;
        private bool _hasLastSegmentPoint;
        private bool _hasLiveHead;

        public bool HasPoint => _hasLastPoint;
        public FixedTerritoryPoint LastPoint => _lastPoint;

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
            Append(FixedTerritoryPoint.FromWorld(point.x, point.y));
        }

        public void Append(FixedTerritoryPoint point)
        {
            if (_template == null || (_hasLastPoint && point == _lastPoint))
                return;

            if (!_hasLastPoint)
            {
                StartSegment(TerritoryChunkCoordinate.FromPoint(point));
                AppendToActiveSegment(point);
                _lastPoint = point;
                _hasLastPoint = true;
                RefreshLiveHead();
                return;
            }

            TerritorySegmentChunkTraversal.Split(_lastPoint, point, _segmentParts);
            for (int index = 0; index < _segmentParts.Count; index++)
            {
                TerritorySegmentChunkTraversal.SegmentPart part = _segmentParts[index];
                if (!_hasActiveChunk || part.Chunk != _activeChunk)
                {
                    StartSegment(part.Chunk);
                    AppendToActiveSegment(part.Start);
                }
                else
                {
                    AppendToActiveSegment(part.Start);
                }

                AppendToActiveSegment(part.End);
            }

            _lastPoint = point;
            _hasLastPoint = true;
            RefreshLiveHead();
        }

        public void Rebuild(IReadOnlyList<FixedTerritoryPoint> points)
        {
            Clear();
            if (points == null)
                return;

            for (int index = 0; index < points.Count; index++)
                Append(points[index]);
        }

        public void SetLiveHead(FixedTerritoryPoint point)
        {
            _liveHeadPoint = point;
            _hasLiveHead = true;
            RefreshLiveHead();
        }

        public void ClearLiveHead()
        {
            _hasLiveHead = false;
            if (_liveHeadRenderer != null)
            {
                _liveHeadRenderer.positionCount = 0;
                _liveHeadRenderer.gameObject.SetActive(false);
            }
        }

        public void Clear()
        {
            for (int index = 0; index < _activeSegmentCount; index++)
            {
                if (_segments[index] != null)
                {
                    _segments[index].positionCount = 0;
                    _segments[index].gameObject.SetActive(false);
                }
            }

            _activeSegmentCount = 0;
            _activeSegment = null;
            _hasActiveChunk = false;
            _hasLastPoint = false;
            _hasLastSegmentPoint = false;
            ClearLiveHead();
        }

        private void StartSegment(TerritoryChunkCoordinate chunk)
        {
            LineRenderer segment;
            if (_activeSegmentCount < _segments.Count)
            {
                segment = _segments[_activeSegmentCount];
                segment.gameObject.SetActive(true);
                segment.positionCount = 0;
            }
            else
            {
                var segmentObject = new GameObject($"Trail Chunk {chunk.X}, {chunk.Y}");
                segmentObject.transform.SetParent(transform, false);
                segment = segmentObject.AddComponent<LineRenderer>();
                segment.positionCount = 0;
                CopyStyle(_template, segment);
                _segments.Add(segment);
            }

            _activeSegmentCount++;
            _activeSegment = segment;
            _activeChunk = chunk;
            _hasActiveChunk = true;
            _hasLastSegmentPoint = false;
        }

        private void AppendToActiveSegment(FixedTerritoryPoint point)
        {
            if (_activeSegment == null || (_hasLastSegmentPoint && point == _lastSegmentPoint))
                return;

            if (_activeSegment.positionCount >= MaximumPointsPerSegment && _hasLastSegmentPoint)
            {
                FixedTerritoryPoint continuationPoint = _lastSegmentPoint;
                TerritoryChunkCoordinate continuationChunk = _activeChunk;
                StartSegment(continuationChunk);
                AppendToActiveSegment(continuationPoint);
            }

            int index = _activeSegment.positionCount;
            _activeSegment.positionCount = index + 1;
            _activeSegment.SetPosition(index, ToWorld(point));
            _lastSegmentPoint = point;
            _hasLastSegmentPoint = true;
        }

        private void RefreshLiveHead()
        {
            if (!_hasLiveHead || !_hasLastPoint || _liveHeadPoint == _lastPoint || _template == null)
            {
                if (_liveHeadRenderer != null)
                {
                    _liveHeadRenderer.positionCount = 0;
                    _liveHeadRenderer.gameObject.SetActive(false);
                }
                return;
            }

            EnsureLiveHeadRenderer();
            _liveHeadRenderer.gameObject.SetActive(true);
            _liveHeadRenderer.positionCount = 2;
            _liveHeadRenderer.SetPosition(0, ToWorld(_lastPoint));
            _liveHeadRenderer.SetPosition(1, ToWorld(_liveHeadPoint));
        }

        private void EnsureLiveHeadRenderer()
        {
            if (_liveHeadRenderer != null)
                return;

            var liveHeadObject = new GameObject("Trail Live Head");
            liveHeadObject.transform.SetParent(transform, false);
            _liveHeadRenderer = liveHeadObject.AddComponent<LineRenderer>();
            CopyStyle(_template, _liveHeadRenderer);
            _liveHeadRenderer.positionCount = 0;
        }

        private static Vector3 ToWorld(FixedTerritoryPoint point)
            => new((float)point.WorldX, 0f, (float)point.WorldY);

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
