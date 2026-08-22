using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryBoundaryLoopIndexTests
    {
        private readonly TerritoryChunkStateBuilder _builder = new();

        [Test]
        public void ValidMultiChunkLoopRestoresOrderedFixedBoundaryAndArea()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            FixedTerritoryPoint[] polygon = Rectangle(-size - 300, -700, size * 2 + 111, size + 900);

            Assert.That(_builder.TryBuild(polygon, 11, out TerritoryChunkSnapshot snapshot, out _), Is.True);
            Assert.That(TerritoryBoundaryLoopIndex.TryCreate(snapshot, out TerritoryBoundaryLoopIndex index, out string reason),
                Is.True, reason);

            Assert.That(index.Revision, Is.EqualTo(11));
            Assert.That(index.SegmentCount, Is.GreaterThan(4));
            Assert.That(index.AbsoluteTwiceArea, Is.EqualTo(
                2m * (polygon[1].X - (decimal)polygon[0].X) * (polygon[2].Y - (decimal)polygon[1].Y)));

            Assert.That(index.TryGetOrderedSegment(0, out FixedTerritoryPoint firstStart, out _), Is.True);
            Assert.That(firstStart, Is.EqualTo(polygon[0]));
            for (int i = 0; i < index.SegmentCount; i++)
            {
                Assert.That(index.TryGetOrderedSegment(i, out _, out FixedTerritoryPoint end), Is.True);
                Assert.That(index.TryGetOrderedSegment((i + 1) % index.SegmentCount, out FixedTerritoryPoint nextStart, out _), Is.True);
                Assert.That(end, Is.EqualTo(nextStart));
            }
        }

        [Test]
        public void MissingSequenceRejectsIndexWithoutPublishingPartialResult()
        {
            TerritoryChunkSnapshot snapshot = ManualSnapshot(
                new TerritoryChunkBoundarySegment(0, Local(100, 100), Local(500, 100)),
                new TerritoryChunkBoundarySegment(2, Local(500, 100), Local(500, 500)),
                new TerritoryChunkBoundarySegment(3, Local(500, 500), Local(100, 100)));

            Assert.That(TerritoryBoundaryLoopIndex.TryCreate(snapshot, out TerritoryBoundaryLoopIndex index, out string reason), Is.False);
            Assert.That(index, Is.Null);
            Assert.That(reason, Does.Contain("sequence 1"));
        }

        [Test]
        public void DisconnectedSequenceRejectsOpenOrMultipleLoopEncoding()
        {
            TerritoryChunkSnapshot snapshot = ManualSnapshot(
                new TerritoryChunkBoundarySegment(0, Local(100, 100), Local(500, 100)),
                new TerritoryChunkBoundarySegment(1, Local(700, 700), Local(100, 700)),
                new TerritoryChunkBoundarySegment(2, Local(100, 700), Local(100, 100)));

            Assert.That(TerritoryBoundaryLoopIndex.TryCreate(snapshot, out TerritoryBoundaryLoopIndex index, out string reason), Is.False);
            Assert.That(index, Is.Null);
            Assert.That(reason, Does.Contain("does not connect"));
        }

        [Test]
        public void RevisionZeroCannotCreateAnIndex()
        {
            var snapshot = new TerritoryChunkSnapshot(
                0,
                new Dictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage>());

            Assert.That(TerritoryBoundaryLoopIndex.TryCreate(snapshot, out _, out string reason), Is.False);
            Assert.That(reason, Does.Contain("Revision zero"));
        }

        [Test]
        public void GlobalEndpointOverflowRejectsIndexAtomically()
        {
            var chunk = new TerritoryChunkCoordinate(int.MaxValue, 0);
            var segments = new[]
            {
                new TerritoryChunkBoundarySegment(0, Local(0, 0), Local(100, 0)),
                new TerritoryChunkBoundarySegment(1, Local(100, 0), Local(100, 100)),
                new TerritoryChunkBoundarySegment(2, Local(100, 100), Local(0, 0))
            };
            var snapshot = new TerritoryChunkSnapshot(
                1,
                new Dictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage>
                {
                    [chunk] = TerritoryChunkCoverage.Boundary(chunk, true, segments)
                });

            Assert.That(TerritoryBoundaryLoopIndex.TryCreate(snapshot, out TerritoryBoundaryLoopIndex index, out string reason), Is.False);
            Assert.That(index, Is.Null);
            Assert.That(reason, Does.Contain("fixed coordinate range"));
        }

        private static TerritoryChunkSnapshot ManualSnapshot(params TerritoryChunkBoundarySegment[] segments)
        {
            var chunk = new TerritoryChunkCoordinate(0, 0);
            return new TerritoryChunkSnapshot(
                1,
                new Dictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage>
                {
                    [chunk] = TerritoryChunkCoverage.Boundary(chunk, true, segments)
                });
        }

        private static TerritoryChunkLocalPoint Local(int x, int y)
            => new(x, y);

        private static FixedTerritoryPoint[] Rectangle(int minX, int minY, int maxX, int maxY)
            => new[]
            {
                new FixedTerritoryPoint(minX, minY),
                new FixedTerritoryPoint(maxX, minY),
                new FixedTerritoryPoint(maxX, maxY),
                new FixedTerritoryPoint(minX, maxY)
            };
    }
}
