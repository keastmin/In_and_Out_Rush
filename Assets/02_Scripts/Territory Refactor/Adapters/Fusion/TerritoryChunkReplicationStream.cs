using System;
using System.Collections.Generic;
using Fusion;

namespace ProjectIO.Territory
{
    public sealed class TerritoryChunkReplicationStream
    {
        private readonly TerritoryChunkTransferPacketizer _packetizer = new();
        private readonly TerritoryChunkReplica _replica = new();
        private readonly Queue<OutboundTransfer> _outbound = new();
        private readonly HashSet<PlayerRef> _pendingSnapshotTargets = new();

        private int _recoveryMismatchTicks;
        private int _recoveryWaitTicks;
        private bool _recoveryRequestOutstanding;

        public const int MaximumDataPacketsPerTick = 2;
        public const int RecoveryMismatchDelayTicks = 30;
        public const int RecoveryRequestTimeoutTicks = 300;

        public TerritoryChunkSnapshot ReplicaSnapshot => _replica.Current;
        public bool IsReceiving => _replica.IsReceiving;
        public ulong IncomingRevision => _replica.IncomingRevision;
        public int PendingOutboundTransferCount => _outbound.Count;
        public bool IsRecoveryRequestOutstanding => _recoveryRequestOutstanding;

        public bool TryEnqueueDelta(
            TerritoryChunkCommitResult result,
            out string reason)
        {
            if (!_packetizer.TryCreateDelta(result, out var packets, out reason))
                return false;

            _outbound.Enqueue(new OutboundTransfer(
                TerritoryChunkTransferKind.Delta,
                result.BaseRevision,
                result.Revision,
                packets,
                false,
                PlayerRef.None));
            reason = null;
            return true;
        }

        public bool TryEnqueueSnapshot(
            PlayerRef target,
            TerritoryChunkSnapshot snapshot,
            out string reason)
        {
            if (target == PlayerRef.None)
            {
                reason = "Chunk snapshot recovery requires a target PlayerRef.";
                return false;
            }
            if (_pendingSnapshotTargets.Contains(target))
            {
                reason = null;
                return true;
            }
            if (!_packetizer.TryCreateSnapshot(snapshot, out var packets, out reason))
                return false;

            _pendingSnapshotTargets.Add(target);
            _outbound.Enqueue(new OutboundTransfer(
                TerritoryChunkTransferKind.Snapshot,
                0,
                snapshot.Revision,
                packets,
                true,
                target));
            reason = null;
            return true;
        }

        public bool TryTakeOutbound(
            int remainingDataPacketBudget,
            out OutboundMessage message)
        {
            message = null;
            if (_outbound.Count == 0)
                return false;

            OutboundTransfer transfer = _outbound.Peek();
            if (!transfer.BeginSent)
            {
                transfer.BeginSent = true;
                message = OutboundMessage.Begin(transfer);
                return true;
            }

            if (transfer.NextPacketIndex < transfer.Packets.Count)
            {
                if (remainingDataPacketBudget <= 0)
                    return false;

                TerritoryChunkTransferPacket packet = transfer.Packets[transfer.NextPacketIndex++];
                message = OutboundMessage.Data(transfer, packet);
                return true;
            }

            _outbound.Dequeue();
            if (transfer.IsTargeted)
                _pendingSnapshotTargets.Remove(transfer.Target);
            message = OutboundMessage.Complete(transfer);
            return true;
        }

        public void SkipCurrentOutbound()
        {
            if (_outbound.Count == 0)
                return;

            OutboundTransfer transfer = _outbound.Dequeue();
            if (transfer.IsTargeted)
                _pendingSnapshotTargets.Remove(transfer.Target);
        }

        public bool TryBeginInbound(
            TerritoryChunkTransferKind kind,
            ulong baseRevision,
            ulong revision,
            int packetCount,
            out string reason)
        {
            if (_replica.IsReceiving)
                _replica.Abort();

            if (_replica.TryBegin(kind, baseRevision, revision, packetCount, out reason))
            {
                _recoveryMismatchTicks = 0;
                return true;
            }

            MarkInboundFailure();
            return false;
        }

        public bool TryAppendInbound(
            TerritoryChunkTransferKind kind,
            ulong baseRevision,
            ulong revision,
            uint sequence,
            int[] words,
            out string reason)
        {
            try
            {
                var packet = new TerritoryChunkTransferPacket(
                    kind,
                    baseRevision,
                    revision,
                    sequence,
                    words);
                if (_replica.TryAppend(packet, out reason))
                    return true;
            }
            catch (Exception exception) when (exception is ArgumentException)
            {
                reason = exception.Message;
            }

            _replica.Abort();
            MarkInboundFailure();
            return false;
        }

