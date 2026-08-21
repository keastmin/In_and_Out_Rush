using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryChunkTransferPacketizerTests
    {
        [Test]
        public void SnapshotRoundTripPreservesRunsAndBoundaryAcrossBoundedPackets()
        {
            TerritoryChunkSnapshot expected = CreateLargeSnapshot(7);
            var packetizer = new TerritoryChunkTransferPacketizer();

            Assert.That(packetizer.TryCreateSnapshot(expected, out var packets, out string reason),
                Is.True, reason);
            Assert.That(packets.Count, Is.GreaterThan(1));
            for (int i = 0; i < packets.Count; i++)
            {
                Assert.That(packets[i].Sequence, Is.EqualTo((uint)i));
                Assert.That(packets[i].Words.Count,
                    Is.InRange(1, TerritoryChunkTransferPacketizer.MaximumWordCount));
            }

            var replica = new TerritoryChunkReplica();
            Assert.That(replica.TryBegin(
                TerritoryChunkTransferKind.Snapshot,
                0,
                expected.Revision,
                packets.Count,
                out reason), Is.True, reason);
            for (int i = 0; i < packets.Count; i++)
                Assert.That(replica.TryAppend(packets[i], out reason), Is.True, reason);
            Assert.That(replica.TryComplete(out TerritoryChunkSnapshot actual, out reason),
                Is.True, reason);

            AssertSnapshotsEqual(expected, actual);
        }

        [Test]
        public void DeltaRoundTripAppliesFullAndEmptyRunsInDeterministicOrder()
        {
            var packetizer = new TerritoryChunkTransferPacketizer();
            var replica = new TerritoryChunkReplica();
            TerritoryChunkSnapshot initial = new(
                3,
                new Dictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage>
                {
                    [new TerritoryChunkCoordinate(-2, 4)] =
                        TerritoryChunkCoverage.Full(new TerritoryChunkCoordinate(-2, 4)),
                    [new TerritoryChunkCoordinate(-1, 4)] =
                        TerritoryChunkCoverage.Full(new TerritoryChunkCoordinate(-1, 4)),
                    [new TerritoryChunkCoordinate(8, 9)] =
                        TerritoryChunkCoverage.Full(new TerritoryChunkCoordinate(8, 9))
                });
            ApplySnapshot(replica, packetizer, initial);

            TerritoryChunkCoverage[] changed =
            {
                TerritoryChunkCoverage.Full(new TerritoryChunkCoordinate(2, 4)),
                TerritoryChunkCoverage.Empty(new TerritoryChunkCoordinate(8, 9)),
                TerritoryChunkCoverage.Full(new TerritoryChunkCoordinate(1, 4)),
                TerritoryChunkCoverage.Full(new TerritoryChunkCoordinate(0, 4))
            };
            var result = new TerritoryChunkCommitResult(3, 4, changed);

            Assert.That(packetizer.TryCreateDelta(result, out var packets, out string reason),
                Is.True, reason);
            Assert.That(replica.TryBegin(
                TerritoryChunkTransferKind.Delta,
                3,
                4,
                packets.Count,
                out reason), Is.True, reason);
            for (int i = 0; i < packets.Count; i++)
                Assert.That(replica.TryAppend(packets[i], out reason), Is.True, reason);
            Assert.That(replica.TryComplete(out TerritoryChunkSnapshot actual, out reason),
                Is.True, reason);

            Assert.That(actual.Revision, Is.EqualTo(4));
            Assert.That(actual.GetFill(new TerritoryChunkCoordinate(8, 9)),
                Is.EqualTo(TerritoryChunkFill.Empty));
            for (int x = -2; x <= 2; x++)
            {
                Assert.That(actual.GetFill(new TerritoryChunkCoordinate(x, 4)),
                    Is.EqualTo(TerritoryChunkFill.Full));
            }
        }

        private static TerritoryChunkSnapshot CreateLargeSnapshot(ulong revision)
        {
            var chunks = new Dictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage>();
            for (int x = -4; x <= 4; x++)
            {
                var chunk = new TerritoryChunkCoordinate(x, -2);
                chunks.Add(chunk, TerritoryChunkCoverage.Full(chunk));
            }

            var boundaryChunk = new TerritoryChunkCoordinate(5, 6);
            var segments = new TerritoryChunkBoundarySegment[12];
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i] = new TerritoryChunkBoundarySegment(
                    20 + i,
                    new TerritoryChunkLocalPoint(i, i),
                    new TerritoryChunkLocalPoint(i + 1, i + 2));
            }
            chunks.Add(boundaryChunk, TerritoryChunkCoverage.Boundary(boundaryChunk, false, segments));
            return new TerritoryChunkSnapshot(revision, chunks);
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

        private static void AssertSnapshotsEqual(
            TerritoryChunkSnapshot expected,
            TerritoryChunkSnapshot actual)
        {
            Assert.That(actual.Revision, Is.EqualTo(expected.Revision));
            Assert.That(actual.Chunks.Count, Is.EqualTo(expected.Chunks.Count));
            foreach (KeyValuePair<TerritoryChunkCoordinate, TerritoryChunkCoverage> pair in expected.Chunks)
            {
                Assert.That(actual.TryGetCoverage(pair.Key, out TerritoryChunkCoverage coverage), Is.True);
                Assert.That(coverage, Is.EqualTo(pair.Value));
            }
        }
    }
}
