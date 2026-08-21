using System.Collections.Generic;
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
            TerritoryChunkSnapshot initial = SnapshotWithFullChunk(1, 3, 4);
            ApplySnapshot(replica, packetizer, initial);

            var result = new TerritoryChunkCommitResult(
                1,
                2,
                new TerritoryChunkCoverage[0]);
            Assert.That(packetizer.TryCreateDelta(result, out var packets, out string reason),
                Is.True, reason);
            Assert.That(packets.Count, Is.EqualTo(1));
            Assert.That(replica.TryBegin(
                TerritoryChunkTransferKind.Delta,
                1,
                2,
                packets.Count,
                out reason), Is.True, reason);
            Assert.That(replica.TryAppend(packets[0], out reason), Is.True, reason);
            Assert.That(replica.TryComplete(out TerritoryChunkSnapshot actual, out reason),
                Is.True, reason);

            Assert.That(actual.Revision, Is.EqualTo(2));
            Assert.That(actual.Chunks.Count, Is.EqualTo(1));
            Assert.That(actual.GetFill(new TerritoryChunkCoordinate(3, 4)),
                Is.EqualTo(TerritoryChunkFill.Full));
        }

        [Test]
        public void PacketGapAndMalformedTerminalPreservePublishedReplica()
        {
            var packetizer = new TerritoryChunkTransferPacketizer();
            var replica = new TerritoryChunkReplica();
            ApplySnapshot(replica, packetizer, SnapshotWithFullChunk(5, 1, 2));
            TerritoryChunkSnapshot published = replica.Current;

            TerritoryChunkSnapshot large = LargeBoundarySnapshot(6);
            Assert.That(packetizer.TryCreateSnapshot(large, out var packets, out string reason),
                Is.True, reason);
            Assert.That(packets.Count, Is.GreaterThan(1));
            Assert.That(replica.TryBegin(
                TerritoryChunkTransferKind.Snapshot,
                0,
                6,
                packets.Count,
                out reason), Is.True, reason);
            Assert.That(replica.TryAppend(packets[1], out reason), Is.False);
            replica.Abort();
            Assert.That(replica.Current, Is.SameAs(published));

            var malformed = new TerritoryChunkTransferPacket(
                TerritoryChunkTransferKind.Snapshot,
                0,
                7,
                0,
                new[] { 1, 999 });
            Assert.That(replica.TryBegin(
                TerritoryChunkTransferKind.Snapshot,
                0,
                7,
                1,
                out reason), Is.True, reason);
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
            ApplySnapshot(replica, packetizer, SnapshotWithFullChunk(3, 0, 0));
            TerritoryChunkSnapshot published = replica.Current;

            Assert.That(replica.TryBegin(
                TerritoryChunkTransferKind.Delta,
                2,
                3,
                1,
                out string reason), Is.False);
            Assert.That(reason, Does.Contain("does not match replica 3"));
            Assert.That(replica.Current, Is.SameAs(published));
            Assert.That(replica.IsReceiving, Is.False);
        }

        [Test]
        public void NewerSnapshotAtomicallyReplacesOlderReplica()
        {
            var packetizer = new TerritoryChunkTransferPacketizer();
            var replica = new TerritoryChunkReplica();
            ApplySnapshot(replica, packetizer, SnapshotWithFullChunk(2, -1, -1));

            TerritoryChunkSnapshot replacement = SnapshotWithFullChunk(9, 7, 8);
            ApplySnapshot(replica, packetizer, replacement);

            Assert.That(replica.Current.Revision, Is.EqualTo(9));
            Assert.That(replica.Current.GetFill(new TerritoryChunkCoordinate(-1, -1)),
                Is.EqualTo(TerritoryChunkFill.Empty));
            Assert.That(replica.Current.GetFill(new TerritoryChunkCoordinate(7, 8)),
                Is.EqualTo(TerritoryChunkFill.Full));
        }

        [Test]
        public void OlderRecoverySnapshotCannotDowngradeReplica()
        {
            var packetizer = new TerritoryChunkTransferPacketizer();
            var replica = new TerritoryChunkReplica();
            ApplySnapshot(replica, packetizer, SnapshotWithFullChunk(8, 4, 5));
            TerritoryChunkSnapshot published = replica.Current;

            Assert.That(replica.TryBegin(
                TerritoryChunkTransferKind.Snapshot,
                0,
                7,
                1,
                out string reason), Is.False);
            Assert.That(reason, Does.Contain("older than replica 8"));
            Assert.That(replica.Current, Is.SameAs(published));
        }

        private static TerritoryChunkSnapshot SnapshotWithFullChunk(
            ulong revision,
            int x,
            int y)
        {
            var chunk = new TerritoryChunkCoordinate(x, y);
            return new TerritoryChunkSnapshot(
                revision,
                new Dictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage>
                {
                    [chunk] = TerritoryChunkCoverage.Full(chunk)
                });
        }

        private static TerritoryChunkSnapshot LargeBoundarySnapshot(ulong revision)
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

            return new TerritoryChunkSnapshot(
                revision,
                new Dictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage>
                {
                    [chunk] = TerritoryChunkCoverage.Boundary(chunk, true, segments)
                });
        }

        private static void ApplySnapshot(
            TerritoryChunkReplica replica,
            TerritoryChunkTransferPacketizer packetizer,
            TerritoryChunkSnapshot snapshot)
        {
            Assert.That(packetizer.TryCreateSnapshot(snapshot, out var packets, out string reason),
                Is.True, reason);
            Assert.That(replica.TryBegin(
                TerritoryChunkTransferKind.Snapshot,
                0,
                snapshot.Revision,
                packets.Count,
                out reason), Is.True, reason);
            for (int i = 0; i < packets.Count; i++)
                Assert.That(replica.TryAppend(packets[i], out reason), Is.True, reason);
            Assert.That(replica.TryComplete(out _, out reason), Is.True, reason);
        }
    }
}