        public bool TryCompleteInbound(
            TerritoryChunkTransferKind kind,
            ulong baseRevision,
            ulong revision,
            out TerritoryChunkSnapshot snapshot,
            out string reason)
        {
            snapshot = null;
            if (!_replica.IsReceiving || _replica.IncomingKind != kind ||
                _replica.IncomingBaseRevision != baseRevision ||
                _replica.IncomingRevision != revision)
            {
                reason = "Chunk transfer terminal does not match the active transaction.";
                _replica.Abort();
                MarkInboundFailure();
                return false;
            }

            if (_replica.TryComplete(out snapshot, out reason))
            {
                _recoveryMismatchTicks = 0;
                if (kind == TerritoryChunkTransferKind.Snapshot)
                {
                    _recoveryRequestOutstanding = false;
                    _recoveryWaitTicks = 0;
                }
                return true;
            }

            MarkInboundFailure();
            return false;
        }

        public bool TickRecovery(ulong advertisedRevision)
        {
            if (advertisedRevision == 0 || _replica.Current.Revision == advertisedRevision)
            {
                _recoveryMismatchTicks = 0;
                _recoveryWaitTicks = 0;
                _recoveryRequestOutstanding = false;
                return false;
            }
            if (_replica.IsReceiving)
            {
                _recoveryMismatchTicks = 0;
                return false;
            }
            if (_recoveryRequestOutstanding)
            {
                _recoveryWaitTicks++;
                if (_recoveryWaitTicks < RecoveryRequestTimeoutTicks)
                    return false;

                _recoveryRequestOutstanding = false;
                _recoveryWaitTicks = 0;
            }

            _recoveryMismatchTicks++;
            if (_recoveryMismatchTicks < RecoveryMismatchDelayTicks)
                return false;

            _recoveryMismatchTicks = 0;
            _recoveryWaitTicks = 0;
            _recoveryRequestOutstanding = true;
            return true;
        }

        public void Reset()
        {
            _outbound.Clear();
            _pendingSnapshotTargets.Clear();
            _replica.Reset();
            _recoveryMismatchTicks = 0;
            _recoveryWaitTicks = 0;
            _recoveryRequestOutstanding = false;
        }

        private void MarkInboundFailure()
        {
            _recoveryRequestOutstanding = false;
            _recoveryWaitTicks = 0;
            _recoveryMismatchTicks = RecoveryMismatchDelayTicks - 1;
        }

        public enum OutboundMessageType
        {
            Begin,
            Data,
            Complete
        }

        public sealed class OutboundMessage
        {
            private OutboundMessage(
                OutboundMessageType type,
                OutboundTransfer transfer,
                TerritoryChunkTransferPacket packet)
            {
                Type = type;
                Kind = transfer.Kind;
                BaseRevision = transfer.BaseRevision;
                Revision = transfer.Revision;
                PacketCount = transfer.Packets.Count;
                IsTargeted = transfer.IsTargeted;
                Target = transfer.Target;
                PacketSequence = packet?.Sequence ?? 0;
                Words = packet?.CopyWords();
            }

            public OutboundMessageType Type { get; }
            public TerritoryChunkTransferKind Kind { get; }
            public ulong BaseRevision { get; }
            public ulong Revision { get; }
            public int PacketCount { get; }
            public bool IsTargeted { get; }
            public PlayerRef Target { get; }
            public uint PacketSequence { get; }
            public int[] Words { get; }

            internal static OutboundMessage Begin(OutboundTransfer transfer)
                => new(OutboundMessageType.Begin, transfer, null);

            internal static OutboundMessage Data(
                OutboundTransfer transfer,
                TerritoryChunkTransferPacket packet)
                => new(OutboundMessageType.Data, transfer, packet);

            internal static OutboundMessage Complete(OutboundTransfer transfer)
                => new(OutboundMessageType.Complete, transfer, null);
        }

        internal sealed class OutboundTransfer
        {
            public OutboundTransfer(
                TerritoryChunkTransferKind kind,
                ulong baseRevision,
                ulong revision,
                IReadOnlyList<TerritoryChunkTransferPacket> packets,
                bool isTargeted,
                PlayerRef target)
            {
                Kind = kind;
                BaseRevision = baseRevision;
                Revision = revision;
                Packets = packets;
                IsTargeted = isTargeted;
                Target = target;
            }

            public TerritoryChunkTransferKind Kind { get; }
            public ulong BaseRevision { get; }
            public ulong Revision { get; }
            public IReadOnlyList<TerritoryChunkTransferPacket> Packets { get; }
            public bool IsTargeted { get; }
            public PlayerRef Target { get; }
            public bool BeginSent { get; set; }
            public int NextPacketIndex { get; set; }
        }
    }
}
