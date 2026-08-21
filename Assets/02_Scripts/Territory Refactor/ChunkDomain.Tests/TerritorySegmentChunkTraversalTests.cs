using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritorySegmentChunkTraversalTests
    {
        private readonly List<TerritorySegmentChunkTraversal.SegmentPart> _parts = new();
        private readonly List<TerritorySegmentChunkTraversal.SegmentPart> _repeatParts = new();

        [Test]
        public void FixedPointConversionUsesAwayFromZeroMidpoints()
        {
            Assert.That(FixedTerritoryPoint.FromWorld(0.5 / 256.0, -0.5 / 256.0),
                Is.EqualTo(new FixedTerritoryPoint(1, -1)));
        }

        [TestCase(-2049, -2)]
        [TestCase(-2048, -1)]
        [TestCase(-1, -1)]
        [TestCase(0, 0)]
        [TestCase(2047, 0)]
        [TestCase(2048, 1)]
        public void ChunkOwnershipUsesMathematicalFloor(int x, int expectedChunkX)
        {
            TerritoryChunkCoordinate chunk =
                TerritoryChunkCoordinate.FromPoint(new FixedTerritoryPoint(x, 0));

            Assert.That(chunk.X, Is.EqualTo(expectedChunkX));
        }

        [Test]
        public void SplitVisitsEveryCrossedChunkInOrder()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            var start = new FixedTerritoryPoint(100, 100);
            var end = new FixedTerritoryPoint(size * 3 + 100, 100);

            TerritorySegmentChunkTraversal.Split(start, end, _parts);

            Assert.That(_parts.Count, Is.EqualTo(4));
            for (int i = 0; i < _parts.Count; i++)
                Assert.That(_parts[i].Chunk, Is.EqualTo(new TerritoryChunkCoordinate(i, 0)));

            Assert.That(_parts[0].Start, Is.EqualTo(start));
            Assert.That(_parts[^1].End, Is.EqualTo(end));
            for (int i = 1; i < _parts.Count; i++)
                Assert.That(_parts[i - 1].End, Is.EqualTo(_parts[i].Start));
        }

        [Test]
        public void ExactCornerCrossingDoesNotVisitSideChunks()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            var start = new FixedTerritoryPoint(100, 100);
            var end = new FixedTerritoryPoint(size * 2 + 100, size * 2 + 100);

            TerritorySegmentChunkTraversal.Split(start, end, _parts);

            Assert.That(_parts.Count, Is.EqualTo(3));
            Assert.That(_parts[0].Chunk, Is.EqualTo(new TerritoryChunkCoordinate(0, 0)));
            Assert.That(_parts[1].Chunk, Is.EqualTo(new TerritoryChunkCoordinate(1, 1)));
            Assert.That(_parts[2].Chunk, Is.EqualTo(new TerritoryChunkCoordinate(2, 2)));
            Assert.That(_parts[0].End, Is.EqualTo(new FixedTerritoryPoint(size, size)));
            Assert.That(_parts[1].End, Is.EqualTo(new FixedTerritoryPoint(size * 2, size * 2)));
        }

        [Test]
        public void NegativeMovementFromBoundarySkipsZeroLengthOwnerChunk()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            var start = new FixedTerritoryPoint(size, 100);
            var end = new FixedTerritoryPoint(100, 100);

            TerritorySegmentChunkTraversal.Split(start, end, _parts);

            Assert.That(_parts.Count, Is.EqualTo(1));
            Assert.That(_parts[0].Chunk, Is.EqualTo(new TerritoryChunkCoordinate(0, 0)));
            Assert.That(_parts[0].Start, Is.EqualTo(start));
            Assert.That(_parts[0].End, Is.EqualTo(end));
        }

        [Test]
        public void RandomizedTraversalIsContinuousAndDeterministic()
        {
            var random = new Random(20260821);
            for (int iteration = 0; iteration < 1000; iteration++)
            {
                var start = new FixedTerritoryPoint(random.Next(-20000, 20001), random.Next(-20000, 20001));
                var end = new FixedTerritoryPoint(random.Next(-20000, 20001), random.Next(-20000, 20001));

                TerritorySegmentChunkTraversal.Split(start, end, _parts);
                TerritorySegmentChunkTraversal.Split(start, end, _repeatParts);

                Assert.That(_repeatParts.Count, Is.EqualTo(_parts.Count));
                Assert.That(_parts[0].Start, Is.EqualTo(start));
                Assert.That(_parts[^1].End, Is.EqualTo(end));
                for (int i = 0; i < _parts.Count; i++)
                {
                    TerritorySegmentChunkTraversal.SegmentPart part = _parts[i];
                    TerritorySegmentChunkTraversal.SegmentPart repeated = _repeatParts[i];
                    Assert.That(part.Chunk, Is.EqualTo(repeated.Chunk));
                    Assert.That(part.Start, Is.EqualTo(repeated.Start));
                    Assert.That(part.End, Is.EqualTo(repeated.End));
                    Assert.That(part.Chunk.ContainsClosed(part.Start), Is.True);
                    Assert.That(part.Chunk.ContainsClosed(part.End), Is.True);
                    if (i > 0)
                        Assert.That(_parts[i - 1].End, Is.EqualTo(part.Start));
                }
            }
        }
    }
}
