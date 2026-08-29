using NUnit.Framework;
using UnityEngine;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryTrailSegmentIndexTests
    {
        [Test]
        public void NegativeCoordinatesUseTheSameMathematicalChunkOwnership()
        {
            var index = new TerritoryTrailSegmentIndex();
            index.Add(new Vector2(-16f, -16f), new Vector2(-1f, -16f));

            Assert.That(index.Intersects(new Vector2(-12f, -20f), new Vector2(-12f, -12f),
                    Vector2.zero, Vector2.zero), Is.True);
        }

        [Test]
        public void LongDiagonalFindsAnIntersectionAcrossEveryTraversedChunk()
        {
            var index = new TerritoryTrailSegmentIndex();
            index.Add(new Vector2(-24f, 4f), new Vector2(-16f, 12f));

            Assert.That(index.Intersects(new Vector2(-32f, 16f), new Vector2(0f, -16f),
                    Vector2.zero, Vector2.zero), Is.True);
        }

        [Test]
        public void SharedEndpointCountsAsIntersectionUnlessTheSegmentIsIgnored()
        {
            var index = new TerritoryTrailSegmentIndex();
            Vector2 segmentStart = new(0f, 0f);
            Vector2 segmentEnd = new(8f, 0f);
            index.Add(segmentStart, segmentEnd);

            Assert.That(index.Intersects(new Vector2(8f, -2f), new Vector2(8f, 2f),
                    Vector2.zero, Vector2.zero), Is.True);
            Assert.That(index.Intersects(new Vector2(8f, -2f), new Vector2(8f, 2f),
                    segmentStart, segmentEnd), Is.False);
        }
    }
}
