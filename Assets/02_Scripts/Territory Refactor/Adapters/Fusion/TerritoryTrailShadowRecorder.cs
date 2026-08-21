using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritoryTrailShadowRecorder
    {
        private readonly TerritoryTrailSession _session = new();
        private readonly List<FixedTerritoryPoint> _legacyFixedPoints = new();

        private ulong _nextSessionId = 1;
        private ulong _currentSessionId;
        private uint _nextSampleSequence;
        private bool _hasDomainSession;

        public bool IsRecording { get; private set; }
        public bool HasLastComparison { get; private set; }
        public ulong LastSessionId { get; private set; }
        public TerritoryTrailSessionStatus LastTerminalStatus { get; private set; }
        public TerritoryTrailShadowComparison LastComparison { get; private set; }
        public int LastFragmentCount { get; private set; }

        public bool TryBegin(out string reason)
        {
            if (IsRecording)
            {
                reason = "A shadow Trail session is already recording.";
                return false;
            }
            if (_nextSessionId == 0)
            {
                reason = "Shadow Trail SessionId exhausted UInt64 range.";
                return false;
            }

            _currentSessionId = _nextSessionId;
            _nextSessionId = unchecked(_nextSessionId + 1);
            _nextSampleSequence = 0;
            _hasDomainSession = false;
            IsRecording = true;
            HasLastComparison = false;
            LastSessionId = _currentSessionId;
            LastTerminalStatus = TerritoryTrailSessionStatus.Active;
            LastComparison = default;
            LastFragmentCount = 0;
            reason = null;
            return true;
        }

        public bool TryAppend(Vector2 point, int simulationTick, out string reason)
        {
            if (!IsRecording)
            {
                reason = "The shadow Trail session is not recording.";
                return false;
            }
            if (_nextSampleSequence == uint.MaxValue)
            {
                reason = "Shadow Trail sample sequence exhausted UInt32 range.";
                AbortFailedRecording();
                return false;
            }

            try
            {
                var sample = new TerritoryTrailSample(
                    _currentSessionId,
                    _nextSampleSequence,
                    simulationTick,
                    FixedTerritoryPoint.FromWorld(point.x, point.y));

                bool accepted = _hasDomainSession
                    ? _session.TryAppendSample(sample, out reason)
                    : _session.TryBegin(sample, out reason);
                if (!accepted)
                {
                    AbortFailedRecording();
                    return false;
                }

                _hasDomainSession = true;
                _nextSampleSequence++;
                return true;
            }
            catch (Exception exception)
            {
                reason = exception.Message;
                AbortFailedRecording();
                return false;
            }
        }

        public bool TryCommit(
            IReadOnlyList<Vector2> legacyPoints,
            out TerritoryTrailShadowComparison comparison,
            out string reason)
        {
            comparison = default;
            if (legacyPoints == null)
            {
                reason = "Legacy Trail points cannot be null.";
                AbortFailedRecording();
                return false;
            }
            if (!IsRecording || !_hasDomainSession)
            {
                reason = "The shadow Trail session has no samples to commit.";
                AbortFailedRecording();
                return false;
            }

            try
            {
                _legacyFixedPoints.Clear();
                for (int index = 0; index < legacyPoints.Count; index++)
                {
                    Vector2 point = legacyPoints[index];
                    _legacyFixedPoints.Add(FixedTerritoryPoint.FromWorld(point.x, point.y));
                }

                if (!_session.TryCommit(_currentSessionId, out reason))
                {
                    AbortFailedRecording();
                    return false;
                }

                comparison = TerritoryTrailShadowComparer.Compare(
                    _legacyFixedPoints,
                    _session.Samples);
                LastComparison = comparison;
                LastFragmentCount = _session.Fragments.Count;
                HasLastComparison = true;
                LastTerminalStatus = TerritoryTrailSessionStatus.Committed;
                IsRecording = false;
                _hasDomainSession = false;
                reason = null;
                return true;
            }
            catch (Exception exception)
            {
                reason = exception.Message;
                AbortFailedRecording();
                return false;
            }
        }

        public bool TryAbort(out string reason)
        {
            if (!IsRecording)
            {
                reason = "The shadow Trail session is not recording.";
                return false;
            }

            if (_hasDomainSession && !_session.TryAbort(_currentSessionId, out reason))
            {
                AbortFailedRecording();
                return false;
            }

            FinishAbort();
            reason = null;
            return true;
        }

        private void AbortFailedRecording()
        {
            if (_hasDomainSession &&
                _session.Status == TerritoryTrailSessionStatus.Active &&
                _session.SessionId == _currentSessionId)
            {
                _session.TryAbort(_currentSessionId, out _);
            }

            FinishAbort();
        }

        private void FinishAbort()
        {
            IsRecording = false;
            _hasDomainSession = false;
            HasLastComparison = false;
            LastTerminalStatus = TerritoryTrailSessionStatus.Aborted;
            LastComparison = default;
            LastFragmentCount = 0;
            _legacyFixedPoints.Clear();
        }
    }
}
