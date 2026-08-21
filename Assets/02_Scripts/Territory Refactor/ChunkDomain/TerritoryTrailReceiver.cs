using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryTrailReceiver
    {
        private readonly TerritoryTrailSession _session = new();

        private ulong _lastAnnouncedSessionId;
        private ulong _activeSessionId;
        private uint _nextPacketSequence;
        private uint _nextSampleSequence;
        private int _lastSimulationTick;
        private bool _hasDomainSession;

        public bool IsActive { get; private set; }
        public ulong SessionId => _activeSessionId;
        public IReadOnlyList<TerritoryTrailSample> Samples => _session.Samples;
        public IReadOnlyList<TerritoryTrailFragment> Fragments => _session.Fragments;

        public bool TryBegin(ulong sessionId, out string reason)
        {
            if (IsActive)
            {
                reason = "The inbound Trail stream is already active.";
                return false;
            }
            if (sessionId == 0 || sessionId <= _lastAnnouncedSessionId)
            {
                reason = $"SessionId {sessionId} is not newer than {_lastAnnouncedSessionId}.";
                return false;
            }

            _activeSessionId = sessionId;
            _lastAnnouncedSessionId = sessionId;
            _nextPacketSequence = 0;
            _nextSampleSequence = 0;
            _lastSimulationTick = int.MinValue;
            _hasDomainSession = false;
            IsActive = true;
            reason = null;
            return true;
        }

        public bool TryAppendPacket(TerritoryTrailPacket packet, out string reason)
        {
            if (packet == null)
            {
                reason = "Trail packet cannot be null.";
                return false;
            }
            if (!IsActive || packet.SessionId != _activeSessionId)
            {
                reason = "The packet does not belong to the active inbound Trail stream.";
                return false;
            }
            if (packet.Sequence != _nextPacketSequence)
            {
                reason = $"Expected packet sequence {_nextPacketSequence}, received {packet.Sequence}.";
                return false;
            }
            if (packet.FirstSampleSequence != _nextSampleSequence)
            {
                reason = $"Expected sample sequence {_nextSampleSequence}, received {packet.FirstSampleSequence}.";
                return false;
            }
            if (packet.Samples[0].SimulationTick < _lastSimulationTick)
            {
                reason = "Packet simulation ticks precede the confirmed Trail.";
                return false;
            }
            if (packet.Samples[^1].Sequence == uint.MaxValue)
            {
                reason = "Sample sequence exhausted UInt32 range.";
                return false;
            }
            if (packet.Sequence == uint.MaxValue)
            {
                reason = "Packet sequence exhausted UInt32 range.";
                return false;
            }

            int firstAppendIndex = 0;
            if (!_hasDomainSession)
            {
                if (!_session.TryBegin(packet.Samples[0], out reason))
                    return false;

                _hasDomainSession = true;
                firstAppendIndex = 1;
            }

            for (int index = firstAppendIndex; index < packet.Samples.Count; index++)
            {
                if (!_session.TryAppendSample(packet.Samples[index], out reason))
                    return false;
            }

            _nextPacketSequence++;
            _nextSampleSequence += (uint)packet.Samples.Count;
            _lastSimulationTick = packet.Samples[^1].SimulationTick;
            reason = null;
            return true;
        }

        public bool TryCommit(ulong sessionId, out string reason)
        {
            if (!ValidateActiveSession(sessionId, out reason))
                return false;
            if (!_hasDomainSession)
            {
                reason = "The inbound Trail stream has no confirmed samples.";
                return false;
            }
            if (!_session.TryCommit(sessionId, out reason))
                return false;

            IsActive = false;
            reason = null;
            return true;
        }

        public bool TryAbort(ulong sessionId, out string reason)
        {
            if (!ValidateActiveSession(sessionId, out reason))
                return false;
            if (_hasDomainSession && !_session.TryAbort(sessionId, out reason))
                return false;

            IsActive = false;
            _hasDomainSession = false;
            reason = null;
            return true;
        }

        public bool TryCopyActivePathTo(List<FixedTerritoryPoint> results)
        {
            if (results == null || !IsActive || !_hasDomainSession)
                return false;

            results.Clear();
            for (int index = 0; index < _session.Samples.Count; index++)
                results.Add(_session.Samples[index].Point);
            return true;
        }

        private bool ValidateActiveSession(ulong sessionId, out string reason)
        {
            if (!IsActive || sessionId != _activeSessionId)
            {
                reason = "The lifecycle message does not belong to the active inbound Trail stream.";
                return false;
            }

            reason = null;
            return true;
        }
    }
}
