using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryCompactStoreTests
    {
        [Test]
        public void HundredExpansionsInThousandWorldKeepCompactStateAndStableRetainedIdentity()
        {
            int world = 1000 * FixedTerritoryPoint.UnitsPerWorldUnit;
            TerritoryCompactSnapshot initial = TerritoryCompactApplySessionTests.BuildCompactRectangle(0, 0, world, world);
            TerritoryBoundarySegmentId retainedId = FindLeftBoundaryId(initial);
            var store = new TerritoryCompactStore(initial);

            for (ulong sessionId = 1; sessionId <= 100; sessionId++)
            {
                TerritoryCompactBoundarySegment edge = FindOutermostUpwardEdge(store.Current);
                FixedTerritoryPoint start = edge.GlobalStart;
                FixedTerritoryPoint end = edge.GlobalEnd;
                Assert.That(end.Y - start.Y, Is.GreaterThan(256));
                FixedTerritoryPoint exit = new(start.X, start.Y + 1);
                FixedTerritoryPoint entry = new(end.X, end.Y - 1);
                FixedTerritoryPoint outerExit = new(exit.X + FixedTerritoryPoint.UnitsPerWorldUnit, exit.Y);
                FixedTerritoryPoint outerEntry = new(entry.X + FixedTerritoryPoint.UnitsPerWorldUnit, entry.Y);
                TerritoryChunkExpansionMaterialization materialization = TerritoryCompactApplySessionTests.Materialize(
                    store.Current,
                    sessionId,
                    exit,
                    outerExit,
                    outerEntry,
                    entry);

                Assert.That(store.TryBeginApply(materialization, out TerritoryCompactApplySession apply, out string reason), Is.True, reason);
                bool completed = false;
                int guard = 0;
                while (!completed && guard++ < 100000)
                    Assert.That(apply.TryStep(31, out _, out completed, out reason), Is.True, reason);
                Assert.That(completed, Is.True);
                Assert.That(store.TryPublish(apply, out TerritoryCompactCommitResult commit, out reason), Is.True, reason);
                Assert.That(commit.Metrics.SourceWideBoundaryScans, Is.Zero);
                Assert.That(commit.Metrics.SourceWideFullChunkScans, Is.Zero);
                Assert.That(commit.Metrics.GlobalBoundaryRenumbers, Is.Zero);
                Assert.That(commit.Metrics.ExpandedFullChunkObjects, Is.Zero);
                Assert.That(commit.Metrics.UnchangedNodeCopies, Is.Zero);
                Assert.That(commit.Metrics.RemovedBoundarySegments, Is.LessThan(8));
                Assert.That(store.Current.Boundary.TryGetSegment(retainedId, out _), Is.True);
            }

            Assert.That(store.Current.Revision, Is.EqualTo(101));
            Assert.That(initial.Revision, Is.EqualTo(1));
            Assert.That(store.Current.FullRowCount, Is.LessThanOrEqualTo(126));
            Assert.That(store.Current.GetFill(new TerritoryChunkCoordinate(50, 50)), Is.EqualTo(TerritoryChunkFill.Full));
        }

        [Test]
        public void StoreRejectsPublishingACompletedCandidateAfterCurrentRevisionChanges()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            TerritoryCompactSnapshot source = TerritoryCompactApplySessionTests.BuildCompactRectangle(0, 0, size * 5, size * 5);
            TerritoryChunkExpansionMaterialization first = TerritoryCompactApplySessionTests.Materialize(
                source,
                1,
                new FixedTerritoryPoint(size * 4, size),
                new FixedTerritoryPoint(size * 7, size),
                new FixedTerritoryPoint(size * 7, size * 3),
                new FixedTerritoryPoint(size * 4, size * 3));
            var store = new TerritoryCompactStore(source);
            Assert.That(store.TryBeginApply(first, out TerritoryCompactApplySession firstApply, out string reason), Is.True, reason);
            Assert.That(store.TryBeginApply(first, out TerritoryCompactApplySession staleApply, out reason), Is.True, reason);
            Complete(firstApply);
            Complete(staleApply);
            Assert.That(store.TryPublish(firstApply, out _, out reason), Is.True, reason);
            Assert.That(store.TryPublish(staleApply, out _, out reason), Is.False);
            Assert.That(reason, Does.Contain("stale"));
        }

        private static void Complete(TerritoryCompactApplySession session)
        {
            bool completed = false;
            int guard = 0;
            while (!completed && guard++ < 100000)
                Assert.That(session.TryStep(100, out _, out completed, out string reason), Is.True, reason);
            Assert.That(completed, Is.True);
        }

        private static TerritoryCompactBoundarySegment FindOutermostUpwardEdge(TerritoryCompactSnapshot snapshot)
        {
            TerritoryCompactBoundarySegment selected = default;
            bool found = false;
            for (int i = 0; i < snapshot.Boundary.Count; i++)
            {
                Assert.That(snapshot.Boundary.TryGetOrderedSegment(i, out TerritoryCompactBoundarySegment segment), Is.True);
                FixedTerritoryPoint start = segment.GlobalStart;
                FixedTerritoryPoint end = segment.GlobalEnd;
                if (start.X != end.X || end.Y <= start.Y)
                    continue;
                if (!found || start.X > selected.GlobalStart.X ||
                    (start.X == selected.GlobalStart.X && end.Y - start.Y > selected.GlobalEnd.Y - selected.GlobalStart.Y))
                {
                    selected = segment;
                    found = true;
                }
            }
            Assert.That(found, Is.True);
            return selected;
        }

        private static TerritoryBoundarySegmentId FindLeftBoundaryId(TerritoryCompactSnapshot snapshot)
        {
            for (int i = 0; i < snapshot.Boundary.Count; i++)
            {
                Assert.That(snapshot.Boundary.TryGetOrderedSegment(i, out TerritoryCompactBoundarySegment segment), Is.True);
                if (segment.GlobalStart.X == 0 && segment.GlobalEnd.X == 0)
                    return segment.Id;
            }
            Assert.Fail("Initial compact rectangle has no left Boundary segment.");
            return default;
        }
    }
}
