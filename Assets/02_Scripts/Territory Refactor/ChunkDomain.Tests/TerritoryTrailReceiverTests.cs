using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryTrailReceiverTests
    {
        [Test]
        public void OrderedPacketsRebuildExactSamplePathAndChunkFragments()
        {
            int chunkSize = TerritoryChunkCoordinate.SizeInFixedUnits;
            var receiver = new TerritoryTrailReceiver();
            Assert.That(receiver.TryBegin(40, out _), Is.True);

            TerritoryTrailPacket first = Packet(
                40,
                0,
                Sample(40, 0, 1, 100, 100),
                Sample(40, 1, 2, chunkSize + 100, 100));
            TerritoryTrailPacket second = Packet(
                40,
                1,
                Sample(40, 2, 3, 100, 200));

            Assert.That(receiver.TryAppendPacket(first, out _), Is.True);
            Assert.That(receiver.TryAppendPacket(second, out _), Is.True);

            var path = new List<FixedTerritoryPoint>();
            Assert.That(receiver.TryCopyActivePathTo(path), Is.True);
            Assert.That(path, Is.EqualTo(new[]
            {
                new FixedTerritoryPoint(100, 100),
                new FixedTerritoryPoint(chunkSize + 100, 100),
                new FixedTerritoryPoint(100, 200)
            }));

            Assert.That(receiver.TryCommit(40, out _), Is.True);
            Assert.That(receiver.Fragments.Count, Is.EqualTo(3));
            Assert.That(receiver.Fragments[0].Sequence, Is.EqualTo(0));
            Assert.That(receiver.Fragments[1].Sequence, Is.EqualTo(1));
            Assert.That(receiver.Fragments[2].Sequence, Is.EqualTo(2));
        }

        [Test]
        public void PacketGapIsRejectedWithoutAdvancingReceiver()
        {
            var receiver = new TerritoryTrailReceiver();
            Assert.That(receiver.TryBegin(12, out _), Is.True);

            TerritoryTrailPacket second = Packet(
                12,
                1,
                Sample(12, 1, 2, 20, 0));
            Assert.That(receiver.TryAppendPacket(second, out string gapReason), Is.False);
            Assert.That(gapReason, Does.Contain("Expected packet sequence 0"));

            TerritoryTrailPacket first = Packet(
                12,
                0,
                Sample(12, 0, 1, 10, 0));
            Assert.That(receiver.TryAppendPacket(first, out _), Is.True);
            Assert.That(receiver.Samples.Count, Is.EqualTo(1));
        }

        [Test]
        public void AbortRejectsStalePayloadAndBurnsConfirmedSamples()
        {
            var receiver = new TerritoryTrailReceiver();
            Assert.That(receiver.TryBegin(17, out _), Is.True);
            TerritoryTrailPacket packet = Packet(
                17,
                0,
                Sample(17, 0, 1, 10, 10),
                Sample(17, 1, 2, 20, 10));
            Assert.That(receiver.TryAppendPacket(packet, out _), Is.True);
            Assert.That(receiver.TryAbort(17, out _), Is.True);

            Assert.That(receiver.Samples, Is.Empty);
            Assert.That(receiver.TryAppendPacket(packet, out _), Is.False);
            Assert.That(receiver.TryBegin(17, out _), Is.False);
            Assert.That(receiver.TryBegin(18, out _), Is.True);
        }

        private static TerritoryTrailPacket Packet(
            ulong sessionId,
            uint packetSequence,
            params TerritoryTrailSample[] samples)
            => new(sessionId, packetSequence, samples[0].Sequence, samples);

        private static TerritoryTrailSample Sample(
            ulong sessionId,
            uint sequence,
            int tick,
            int x,
            int y)
            => new(sessionId, sequence, tick, new FixedTerritoryPoint(x, y));
    }
}
