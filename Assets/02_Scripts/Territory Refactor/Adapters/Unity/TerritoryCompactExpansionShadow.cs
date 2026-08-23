using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryCompactExpansionShadow : IDisposable
    {
        private TerritoryCompactStore _store;
        private TerritoryCompactExpansionWorker _worker;
        private List<TerritoryTrailFragment> _activeFragments;
        private ulong _activeSessionId;
        private ulong _nextScheduledSourceRevision;
        private int _nextDrainIndex;
        private bool _disabled;

        public bool IsInitialized => _store != null && _worker != null && !_disabled;
        public bool IsCollecting => _activeFragments != null;
        public ulong PublishedRevision => _store?.Current.Revision ?? 0UL;
        public int PendingWorkCount => _worker?.PendingCount ?? 0;
        public TerritoryCompactExpansionWorkerMetrics LastMetrics { get; private set; }
        public string LastFailureReason { get; private set; }

        public bool TryInitialize(TerritoryChunkSnapshot initialSnapshot, out string reason)
        {
            if (initialSnapshot == null)
            {
                reason = "Compact expansion shadow initialization requires a C006 snapshot.";
                return false;
            }
            if (_store != null || _worker != null)
            {
                reason = "Compact expansion shadow is already initialized.";
                return false;
            }

            var builder = new TerritoryCompactSnapshotBuilder();
            if (!builder.TryBuild(initialSnapshot, out TerritoryCompactSnapshot compact, out reason))
                return false;

            _store = new TerritoryCompactStore(compact);
            _worker = new TerritoryCompactExpansionWorker(compact);
            _nextScheduledSourceRevision = compact.Revision;
            _disabled = false;
            LastFailureReason = null;
            return true;
        }

        public bool TryBeginTrail(ulong sessionId, out string reason)
        {
            if (!IsInitialized)
            {
                reason = LastFailureReason ?? "Compact expansion shadow is not initialized.";
                return false;
            }
            if (sessionId == 0)
            {
                reason = "Compact expansion shadow requires a non-zero Trail SessionId.";
                return false;
            }
            if (_activeFragments != null)
            {
                reason = "Compact expansion shadow is already collecting a Trail.";
                return false;
            }

            _activeFragments = new List<TerritoryTrailFragment>();
            _activeSessionId = sessionId;
            _nextDrainIndex = 0;
            reason = null;
            return true;
        }

        public bool TryDrainFragments(
            IReadOnlyList<TerritoryTrailFragment> confirmedFragments,
            out string reason)
        {
            if (_activeFragments == null)
            {
                reason = "Compact expansion shadow has no active Trail collection.";
                return false;
            }
            if (confirmedFragments == null || confirmedFragments.Count < _nextDrainIndex)
            {
                reason = "Confirmed Trail fragments were reset before the active collection completed.";
                return false;
            }

            while (_nextDrainIndex < confirmedFragments.Count)
            {
                TerritoryTrailFragment fragment = confirmedFragments[_nextDrainIndex];
                if (fragment == null || fragment.SessionId != _activeSessionId ||
                    fragment.Sequence != (uint)_nextDrainIndex)
                {
                    reason = "Confirmed Trail fragments do not match the active compact shadow session.";
                    return false;
                }

                _activeFragments.Add(fragment);
                _nextDrainIndex++;
            }

            reason = null;
            return true;
        }

        public bool TrySchedule(ulong sessionId, out string reason)
        {
            if (!IsInitialized || _activeFragments == null || sessionId != _activeSessionId)
            {
                reason = "Compact expansion shadow cannot schedule the requested Trail session.";
                return false;
            }

            List<TerritoryTrailFragment> owned = _activeFragments;
            _activeFragments = null;
            _activeSessionId = 0UL;
            _nextDrainIndex = 0;
            if (!TerritoryCompactExpansionWorkItem.TryTakeOwnership(
                    sessionId,
                    _nextScheduledSourceRevision,
                    owned,
                    out TerritoryCompactExpansionWorkItem item,
                    out reason))
            {
                return false;
            }
            if (!_worker.TryEnqueue(item, out reason))
                return false;

            _nextScheduledSourceRevision++;
            return true;
        }

        public void AbortActiveTrail()
        {
            _activeFragments = null;
            _activeSessionId = 0UL;
            _nextDrainIndex = 0;
        }

        public bool TryPollOne(
            out bool hadCompletion,
            out TerritoryCompactCommitResult commit,
            out string reason)
        {
            hadCompletion = false;
            commit = null;
            if (!IsInitialized)
            {
                reason = LastFailureReason;
                return string.IsNullOrEmpty(reason);
            }
            if (!_worker.TryTakeCompleted(out TerritoryCompactExpansionWorkResult result))
            {
                reason = null;
                return true;
            }

            hadCompletion = true;
            if (!result.IsSuccess)
                return Disable(result.FailureReason, out reason);
            if (!_store.TryPublish(result.ApplySession, out commit, out string publishReason))
                return Disable(publishReason, out reason);
            if (commit.Revision != result.Candidate.Revision)
                return Disable("Published compact revision does not match the worker candidate.", out reason);

            LastMetrics = result.Metrics;
            LastFailureReason = null;
            reason = null;
            return true;
        }

        public void Dispose()
        {
            _disabled = true;
            _activeFragments = null;
            _activeSessionId = 0UL;
            _nextDrainIndex = 0;
            _worker?.Dispose();
            _worker = null;
            _store = null;
        }

        private bool Disable(string failureReason, out string reason)
        {
            LastFailureReason = string.IsNullOrEmpty(failureReason)
                ? "Compact expansion shadow failed without a reason."
                : failureReason;
            _disabled = true;
            _activeFragments = null;
            _worker?.Dispose();
            reason = LastFailureReason;
            return false;
        }
    }
}
