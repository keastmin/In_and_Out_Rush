using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritoryExpansionSession
    {
        private const float CalculationPointDistanceSqr = 0.04f;
        private const float CalculationTurnDotThreshold = 0.995f;
        private const float CalculationTurnLateralDistance = 0.08f;

        private readonly List<Vector2> _playerPath = new();
        private readonly List<Vector2> _calculationPath = new();
        private readonly TerritoryTrailSegmentIndex _segmentIndex = new();

        public bool IsExpanding { get; private set; }
        public bool IsRecoveringFromLifeline { get; private set; }
        public bool IsIntersected { get; private set; }
        public Vector2 PreviousPosition { get; private set; }
        public int PlayerPathCount => _playerPath.Count;
        public int CalculationPathCount => _calculationPath.Count;

        public void Begin()
        {
            IsExpanding = true;
            ClearPath();
        }

        public void Stop()
        {
            ClearPath();
            IsExpanding = false;
        }

        public void Reset(Vector2 safePosition)
        {
            Stop();
            PreviousPosition = safePosition;
            IsIntersected = false;
        }

        public void BeginLifelineRecovery(Vector2 safePosition)
        {
            Reset(safePosition);
            IsRecoveringFromLifeline = true;
        }

        public bool TryFinishLifelineRecovery(bool isInTerritory, Vector2 currentPosition)
        {
            if (!IsRecoveringFromLifeline || !isInTerritory)
                return false;

            Reset(currentPosition);
            IsRecoveringFromLifeline = false;
            return true;
        }

        public void SetPreviousPosition(Vector2 position)
            => PreviousPosition = position;

        public void AppendPathPoint(Vector2 point, bool forceCalculationPoint = false)
        {
            if (_playerPath.Count > 0)
                _segmentIndex.Add(_playerPath[^1], point);

            _playerPath.Add(point);
            AddCalculationPathPoint(point, forceCalculationPoint);
        }

        public bool HasMovedEnough(Vector2 currentPosition, float minimumMoveDistanceSqr)
            => Vector2.SqrMagnitude(currentPosition - PreviousPosition) > minimumMoveDistanceSqr;

        public bool CrossesOwnPath(Vector2 currentPosition)
        {
            if (_playerPath.Count < 2 ||
                Vector2.SqrMagnitude(currentPosition - PreviousPosition) <= 0.0001f)
                return false;

            return _segmentIndex.Intersects(
                currentPosition,
                PreviousPosition,
                _playerPath[^2],
                _playerPath[^1]);
        }

        public bool TryExpand(global::Territory territory, out TerritoryMeshData meshData)
        {
            if (territory != null)
                return territory.TryExpand(_calculationPath, out meshData);

            meshData = null;
            return false;
        }

        public void MarkIntersected()
            => IsIntersected = true;

        public bool TryCopyPathTo(List<Vector3> results)
        {
            if (results == null)
                return false;

            results.Clear();
            if (!IsExpanding || _playerPath.Count < 2)
                return false;

            for (int i = 0; i < _playerPath.Count; i++)
            {
                Vector2 point = _playerPath[i];
                results.Add(new Vector3(point.x, 0f, point.y));
            }

            return true;
        }

        public bool TryCopyPathTo(List<Vector2> results)
        {
            if (results == null)
                return false;

            results.Clear();
            if (!IsExpanding || _playerPath.Count < 2)
                return false;

            results.AddRange(_playerPath);
            return true;
        }

        private void ClearPath()
        {
            _playerPath.Clear();
            _calculationPath.Clear();
            _segmentIndex.Clear();
        }

        private void AddCalculationPathPoint(Vector2 point, bool force)
        {
            int count = _calculationPath.Count;
            if (force || count < 2)
            {
                _calculationPath.Add(point);
                return;
            }

            Vector2 lastPoint = _calculationPath[^1];
            if (Vector2.SqrMagnitude(point - lastPoint) >= CalculationPointDistanceSqr)
            {
                _calculationPath.Add(point);
                return;
            }

            Vector2 previousPoint = _calculationPath[^2];
            Vector2 previousSegment = lastPoint - previousPoint;
            Vector2 currentSegment = point - lastPoint;
            if (Vector2.SqrMagnitude(previousSegment) <= 0.0001f ||
                Vector2.SqrMagnitude(currentSegment) <= 0.0001f)
                return;

            float directionDot = Vector2.Dot(previousSegment.normalized, currentSegment.normalized);
            if (directionDot >= CalculationTurnDotThreshold)
                return;

            float lateralDistance = Mathf.Abs(
                previousSegment.x * (lastPoint.y - point.y) -
                (previousPoint.x - point.x) * previousSegment.y) /
                Mathf.Sqrt(Vector2.SqrMagnitude(previousSegment));
            if (lateralDistance >= CalculationTurnLateralDistance)
                _calculationPath.Add(point);
        }
    }
}
