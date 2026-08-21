using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryChunkStoreTests
    {
        [Test]
        public void InitialAndRepeatedCommitAdvanceRevisionWithDeterministicDelta()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            var store = new TerritoryChunkStore();
            FixedTerritoryPoint[] polygon = Rectangle(100, 100, size * 3 - 100, size * 3 - 100);

            Assert.That(store.TryCommit(0, polygon, out TerritoryChunkCommitResult initial, out _), Is.True);
            Assert.That(initial.BaseRevision, Is.Zero);
            Assert.That(initial.Revision, Is.EqualTo(1));
            Assert.That(initial.ChangedChunks, Is.Not.Empty);
            Assert.That(store.Current.Revision, Is.EqualTo(1));

            Assert.That(store.TryCommit(1, polygon, out TerritoryChunkCommitResult repeated, out _), Is.True);
            Assert.That(repeated.Revision, Is.EqualTo(2));
            Assert.That(repeated.ChangedChunks, Is.Empty);
            Assert.That(store.Current.Revision, Is.EqualTo(2));
        }

        [Test]
        public void ShrinkingCoverageProducesOrderedEmptyTombstones()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            var store = new TerritoryChunkStore();
            Assert.That(store.TryCommit(
                0,
                Rectangle(100, 100, size * 4 - 100, size * 3 - 100),
                out _,
                out _), Is.True);

            Assert.That(store.TryCommit(
                1,
                Rectangle(100, 100, size - 100, size - 100),
                out TerritoryChunkCommitResult result,
                out _), Is.True);

            Assert.That(result.ChangedChunks, Has.Some.Matches<TerritoryChunkCoverage>(
                coverage => coverage.Fill == TerritoryChunkFill.Empty));
            for (int i = 1; i < result.ChangedChunks.Count; i++)
            {
                TerritoryChunkCoordinate previous = result.ChangedChunks[i - 1].Chunk;
                TerritoryChunkCoordinate current = result.ChangedChunks[i].Chunk;
                Assert.That(
                    current.Y > previous.Y ||
                    (current.Y == previous.Y && current.X > previous.X),
                    Is.True);
            }
        }

        [Test]
        public void StaleAndInvalidCommitPreserveThePublishedSnapshot()
        {
            var store = new TerritoryChunkStore();
            FixedTerritoryPoint[] valid = Rectangle(0, 0, 1000, 1000);
            Assert.That(store.TryCommit(0, valid, out _, out _), Is.True);
            TerritoryChunkSnapshot published = store.Current;

            Assert.That(store.TryCommit(0, valid, out _, out string staleReason), Is.False);
            Assert.That(staleReason, Does.Contain("Expected base revision 1"));
            Assert.That(store.Current, Is.SameAs(published));

            FixedTerritoryPoint[] invalid =
            {
                new(0, 0),
                new(1000, 1000),
                new(0, 1000),
                new(1000, 0)
            };
            Assert.That(store.TryCommit(1, invalid, out _, out _), Is.False);
            Assert.That(store.Current, Is.SameAs(published));
            Assert.That(store.Current.Revision, Is.EqualTo(1));
        }

        [Test]
        public void RevisionOverflowRejectsCommitWithoutBuildingOrMutation()
        {
            var builder = new TerritoryChunkStateBuilder();
            FixedTerritoryPoint[] polygon = Rectangle(0, 0, 1000, 1000);
            Assert.That(builder.TryBuild(
                polygon,
                ulong.MaxValue,
                out TerritoryChunkSnapshot maximum,
                out _), Is.True);
            var store = new TerritoryChunkStore(maximum);

            Assert.That(store.TryCommit(
                ulong.MaxValue,
                polygon,
                out _,
                out string reason), Is.False);
            Assert.That(reason, Does.Contain("exhausted"));
            Assert.That(store.Current, Is.SameAs(maximum));
        }

        private static FixedTerritoryPoint[] Rectangle(int minimumX, int minimumY, int maximumX, int maximumY)
            => new[]
            {
                new FixedTerritoryPoint(minimumX, minimumY),
                new FixedTerritoryPoint(maximumX, minimumY),
                new FixedTerritoryPoint(maximumX, maximumY),
                new FixedTerritoryPoint(minimumX, maximumY)
            };
    }
}
