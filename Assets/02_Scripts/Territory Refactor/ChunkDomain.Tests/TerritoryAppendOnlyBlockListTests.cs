using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryAppendOnlyBlockListTests
    {
        [Test]
        public void LongHistoryUsesFixedBlocksAndReusesThemAfterClear()
        {
            const int blockCapacity = 64;
            const int itemCount = 100_000;
            var items = new TerritoryAppendOnlyBlockList<int>(blockCapacity);

            for (int index = 0; index < itemCount; index++)
                items.Add(index);

            int expectedBlockCount = (itemCount + blockCapacity - 1) / blockCapacity;
            Assert.That(items.Count, Is.EqualTo(itemCount));
            Assert.That(items.AllocatedBlockCount, Is.EqualTo(expectedBlockCount));
            Assert.That(items[0], Is.Zero);
            Assert.That(items[50_000], Is.EqualTo(50_000));
            Assert.That(items[^1], Is.EqualTo(itemCount - 1));

            items.Clear();
            Assert.That(items, Is.Empty);
            Assert.That(items.AllocatedBlockCount, Is.EqualTo(expectedBlockCount));

            for (int index = 0; index < itemCount; index++)
                items.Add(itemCount - index);

            Assert.That(items.AllocatedBlockCount, Is.EqualTo(expectedBlockCount));
            Assert.That(items[0], Is.EqualTo(itemCount));
            Assert.That(items[^1], Is.EqualTo(1));
        }
    }
}
