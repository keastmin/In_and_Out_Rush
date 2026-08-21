using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryTrailSessionTests
    {
        [Test]
        public void RevisitedChunkProducesSeparateOrderedFragment()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            var session = new TerritoryTrailSession();

            Assert.That(session.TryBegin(Sample(7, 0, 10, 100, 100), out _), Is.True);
            Assert.That(session.TryAppendSample(Sample(7, 1, 11, size + 100, 100), out _), Is.True);
            Assert.That(session.TryAppendSample(Sample(7, 2, 12, 100, 200), out _), Is.True);
            Assert.That(session.TryCommit(7, out _), Is.True);

            Assert.That(session.Fragments.Count, Is.EqualTo(3));
            Assert.That(session.Fragments[0].Chunk, Is.EqualTo(new TerritoryChunkCoordinate(0, 0)));
            Assert.That(session.Fragments[1].Chunk, Is.EqualTo(new TerritoryChunkCoordinate(1, 0)));
            Assert.That(session.Fragments[2].Chunk, Is.EqualTo(new TerritoryChunkCoordinate(0, 0)));
            Assert.That(session.Fragments[0].Sequence, Is.EqualTo(0));
            Assert.That(session.Fragments[1].Sequence, Is.EqualTo(1));
            Assert.That(session.Fragments[2].Sequence, Is.EqualTo(2));

            var reconstructed = new List<FixedTerritoryPoint>();
            session.CopyFragmentPathTo(reconstructed);
            Assert.That(reconstructed[0], Is.EqualTo(new FixedTerritoryPoint(100, 100)));
            Assert.That(reconstructed[^1], Is.EqualTo(new FixedTerritoryPoint(100, 200)));
            Assert.That(reconstructed, Does.Contain(new FixedTerritoryPoint(size + 100, 100)));
        }

        [Test]
        public void SampleSequenceGapIsRejectedWithoutMutation()
        {
            var session = new TerritoryTrailSession();
            Assert.That(session.TryBegin(Sample(3, 0, 20, 0, 0), out _), Is.True);

            bool accepted = session.TryAppendSample(Sample(3, 2, 21, 10, 0), out string reason);

            Assert.That(accepted, Is.False);
            Assert.That(reason, Does.Contain("Expected sample sequence 1"));
            Assert.That(session.Samples.Count, Is.EqualTo(1));
            Assert.That(session.Fragments, Is.Empty);
        }

        [Test]
        public void AbortBurnsPayloadAndRejectsStaleSessionData()
        {
            var session = new TerritoryTrailSession();
            Assert.That(session.TryBegin(Sample(11, 0, 30, 100, 100), out _), Is.True);
            Assert.That(session.TryAppendSample(Sample(11, 1, 31, 200, 100), out _), Is.True);
            Assert.That(session.TryAbort(11, out _), Is.True);

            Assert.That(session.Status, Is.EqualTo(TerritoryTrailSessionStatus.Aborted));
            Assert.That(session.Samples, Is.Empty);
            Assert.That(session.Fragments, Is.Empty);
            Assert.That(session.TryAppendSample(Sample(11, 2, 32, 300, 100), out _), Is.False);
            Assert.That(session.TryAppendFragment(Fragment(11, 0, 100, 100, 200, 100), out _), Is.False);
            Assert.That(session.TryBegin(11, out _), Is.False);
            Assert.That(session.TryBegin(12, out _), Is.True);
        }

        [Test]
        public void FragmentReceiverRejectsGapAndDisconnectedGeometry()
        {
            var session = new TerritoryTrailSession();
            Assert.That(session.TryBegin(20, out _), Is.True);
            Assert.That(session.TryAppendFragment(Fragment(20, 1, 100, 100, 200, 100), out _), Is.False);
            Assert.That(session.TryAppendFragment(Fragment(20, 0, 100, 100, 200, 100), out _), Is.True);

            TerritoryTrailFragment disconnected = Fragment(20, 1, 300, 100, 400, 100);
            Assert.That(session.TryAppendFragment(disconnected, out string reason), Is.False);
            Assert.That(reason, Does.Contain("share one boundary point"));
            Assert.That(session.Fragments.Count, Is.EqualTo(1));
        }

        private static TerritoryTrailSample Sample(
            ulong sessionId,
            uint sequence,
            int tick,
            int x,
            int y)
            => new(sessionId, sequence, tick, new FixedTerritoryPoint(x, y));

        private static TerritoryTrailFragment Fragment(
            ulong sessionId,
            uint sequence,
            int startX,
            int startY,
            int endX,
            int endY)
        {
            var start = new FixedTerritoryPoint(startX, startY);
            var end = new FixedTerritoryPoint(endX, endY);
            return new TerritoryTrailFragment(
                sessionId,
                sequence,
                TerritoryChunkCoordinate.FromPoint(start),
                sequence,
                sequence + 1,
                new[] { start, end });
        }
    }
}
