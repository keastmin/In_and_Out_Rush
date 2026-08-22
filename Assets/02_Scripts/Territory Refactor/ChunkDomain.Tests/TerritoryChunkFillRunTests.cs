using System;
using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryChunkFillRunTests
    {
        [Test]
        public void InclusiveRunCountsAndContainsNegativeChunksExactly()
        {
            var run = new TerritoryChunkFillRun(-3, -5, -2);

            Assert.That(run.ChunkCount, Is.EqualTo(4));
            Assert.That(run.Contains(new TerritoryChunkCoordinate(-5, -3)), Is.True);
            Assert.That(run.Contains(new TerritoryChunkCoordinate(-2, -3)), Is.True);
            Assert.That(run.Contains(new TerritoryChunkCoordinate(-1, -3)), Is.False);
            Assert.That(run.Contains(new TerritoryChunkCoordinate(-5, -2)), Is.False);
            Assert.That(run, Is.EqualTo(new TerritoryChunkFillRun(-3, -5, -2)));
        }

        [Test]
        public void ReversedRunIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new TerritoryChunkFillRun(0, 2, 1));
        }
    }
}
