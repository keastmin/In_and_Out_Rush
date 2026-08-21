using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryTrailReplicationStream
    {
        private readonly TerritoryTrailPacketizer _outbound = new();
        private readonly TerritoryTrailReceiver _inbound = new();

        private FixedTerritoryPoint _liveHead;
        private uint _liveHeadSampleSequence;
        private bool _hasLiveHead;

        public bool IsOutboundActive => _outbound.IsActive;
        public bool IsInboundActive => _inbound.IsActive;
        public int PendingOutboundSampleCount => _outbound.PendingSampleCount;
        public ulong OutboundSessionId => _outbound.SessionId;
        public ulong InboundSessionId => _inbound.SessionId;
        public IReadOnlyList<TerritoryTrailSample> ConfirmedSamples => _inbound.Samples;
        public bool HasLiveHead => _hasLiveHead;
        public FixedTerritoryPoint LiveHead => _liveHead;

        public bool TryBeginOutbound(ulong sessionId, out string reason)
            => _outbound.TryBegin(sessionId, out reason);

        public bool TryAppendOutbound(TerritoryTrailSample sample, out string reason)
            => _outbound.TryAppendSample(sample, out reason);

        public bool TryTakeOutboundPacket(out TerritoryTrailPacket packet, out string reason)
            => _outbound.TryTakePacket(
                TerritoryTrailPacket.MaximumSampleCount,
                out packet,
                out reason);

        public bool TryCommitOutbound(out string reason)
            => _outbound.TryCommit(out reason);

        public bool TryAbortOutbound(out string reason)
            => _outbound.TryAbort(out reason);

        public bool TryBeginInbound(ulong sessionId, out string reason)
        {
            if (!_inbound.TryBegin(sessionId, out reason))
                return false;

            ClearLiveHead();
            return true;
        }

        public bool TryAppendInbound(TerritoryTrailPacket packet, out string reason)
        {
            if (!_inbound.TryAppendPacket(packet, out reason))
                return false;

            if (_hasLiveHead &&
                _inbound.Samples.Count > 0 &&
                _inbound.Samples[^1].Sequence >= _liveHeadSampleSequence)
            {
                ClearLiveHead();
            }

            return true;
        }

        public bool TryUpdateLiveHead(
            ulong sessionId,
            uint sampleSequence,
            FixedTerritoryPoint point,
            out string reason)
        {
            if (!_inbound.IsActive || sessionId != _inbound.SessionId)
            {
                reason = "The live head does not belong to the active inbound Trail stream.";
                return false;
            }
            if (_inbound.Samples.Count > 0 &&
                sampleSequence <= _inbound.Samples[^1].Sequence)
            {
                reason = "The live head is not newer than the confirmed Trail.";
                return false;
            }
            if (_hasLiveHead && sampleSequence <= _liveHeadSampleSequence)
            {
                reason = "The live head is stale.";
                return false;
            }

            _liveHead = point;
            _liveHeadSampleSequence = sampleSequence;
            _hasLiveHead = true;
            reason = null;
            return true;
        }

        public bool TryCommitInbound(ulong sessionId, out string reason)
        {
            if (!_inbound.TryCommit(sessionId, out reason))
                return false;

            ClearLiveHead();
            return true;
        }

        public bool TryAbortInbound(ulong sessionId, out string reason)
        {
            if (!_inbound.TryAbort(sessionId, out reason))
                return false;

            ClearLiveHead();
            return true;
        }

        public bool TryCopyInboundPathTo(List<FixedTerritoryPoint> results)
            => _inbound.TryCopyActivePathTo(results);

        private void ClearLiveHead()
        {
            _hasLiveHead = false;
            _liveHead = default;
            _liveHeadSampleSequence = 0;
        }
    }
}
