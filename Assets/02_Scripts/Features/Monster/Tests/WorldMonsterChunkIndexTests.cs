using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectIO.Monsters.Tests
{
    public sealed class WorldMonsterChunkIndexTests
    {
        [Test]
        public void CollectRange_ReturnsOnlyRecordsInsideRequestedChunks()
        {
            var index = new WorldMonsterChunkIndex<object>();
            var centerRecord = new object();
            var edgeRecord = new object();
            var farRecord = new object();
            var results = new List<object>();

            index.Add(new MonsterChunkCoordinate(0, 0), centerRecord);
            index.Add(new MonsterChunkCoordinate(1, -1), edgeRecord);
            index.Add(new MonsterChunkCoordinate(2, 0), farRecord);

            index.CollectRange(new MonsterChunkCoordinate(0, 0), 1, results);

            CollectionAssert.AreEquivalent(new[] { centerRecord, edgeRecord }, results);
        }

        [Test]
        public void CollectRange_ZeroRadiusReturnsCenterChunk()
        {
            var index = new WorldMonsterChunkIndex<object>();
            var record = new object();
            var results = new List<object>();

            index.Add(new MonsterChunkCoordinate(-3, 5), record);
            index.CollectRange(new MonsterChunkCoordinate(-3, 5), 0, results);

            CollectionAssert.AreEqual(new[] { record }, results);
        }

        [Test]
        public void Clear_RemovesAllRecords()
        {
            var index = new WorldMonsterChunkIndex<object>();
            var results = new List<object>();

            index.Add(new MonsterChunkCoordinate(0, 0), new object());
            index.Clear();
            index.CollectRange(new MonsterChunkCoordinate(0, 0), 4, results);

            Assert.That(results, Is.Empty);
        }
    }
}
