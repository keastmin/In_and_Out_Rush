using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryChunkTransferPacketizerTests
    {
        [Test]
        public void InitialDeltaRoundTripPreservesRunsAndBoundaryAcrossBoundedPackets()
        {
            TerritoryChunkCoverage[] changed = CreateLargeCoverage();
            var result = new TerritoryChunkCommitResult(0, 1, changed);
            var packetizer = new TerritoryChunkTransferPacketizer();

            Assert.That(packetizer.TryCreateDelta(result, out var packets, out string reason),
                Is.True, reason);
            Assert.That(packets.Count, Is.GreaterThan(1));
            for (int i = 0; i < packets.Count; i++)
            {
                Assert.That(packets[i].BaseRevision, Is.EqualTo(0));
                Assert.That(packets[i].Revision, Is.EqualTo(1));
                Assert.That(packets[i].Sequence, Is.EqualTo((uint)i));
                Assert.That(packets[i].Words.Count,
                    Is.InRange(1, TerritoryChunkTransferPacketizer.MaximumWordCount));
            }

            var replica = new TerritoryChunkReplica();
            ApplyDelta(replica, packets, 0, 1);

            Assert.That(replica.Current.Revision, Is.EqualTo(1));
            Assert.That(replica.Current.Chunks.Count, Is.EqualTo(changed.Length));
            for (int i = 0; i < changed.Length; i++)
            {
                Assert.That(replica.Current.TryGetCoverage(
                    changed[i].Chunk,
                    out TerritoryChunkCoverage actual), Is.True);
                Assert.That(actual, Is.EqualTo(changed[i]));
            }
        }

        [Test]
        public void DeltaRoundTripAppliesFullAndEmptyRunsInDeterministicOrder()
        {
            var packetizer = new TerritoryChunkTransferPacketizer();
            var replica = new TerritoryChunkReplica();
            TerritoryChunkCoverage[] initial =
            {
                TerritoryChunkCoverage.Full(new TerritoryChunkCoordinate(-2, 4)),
                TerritoryChunkCoverage.Full(new TerritoryChunkCoordinate(-1, 4)),
                TerritoryChunkCoverage.Full(new TerritoryChunkCoordinate(8, 9))
            };
            ApplyResult(replica, packetizer, new TerritoryChunkCommitResult(0, 1, initial));

            TerritoryChunkCoverage[] changed =
            {
                TerritoryChunkCoverage.Full(new TerritoryChunkCoordinate(2, 4)),
                TerritoryChunkCoverage.Empty(new TerritoryChunkCoordinate(8, 9)),
                TerritoryChunkCoverage.Full(new TerritoryChunkCoordinate(1, 4)),
                TerritoryChunkCoverage.Full(new TerritoryChunkCoordinate(0, 4))
            };
            ApplyResult(replica, packetizer, new TerritoryChunkCommitResult(1, 2, changed));

            Assert.That(replica.Current.Revision, Is.EqualTo(2));
            Assert.That(replica.Current.GetFill(new TerritoryChunkCoordinate(8, 9)),
                Is.EqualTo(TerritoryChunkFill.Empty));
            for (int x = -2; x <= 2; x++)
            {
                Assert.That(replica.Current.GetFill(new TerritoryChunkCoordinate(x, 4)),
                    Is.EqualTo(TerritoryChunkFill.Full));
            }
        }

        private static TerritoryChunkCoverage[] CreateLargeCoverage()
        {
            var coverage = new List<TerritoryChunkCoverage>();
            for (int x = -4; x <= 4; x++)
            {
                var chunk = new TerritoryChunkCoordinate(x, -2);
                coverage.Add(TerritoryChunkCoverage.Full(chunk));
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
            coverage.Add(TerritoryChunkCoverage.Boundary(
                boundaryChunk,
                false,
                segments));
            return coverage.ToArray();
        }

        private static void ApplyResult(
            TerritoryChunkReplica replica,
            TerritoryChunkTransferPacketizer packetizer,
            TerritoryChunkCommitResult result)
        {
            Assert.That(packetizer.TryCreateDelta(result, out var packets, out string reason),
                Is.True, reason);
            ApplyDelta(replica, packets, result.BaseRevision, result.Revision);
        }

        private static void ApplyDelta(
            TerritoryChunkReplica replica,
            IReadOnlyList<TerritoryChunkTransferPacket> packets,
            ulong baseRevision,
            ulong revision)
        {
            Assert.That(replica.TryBegin(
                baseRevision,
                revision,
                packets.Count,
                out string reason), Is.True, reason);
            for (int i = 0; i < packets.Count; i++)
                Assert.That(replica.TryAppend(packets[i], out reason), Is.True, reason);
            Assert.That(replica.TryComplete(out _, out reason), Is.True, reason);
        }
    }
}
