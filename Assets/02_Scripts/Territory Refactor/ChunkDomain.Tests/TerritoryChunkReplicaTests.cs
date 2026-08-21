using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryChunkReplicaTests
    {
        [Test]
        public void EmptyDeltaAdvancesRevisionWithoutChangingCoverage()
        {
            var packetizer = new TerritoryChunkTransferPacketizer();
            var replica = new TerritoryChunkReplica();
            ApplyResult(replica, packetizer, FullChunkResult(0, 1, 3, 4));

            ApplyResult(
                replica,
                packetizer,
                new TerritoryChunkCommitResult(1, 2, new TerritoryChunkCoverage[0]));

            Assert.That(replica.Current.Revision, Is.EqualTo(2));
            Assert.That(replica.Current.Chunks.Count, Is.EqualTo(1));
            Assert.That(replica.Current.GetFill(new TerritoryChunkCoordinate(3, 4)),
                Is.EqualTo(TerritoryChunkFill.Full));
        }

        [Test]
        public void PacketGapAndMalformedTerminalPreservePublishedReplica()
        {
            var packetizer = new TerritoryChunkTransferPacketizer();
            var replica = new TerritoryChunkReplica();
            ApplyResult(replica, packetizer, FullChunkResult(0, 1, 1, 2));
            TerritoryChunkSnapshot published = replica.Current;

            TerritoryChunkCommitResult large = LargeBoundaryResult(1, 2);
            Assert.That(packetizer.TryCreateDelta(large, out var packets, out string reason),
                Is.True, reason);
            Assert.That(packets.Count, Is.GreaterThan(1));
            Assert.That(replica.TryBegin(1, 2, packets.Count, out reason), Is.True, reason);
            Assert.That(replica.TryAppend(packets[1], out reason), Is.False);
            replica.Abort();
            Assert.That(replica.Current, Is.SameAs(published));

            var malformed = new TerritoryChunkTransferPacket(
                1,
                2,
                0,
                new[] { 1, 999 });
            Assert.That(replica.TryBegin(1, 2, 1, out reason), Is.True, reason);
            Assert.That(replica.TryAppend(malformed, out reason), Is.True, reason);
            Assert.That(replica.TryComplete(out _, out reason), Is.False);
            Assert.That(replica.Current, Is.SameAs(published));
            Assert.That(replica.IsReceiving, Is.False);
        }

        [Test]
        public void StaleDeltaBaseIsRejectedBeforePartialStateIsAccepted()
        {
            var packetizer = new TerritoryChunkTransferPacketizer();
            var replica = new TerritoryChunkReplica();
            ApplyResult(replica, packetizer, FullChunkResult(0, 1, 0, 0));
            TerritoryChunkSnapshot published = replica.Current;

            Assert.That(replica.TryBegin(0, 1, 1, out string reason), Is.False);
            Assert.That(reason, Does.Contain("does not match replica 1"));
            Assert.That(replica.Current, Is.SameAs(published));
            Assert.That(replica.IsReceiving, Is.False);
        }

        [Test]
        public void ResetDiscardsInboundAndPublishedReplica()
        {
            var packetizer = new TerritoryChunkTransferPacketizer();
            var replica = new TerritoryChunkReplica();
            ApplyResult(replica, packetizer, FullChunkResult(0, 1, 4, 5));
            Assert.That(replica.TryBegin(1, 2, 1, out string reason), Is.True, reason);

            replica.Reset();

            Assert.That(replica.IsReceiving, Is.False);
            Assert.That(replica.Current.Revision, Is.EqualTo(0));
            Assert.That(replica.Current.Chunks, Is.Empty);
        }

        private static TerritoryChunkCommitResult FullChunkResult(
            ulong baseRevision,
            ulong revision,
            int x,
            int y)
        {
            var chunk = new TerritoryChunkCoordinate(x, y);
            return new TerritoryChunkCommitResult(
                baseRevision,
                revision,
                new[] { TerritoryChunkCoverage.Full(chunk) });
        }

        private static TerritoryChunkCommitResult LargeBoundaryResult(
            ulong baseRevision,
            ulong revision)
        {
            var chunk = new TerritoryChunkCoordinate(2, 3);
            var segments = new TerritoryChunkBoundarySegment[12];
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i] = new TerritoryChunkBoundarySegment(
                    100 + i,
                    new TerritoryChunkLocalPoint(i, i),
                    new TerritoryChunkLocalPoint(i + 1, i + 2));
            }

            return new TerritoryChunkCommitResult(
                baseRevision,
                revision,
                new[] { TerritoryChunkCoverage.Boundary(chunk, true, segments) });
        }

        private static void ApplyResult(
            TerritoryChunkReplica replica,
            TerritoryChunkTransferPacketizer packetizer,
            TerritoryChunkCommitResult result)
        {
            Assert.That(packetizer.TryCreateDelta(result, out var packets, out string reason),
                Is.True, reason);
            Assert.That(replica.TryBegin(
                result.BaseRevision,
                result.Revision,
                packets.Count,
                out reason), Is.True, reason);
            for (int i = 0; i < packets.Count; i++)
                Assert.That(replica.TryAppend(packets[i], out reason), Is.True, reason);
            Assert.That(replica.TryComplete(out _, out reason), Is.True, reason);
        }
    }
}
