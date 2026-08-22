using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryCompactSnapshotTests
    {
        [Test]
        public void InitialConversionCanonicalizesBoundaryAndCompressesFullRows()
        {
            int world = 1000 * FixedTerritoryPoint.UnitsPerWorldUnit;
            var builder = new TerritoryChunkStateBuilder();
            FixedTerritoryPoint[] clockwise =
            {
                new(0, 0),
                new(0, world),
                new(world, world),
                new(world, 0)
            };
            Assert.That(builder.TryBuild(clockwise, 7, out TerritoryChunkSnapshot source, out string reason), Is.True, reason);

            var compactBuilder = new TerritoryCompactSnapshotBuilder();
            Assert.That(compactBuilder.TryBuild(source, out TerritoryCompactSnapshot compact, out reason), Is.True, reason);

            Assert.That(compact.Revision, Is.EqualTo(source.Revision));
            Assert.That(compact.Boundary.SignedTwiceArea, Is.GreaterThan(0m));
            Assert.That(compact.Boundary.AbsoluteTwiceArea, Is.EqualTo(2m * world * world));
            Assert.That(compact.FullRowCount, Is.LessThan(126));
            Assert.That(compact.GetFill(new TerritoryChunkCoordinate(50, 50)), Is.EqualTo(TerritoryChunkFill.Full));
            Assert.That(compact.GetFill(new TerritoryChunkCoordinate(0, 0)), Is.EqualTo(TerritoryChunkFill.Boundary));
            Assert.That(compactBuilder.LastSourceChunkScanCount, Is.EqualTo(source.Chunks.Count));
            Assert.That(compactBuilder.LastSourceBoundarySegmentScanCount, Is.EqualTo(compact.Boundary.Count));

            Assert.That(TerritoryBoundaryLoopIndex.TryCreate(compact, out TerritoryBoundaryLoopIndex index, out reason), Is.True, reason);
            Assert.That(index.SegmentCount, Is.EqualTo(compact.Boundary.Count));
            Assert.That(index.AbsoluteTwiceArea, Is.EqualTo(compact.Boundary.AbsoluteTwiceArea));
        }
    }
}
