using System.Collections.Generic;
using Fusion;

namespace ProjectIO.Territory
{
    public sealed class TerritoryExpansionReplication
    {
        private readonly Queue<OutboundTransfer> _outbound = new();
        private readonly Queue<OutboundTransfer> _recoveryOutbound = new();
        private readonly TerritoryExpansionResultReplica _replica = new();
        private ulong _nextOutboundSourceRevision;

        public const int MaximumDataPacketsPerTick = 2;

        public ulong ReplicaRevision => _replica.CurrentRevision;
        public int PendingOutboundTransferCount => _outbound.Count;

        public void Reset(ulong initialRevision)
        {
            _outbound.Clear();
            _recoveryOutbound.Clear();
            _replica.Reset(initialRevision);
            _nextOutboundSourceRevision = initialRevision;
        }

        public bool TryEnqueue(
            TerritoryExpansionPresentationData presentation,
            IReadOnlyList<TerritoryExpansionResultPacket> packets,
            out string reason)
        {
            if (presentation == null || packets == null || packets.Count == 0)
            {
                reason = "Expansion replication requires completed data and packets.";
                return false;
            }
            if (presentation.SourceRevision != _nextOutboundSourceRevision ||
                presentation.Revision != presentation.SourceRevision + 1UL)
            {
                reason = "Expansion replication revision is out of order.";
                return false;
            }
            for (int i = 0; i < packets.Count; i++)
            {
                TerritoryExpansionResultPacket packet = packets[i];
                if (packet.SourceRevision != presentation.SourceRevision ||
                    packet.Revision != presentation.Revision ||
                    packet.Sequence != (uint)i)
                {
                    reason = "Expansion replication packet sequence is invalid.";
                    return false;
                }
            }

            _outbound.Enqueue(new OutboundTransfer(presentation, packets));
            _nextOutboundSourceRevision = presentation.Revision;
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

                TerritoryExpansionResultPacket packet =
                    transfer.Packets[transfer.NextPacketIndex++];
                message = OutboundMessage.Data(transfer, packet);
                return true;
            }

            _outbound.Dequeue();
            message = OutboundMessage.Complete(transfer);
            return true;
        }

        public bool TryEnqueueRecovery(
            PlayerRef target,
            TerritoryExpansionPresentationData presentation,
            IReadOnlyList<TerritoryExpansionResultPacket> packets,
            out string reason)
        {
            if (target == PlayerRef.None || presentation == null || packets == null ||
                packets.Count == 0)
            {
                reason = "Expansion recovery requires a target and completed packet data.";
                return false;
            }

            _recoveryOutbound.Enqueue(new OutboundTransfer(presentation, packets, target));
            reason = null;
            return true;
        }

        public bool TryTakeRecoveryOutbound(
            int remainingDataPacketBudget,
            out OutboundMessage message)
        {
            message = null;
            if (_recoveryOutbound.Count == 0)
                return false;

            OutboundTransfer transfer = _recoveryOutbound.Peek();
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

                TerritoryExpansionResultPacket packet =
                    transfer.Packets[transfer.NextPacketIndex++];
                message = OutboundMessage.Data(transfer, packet);
                return true;
            }

            _recoveryOutbound.Dequeue();
            message = OutboundMessage.Complete(transfer);
            return true;
        }

        public bool TryBeginInbound(
            ulong sourceRevision,
            ulong revision,
            int vertexCount,
            int triangleCount,
            int packetCount,
            out string reason)
            => _replica.TryBegin(
                sourceRevision,
                revision,
                vertexCount,
                triangleCount,
                packetCount,
                out reason);

        public bool TryAppendInbound(
            ulong sourceRevision,
            ulong revision,
            uint sequence,
            int[] words,
            out string reason)
            => _replica.TryAppend(
                sourceRevision,
                revision,
                sequence,
                words,
                out reason);

        public bool TryCompleteInbound(
            ulong sourceRevision,
            ulong revision,
            out TerritoryExpansionPresentationData presentation,
            out string reason)
            => _replica.TryComplete(
                sourceRevision,
                revision,
                out presentation,
                out reason);

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
                TerritoryExpansionResultPacket packet)
            {
                Type = type;
                SourceRevision = transfer.Presentation.SourceRevision;
                Revision = transfer.Presentation.Revision;
                VertexCount = transfer.Presentation.Vertices.Count;
                TriangleCount = transfer.Presentation.Triangles.Count;
                PacketCount = transfer.Packets.Count;
                PacketSequence = packet?.Sequence ?? 0U;
                Words = packet?.CopyWords();
                Target = transfer.Target;
            }

            public OutboundMessageType Type { get; }
            public ulong SourceRevision { get; }
            public ulong Revision { get; }
            public int VertexCount { get; }
            public int TriangleCount { get; }
            public int PacketCount { get; }
            public uint PacketSequence { get; }
            public int[] Words { get; }
            public PlayerRef Target { get; }

            internal static OutboundMessage Begin(OutboundTransfer transfer)
                => new(OutboundMessageType.Begin, transfer, null);

            internal static OutboundMessage Data(
                OutboundTransfer transfer,
                TerritoryExpansionResultPacket packet)
                => new(OutboundMessageType.Data, transfer, packet);

            internal static OutboundMessage Complete(OutboundTransfer transfer)
                => new(OutboundMessageType.Complete, transfer, null);
        }

        internal sealed class OutboundTransfer
        {
            public OutboundTransfer(
                TerritoryExpansionPresentationData presentation,
                IReadOnlyList<TerritoryExpansionResultPacket> packets,
                PlayerRef target = default)
            {
                Presentation = presentation;
                Packets = packets;
                Target = target;
            }

            public TerritoryExpansionPresentationData Presentation { get; }
            public IReadOnlyList<TerritoryExpansionResultPacket> Packets { get; }
            public bool BeginSent { get; set; }
            public int NextPacketIndex { get; set; }
            public PlayerRef Target { get; }
        }
    }
}
