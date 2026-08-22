using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryPersistentBoundaryTreeTests
    {
        [Test]
        public void CanonicalTreeKeepsStableIdentityOrderAndPrefixArea()
        {
            var chunk = new TerritoryChunkCoordinate(0, 0);
            TerritoryCompactBoundarySegment[] segments =
            {
                Segment(11, chunk, 100, 100, 900, 100),
                Segment(12, chunk, 900, 100, 900, 900),
                Segment(13, chunk, 900, 900, 100, 900),
                Segment(14, chunk, 100, 900, 100, 100)
            };

            Assert.That(TerritoryPersistentBoundaryTree.TryCreate(segments, out TerritoryPersistentBoundaryTree tree, out string reason), Is.True, reason);
            Assert.That(tree.Count, Is.EqualTo(4));
            Assert.That(tree.SignedTwiceArea, Is.EqualTo(1280000m));
            Assert.That(tree.TryGetNext(new TerritoryBoundarySegmentId(14), out TerritoryCompactBoundarySegment wrapped), Is.True);
            Assert.That(wrapped.Id, Is.EqualTo(new TerritoryBoundarySegmentId(11)));
            Assert.That(tree.GetForwardArcTwiceArea(
                new TerritoryBoundarySegmentId(11),
                new FixedTerritoryPoint(500, 100),
                new TerritoryBoundarySegmentId(13),
                new FixedTerritoryPoint(500, 900)), Is.EqualTo(1040000m));
        }

        [Test]
        public void ClockwiseTreeIsRejectedInsteadOfSilentlyChangingMeaning()
        {
            var chunk = new TerritoryChunkCoordinate(0, 0);
            TerritoryCompactBoundarySegment[] clockwise =
            {
                Segment(1, chunk, 100, 100, 100, 900),
                Segment(2, chunk, 100, 900, 900, 900),
                Segment(3, chunk, 900, 900, 900, 100),
                Segment(4, chunk, 900, 100, 100, 100)
            };

            Assert.That(TerritoryPersistentBoundaryTree.TryCreate(clockwise, out _, out string reason), Is.False);
            Assert.That(reason, Does.Contain("counter-clockwise"));
        }

        private static TerritoryCompactBoundarySegment Segment(
            ulong id,
            TerritoryChunkCoordinate chunk,
            int startX,
            int startY,
            int endX,
            int endY)
            => new(
                new TerritoryBoundarySegmentId(id),
                chunk,
                new TerritoryChunkLocalPoint(startX, startY),
                new TerritoryChunkLocalPoint(endX, endY));
    }
}
