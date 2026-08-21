using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryTrailPacketizerTests
    {
        [Test]
        public void LongTrailIsSplitIntoBoundedOrderedPackets()
        {
            var packetizer = new TerritoryTrailPacketizer();
            Assert.That(packetizer.TryBegin(31, out _), Is.True);

            for (uint sequence = 0; sequence < 50; sequence++)
            {
                Assert.That(
                    packetizer.TryAppendSample(Sample(31, sequence, (int)sequence), out _),
                    Is.True);
            }

            Assert.That(packetizer.TryCommit(out string pendingReason), Is.False);
            Assert.That(pendingReason, Does.Contain("Pending samples"));

            Assert.That(packetizer.TryTakePacket(24, out TerritoryTrailPacket first, out _), Is.True);
            Assert.That(packetizer.TryTakePacket(24, out TerritoryTrailPacket second, out _), Is.True);
            Assert.That(packetizer.TryTakePacket(24, out TerritoryTrailPacket third, out _), Is.True);

            Assert.That(first.Sequence, Is.EqualTo(0));
            Assert.That(second.Sequence, Is.EqualTo(1));
            Assert.That(third.Sequence, Is.EqualTo(2));
            Assert.That(first.Samples.Count, Is.EqualTo(24));
            Assert.That(second.Samples.Count, Is.EqualTo(24));
            Assert.That(third.Samples.Count, Is.EqualTo(2));
            Assert.That(first.FirstSampleSequence, Is.EqualTo(0));
            Assert.That(second.FirstSampleSequence, Is.EqualTo(24));
            Assert.That(third.FirstSampleSequence, Is.EqualTo(48));
            Assert.That(packetizer.TryCommit(out _), Is.True);
        }

        [Test]
        public void SequenceGapAndDecreasingTickAreRejected()
        {
            var packetizer = new TerritoryTrailPacketizer();
            Assert.That(packetizer.TryBegin(4, out _), Is.True);
            Assert.That(packetizer.TryAppendSample(Sample(4, 0, 10), out _), Is.True);
            Assert.That(packetizer.TryTakePacket(24, out _, out _), Is.True);

            Assert.That(
                packetizer.TryAppendSample(Sample(4, 2, 11), out string sequenceReason),
                Is.False);
            Assert.That(sequenceReason, Does.Contain("Expected sample sequence 1"));
            Assert.That(
                packetizer.TryAppendSample(Sample(4, 1, 9), out string tickReason),
                Is.False);
            Assert.That(tickReason, Does.Contain("nondecreasing"));
        }

        [Test]
        public void AbortBurnsPendingSamplesAndRequiresNewerSession()
        {
            var packetizer = new TerritoryTrailPacketizer();
            Assert.That(packetizer.TryBegin(8, out _), Is.True);
            Assert.That(packetizer.TryAppendSample(Sample(8, 0, 1), out _), Is.True);
            Assert.That(packetizer.TryAbort(out _), Is.True);

            Assert.That(packetizer.PendingSampleCount, Is.Zero);
            Assert.That(packetizer.TryBegin(8, out _), Is.False);
            Assert.That(packetizer.TryBegin(9, out _), Is.True);
        }

        [Test]
        public void WireCodecRoundTripPreservesFixedCoordinatesAndTick()
        {
            var source = new TerritoryTrailPacket(
                22,
                3,
                7,
                new[]
                {
                    new TerritoryTrailSample(22, 7, 100, new FixedTerritoryPoint(-2048, 4096)),
                    new TerritoryTrailSample(22, 8, 101, new FixedTerritoryPoint(17, -33))
                });

            int[] payload = TerritoryTrailPacket.Encode(source);
            Assert.That(
                TerritoryTrailPacket.TryDecode(22, 3, 7, payload, out TerritoryTrailPacket decoded, out _),
                Is.True);

            Assert.That(decoded.SessionId, Is.EqualTo(source.SessionId));
            Assert.That(decoded.Sequence, Is.EqualTo(source.Sequence));
            Assert.That(decoded.Samples[0].SimulationTick, Is.EqualTo(100));
            Assert.That(decoded.Samples[0].Point, Is.EqualTo(new FixedTerritoryPoint(-2048, 4096)));
            Assert.That(decoded.Samples[1].Point, Is.EqualTo(new FixedTerritoryPoint(17, -33)));
        }

        private static TerritoryTrailSample Sample(ulong sessionId, uint sequence, int tick)
            => new(
                sessionId,
                sequence,
                tick,
                new FixedTerritoryPoint((int)sequence * 16, tick));
    }
}
