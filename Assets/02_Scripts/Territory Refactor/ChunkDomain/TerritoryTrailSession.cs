using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryTrailSession
    {
        private enum SessionMode
        {
            None,
            SampleBuilder,
            FragmentReceiver
        }

        private readonly List<TerritoryTrailSample> _samples = new();
        private readonly List<TerritoryTrailFragment> _fragments = new();
        private readonly List<TerritorySegmentChunkTraversal.SegmentPart> _segmentParts = new();
        private readonly List<FixedTerritoryPoint> _openFragmentPoints = new();

        private SessionMode _mode;
        private TerritoryChunkCoordinate _openFragmentChunk;
        private uint _openFragmentFirstSampleSequence;
        private uint _openFragmentLastSampleSequence;
        private uint _nextSampleSequence;
        private uint _nextFragmentSequence;
        private int _lastSimulationTick;
        private bool _hasOpenFragment;

        public ulong SessionId { get; private set; }
        public TerritoryTrailSessionStatus Status { get; private set; }
        public IReadOnlyList<TerritoryTrailSample> Samples => _samples;
        public IReadOnlyList<TerritoryTrailFragment> Fragments => _fragments;

        public bool TryBegin(ulong sessionId, out string reason)
        {
            if (!CanBegin(sessionId, out reason))
                return false;

            ResetForSession(sessionId);
            return true;
        }

        public bool TryBegin(TerritoryTrailSample initialSample, out string reason)
        {
            if (initialSample.Sequence != 0)
            {
                reason = "The initial sample sequence must be zero.";
                return false;
            }

            if (!CanBegin(initialSample.SessionId, out reason))
                return false;

            ResetForSession(initialSample.SessionId);
            _mode = SessionMode.SampleBuilder;
            _samples.Add(initialSample);
            _nextSampleSequence = 1;
            _lastSimulationTick = initialSample.SimulationTick;
            return true;
        }

        public bool TryAppendSample(TerritoryTrailSample sample, out string reason)
        {
            if (!ValidateActiveSession(sample.SessionId, out reason))
                return false;
            if (_mode == SessionMode.FragmentReceiver)
            {
                reason = "A fragment receiver cannot append authoritative samples.";
                return false;
            }
            if (_samples.Count == 0)
            {
                reason = "A sample builder requires an initial sample.";
                return false;
            }
            if (sample.Sequence != _nextSampleSequence)
            {
                reason = $"Expected sample sequence {_nextSampleSequence}, received {sample.Sequence}.";
                return false;
            }
            if (sample.Sequence == uint.MaxValue)
            {
                reason = "Sample sequence exhausted UInt32 range.";
                return false;
            }
            if (sample.SimulationTick < _lastSimulationTick)
            {
                reason = "Simulation ticks must be nondecreasing within a Trail session.";
                return false;
            }

            _mode = SessionMode.SampleBuilder;
            TerritoryTrailSample previous = _samples[^1];
            TerritorySegmentChunkTraversal.Split(previous.Point, sample.Point, _segmentParts);
            AppendSegmentParts(previous.Sequence, sample.Sequence);

            _samples.Add(sample);
            _nextSampleSequence++;
            _lastSimulationTick = sample.SimulationTick;
            reason = null;
            return true;
        }

        public bool TryAppendFragment(TerritoryTrailFragment fragment, out string reason)
        {
            if (fragment == null)
            {
                reason = "Trail fragment cannot be null.";
                return false;
            }
            if (!ValidateActiveSession(fragment.SessionId, out reason))
                return false;
            if (_mode == SessionMode.SampleBuilder)
            {
                reason = "A sample builder cannot receive replicated fragments.";
                return false;
            }
            if (fragment.Sequence != _nextFragmentSequence)
            {
                reason = $"Expected fragment sequence {_nextFragmentSequence}, received {fragment.Sequence}.";
                return false;
            }
            if (fragment.Sequence == uint.MaxValue)
            {
                reason = "Fragment sequence exhausted UInt32 range.";
                return false;
            }
            if (_fragments.Count > 0)
            {
                IReadOnlyList<FixedTerritoryPoint> previousPoints = _fragments[^1].Points;
                if (previousPoints[^1] != fragment.Points[0])
                {
                    reason = "Adjacent Trail fragments must share one boundary point.";
                    return false;
                }
            }

            _mode = SessionMode.FragmentReceiver;
            _fragments.Add(fragment);
            _nextFragmentSequence++;
            reason = null;
            return true;
        }

        public bool TryAbort(ulong sessionId, out string reason)
        {
            if (!ValidateActiveSession(sessionId, out reason))
                return false;

            ClearPayload();
            _mode = SessionMode.None;
            Status = TerritoryTrailSessionStatus.Aborted;
            reason = null;
            return true;
        }

        public bool TryCommit(ulong sessionId, out string reason)
        {
            if (!ValidateActiveSession(sessionId, out reason))
                return false;

            if (_mode == SessionMode.SampleBuilder)
                FinalizeOpenFragment();

            Status = TerritoryTrailSessionStatus.Committed;
            reason = null;
            return true;
        }

        public void CopyFragmentPathTo(List<FixedTerritoryPoint> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();
            for (int fragmentIndex = 0; fragmentIndex < _fragments.Count; fragmentIndex++)
            {
                IReadOnlyList<FixedTerritoryPoint> points = _fragments[fragmentIndex].Points;
                for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
                {
                    FixedTerritoryPoint point = points[pointIndex];
                    if (results.Count == 0 || results[^1] != point)
                        results.Add(point);
                }
            }
        }

        private bool CanBegin(ulong sessionId, out string reason)
        {
            if (sessionId == 0)
            {
                reason = "SessionId must be greater than zero.";
                return false;
            }
            if (Status == TerritoryTrailSessionStatus.Active)
            {
                reason = "The current Trail session is still active.";
                return false;
            }
            if (sessionId <= SessionId)
            {
                reason = $"SessionId {sessionId} is stale; the last SessionId is {SessionId}.";
                return false;
            }

            reason = null;
            return true;
        }

        private bool ValidateActiveSession(ulong sessionId, out string reason)
        {
            if (Status != TerritoryTrailSessionStatus.Active)
            {
                reason = "The Trail session is not active.";
                return false;
            }
            if (sessionId != SessionId)
            {
                reason = $"Payload SessionId {sessionId} does not match active SessionId {SessionId}.";
                return false;
            }

            reason = null;
            return true;
        }

        private void ResetForSession(ulong sessionId)
        {
            ClearPayload();
            SessionId = sessionId;
            Status = TerritoryTrailSessionStatus.Active;
            _mode = SessionMode.None;
            _nextSampleSequence = 0;
            _nextFragmentSequence = 0;
            _lastSimulationTick = int.MinValue;
        }

        private void ClearPayload()
        {
            _samples.Clear();
            _fragments.Clear();
            _segmentParts.Clear();
            _openFragmentPoints.Clear();
            _hasOpenFragment = false;
            _nextSampleSequence = 0;
            _nextFragmentSequence = 0;
            _lastSimulationTick = int.MinValue;
        }

        private void AppendSegmentParts(uint firstSampleSequence, uint lastSampleSequence)
        {
            for (int i = 0; i < _segmentParts.Count; i++)
            {
                TerritorySegmentChunkTraversal.SegmentPart part = _segmentParts[i];
                if (part.Start == part.End)
                    continue;

                if (!_hasOpenFragment || _openFragmentChunk != part.Chunk)
                {
                    FinalizeOpenFragment();
                    _openFragmentChunk = part.Chunk;
                    _openFragmentFirstSampleSequence = firstSampleSequence;
                    _openFragmentPoints.Add(part.Start);
                    _hasOpenFragment = true;
                }
                else if (_openFragmentPoints[^1] != part.Start)
                {
                    _openFragmentPoints.Add(part.Start);
                }

                if (_openFragmentPoints[^1] != part.End)
                    _openFragmentPoints.Add(part.End);

                _openFragmentLastSampleSequence = lastSampleSequence;
            }
        }

        private void FinalizeOpenFragment()
        {
            if (!_hasOpenFragment)
                return;

            if (_openFragmentPoints.Count >= 2)
            {
                if (_nextFragmentSequence == uint.MaxValue)
                    throw new InvalidOperationException("Fragment sequence exhausted UInt32 range.");

                var fragment = new TerritoryTrailFragment(
                    SessionId,
                    _nextFragmentSequence,
                    _openFragmentChunk,
                    _openFragmentFirstSampleSequence,
                    _openFragmentLastSampleSequence,
                    _openFragmentPoints);
                _fragments.Add(fragment);
                _nextFragmentSequence++;
            }

            _openFragmentPoints.Clear();
            _hasOpenFragment = false;
        }
    }
}
