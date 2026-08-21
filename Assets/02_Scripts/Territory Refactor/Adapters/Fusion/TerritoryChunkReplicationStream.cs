using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryChunkReplicationStream
    {
        private readonly TerritoryChunkTransferPacketizer _packetizer = new();
        private readonly TerritoryChunkReplica _replica = new();
        private readonly Queue<OutboundTransfer> _outbound = new();

        public const int MaximumDataPacketsPerTick = 2;

        public TerritoryChunkSnapshot ReplicaSnapshot => _replica.Current;
        public bool IsReceiving => _replica.IsReceiving;
        public ulong IncomingRevision => _replica.IncomingRevision;
        public int PendingOutboundTransferCount => _outbound.Count;

        public bool TryEnqueueDelta(
            TerritoryChunkCommitResult result,
            out string reason)
        {
            if (!_packetizer.TryCreateDelta(result, out var packets, out reason))
                return false;

            _outbound.Enqueue(new OutboundTransfer(
                result.BaseRevision,
                result.Revision,
                packets));
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
            message = OutboundMessage.Complete(transfer);
            return true;
        }

        public bool TryBeginInbound(
            ulong baseRevision,
            ulong revision,
            int packetCount,
            out string reason)
        {
            if (_replica.IsReceiving)
                _replica.Abort();

            return _replica.TryBegin(
                baseRevision,
                revision,
                packetCount,
                out reason);
        }

        public bool TryAppendInbound(
            ulong baseRevision,
            ulong revision,
            uint sequence,
            int[] words,
            out string reason)
        {
            try
            {
                var packet = new TerritoryChunkTransferPacket(
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
            return false;
        }

        public bool TryCompleteInbound(
            ulong baseRevision,
            ulong revision,
            out TerritoryChunkSnapshot snapshot,
            out string reason)
        {
            snapshot = null;
            if (!_replica.IsReceiving ||
                _replica.IncomingBaseRevision != baseRevision ||
                _replica.IncomingRevision != revision)
            {
                reason = "Chunk transfer terminal does not match the active transaction.";
                _replica.Abort();
                return false;
            }

            return _replica.TryComplete(out snapshot, out reason);
        }

        public void Reset()
        {
            _outbound.Clear();
            _replica.Reset();
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
                BaseRevision = transfer.BaseRevision;
                Revision = transfer.Revision;
                PacketCount = transfer.Packets.Count;
                PacketSequence = packet?.Sequence ?? 0;
                Words = packet?.CopyWords();
            }

            public OutboundMessageType Type { get; }
            public ulong BaseRevision { get; }
            public ulong Revision { get; }
            public int PacketCount { get; }
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
                ulong baseRevision,
                ulong revision,
                IReadOnlyList<TerritoryChunkTransferPacket> packets)
            {
                BaseRevision = baseRevision;
                Revision = revision;
                Packets = packets;
            }

            public ulong BaseRevision { get; }
            public ulong Revision { get; }
            public IReadOnlyList<TerritoryChunkTransferPacket> Packets { get; }
            public bool BeginSent { get; set; }
            public int NextPacketIndex { get; set; }
        }
    }
}
