using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryTrailPacketizer
    {
        private readonly TerritoryAppendOnlyBlockList<TerritoryTrailSample> _pendingSamples = new();

        private ulong _lastSessionId;
        private ulong _sessionId;
        private uint _nextSampleSequence;
        private uint _nextPacketSequence;
        private int _lastSimulationTick;
        private int _pendingStartIndex;

        public bool IsActive { get; private set; }
        public int PendingSampleCount => _pendingSamples.Count - _pendingStartIndex;
        public ulong SessionId => _sessionId;

        public bool TryBegin(ulong sessionId, out string reason)
        {
            if (IsActive)
            {
                reason = "The outbound Trail stream is already active.";
                return false;
            }
            if (sessionId == 0 || sessionId <= _lastSessionId)
            {
                reason = $"SessionId {sessionId} is not newer than {_lastSessionId}.";
                return false;
            }

            _pendingSamples.Clear();
            _sessionId = sessionId;
            _lastSessionId = sessionId;
            _nextSampleSequence = 0;
            _nextPacketSequence = 0;
            _lastSimulationTick = int.MinValue;
            _pendingStartIndex = 0;
            IsActive = true;
            reason = null;
            return true;
        }

        public bool TryAppendSample(TerritoryTrailSample sample, out string reason)
        {
            if (!IsActive || sample.SessionId != _sessionId)
            {
                reason = "The sample does not belong to the active outbound Trail stream.";
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
                reason = "Simulation ticks must be nondecreasing within a Trail stream.";
                return false;
            }

            _pendingSamples.Add(sample);
            _nextSampleSequence++;
            _lastSimulationTick = sample.SimulationTick;
            reason = null;
            return true;
        }

        public bool TryTakePacket(
            int maximumSampleCount,
            out TerritoryTrailPacket packet,
            out string reason)
        {
            packet = null;
            if (!IsActive)
            {
                reason = "The outbound Trail stream is not active.";
                return false;
            }
            if (PendingSampleCount == 0)
            {
                reason = "The outbound Trail stream has no pending samples.";
                return false;
            }
            if (maximumSampleCount <= 0 || maximumSampleCount > TerritoryTrailPacket.MaximumSampleCount)
            {
                reason = $"Packet size must be between 1 and {TerritoryTrailPacket.MaximumSampleCount}.";
                return false;
            }
            if (_nextPacketSequence == uint.MaxValue)
            {
                reason = "Packet sequence exhausted UInt32 range.";
                return false;
            }

            int sampleCount = Math.Min(maximumSampleCount, PendingSampleCount);
            var samples = new TerritoryTrailSample[sampleCount];
            for (int index = 0; index < sampleCount; index++)
                samples[index] = _pendingSamples[_pendingStartIndex + index];
            packet = new TerritoryTrailPacket(
                _sessionId,
                _nextPacketSequence,
                samples[0].Sequence,
                samples);

            _pendingStartIndex += sampleCount;
            if (_pendingStartIndex == _pendingSamples.Count)
            {
                _pendingSamples.Clear();
                _pendingStartIndex = 0;
            }
            _nextPacketSequence++;
            reason = null;
            return true;
        }

        public bool TryCommit(out string reason)
        {
            if (!IsActive)
            {
                reason = "The outbound Trail stream is not active.";
                return false;
            }
            if (PendingSampleCount > 0)
            {
                reason = "Pending samples must be packetized before committing the Trail stream.";
                return false;
            }

            IsActive = false;
            reason = null;
            return true;
        }

        public bool TryAbort(out string reason)
        {
            if (!IsActive)
            {
                reason = "The outbound Trail stream is not active.";
                return false;
            }

            _pendingSamples.Clear();
            _pendingStartIndex = 0;
            IsActive = false;
            reason = null;
            return true;
        }
    }
}
