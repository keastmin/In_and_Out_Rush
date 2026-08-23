using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryCompactExpansionWorkerTests
    {
        [Test]
        public void HundredQueuedExpansionsPublishInOrderAndMatchSynchronousPersistentChain()
        {
            int world = 1000 * FixedTerritoryPoint.UnitsPerWorldUnit;
            TerritoryCompactSnapshot initial = TerritoryCompactApplySessionTests.BuildCompactRectangle(0, 0, world, world);
            TerritoryCompactSnapshot expected = initial;
            var queued = new List<TerritoryCompactExpansionWorkItem>();

            for (ulong sessionId = 1; sessionId <= 100; sessionId++)
            {
                TerritoryCompactBoundarySegment edge = FindOutermostUpwardEdge(expected);
                FixedTerritoryPoint exit = new(edge.GlobalStart.X, edge.GlobalStart.Y + 1);
                FixedTerritoryPoint entry = new(edge.GlobalEnd.X, edge.GlobalEnd.Y - 1);
                FixedTerritoryPoint outerExit = new(exit.X + FixedTerritoryPoint.UnitsPerWorldUnit, exit.Y);
                FixedTerritoryPoint outerEntry = new(entry.X + FixedTerritoryPoint.UnitsPerWorldUnit, entry.Y);
                List<TerritoryTrailFragment> fragments = BuildFragments(
                    sessionId,
                    exit,
                    outerExit,
                    outerEntry,
                    entry);
                Assert.That(TerritoryCompactExpansionWorkItem.TryTakeOwnership(
                    sessionId,
                    expected.Revision,
                    fragments,
                    out TerritoryCompactExpansionWorkItem item,
                    out string reason), Is.True, reason);
                queued.Add(item);
                expected = CompleteSynchronously(expected, item);
            }

            var store = new TerritoryCompactStore(initial);
            using var worker = new TerritoryCompactExpansionWorker(initial);
            for (int i = 0; i < queued.Count; i++)
                Assert.That(worker.TryEnqueue(queued[i], out string reason), Is.True, reason);

            int published = 0;
            var timeout = Stopwatch.StartNew();
            while (published < queued.Count && timeout.Elapsed < TimeSpan.FromSeconds(30))
            {
                if (!worker.TryTakeCompleted(out TerritoryCompactExpansionWorkResult result))
                {
                    Thread.Sleep(1);
                    continue;
                }

                Assert.That(result.IsSuccess, Is.True, result.FailureReason);
                Assert.That(result.SourceRevision, Is.EqualTo(store.Current.Revision));
                Assert.That(store.TryPublish(result.ApplySession, out TerritoryCompactCommitResult commit, out string reason), Is.True, reason);
                Assert.That(commit.Metrics.SourceWideBoundaryScans, Is.Zero);
                Assert.That(commit.Metrics.SourceWideFullChunkScans, Is.Zero);
                Assert.That(commit.Metrics.GlobalBoundaryRenumbers, Is.Zero);
                Assert.That(commit.Metrics.ExpandedFullChunkObjects, Is.Zero);
                Assert.That(commit.Metrics.UnchangedNodeCopies, Is.Zero);
                Assert.That(result.Metrics.WorkerThreadId, Is.Not.EqualTo(Environment.CurrentManagedThreadId));
                published++;
            }

            Assert.That(published, Is.EqualTo(queued.Count));
            Assert.That(worker.IsFaulted, Is.False);
            Assert.That(store.Current.Revision, Is.EqualTo(expected.Revision));
            Assert.That(store.Current.Boundary.Count, Is.EqualTo(expected.Boundary.Count));
            Assert.That(store.Current.Boundary.SignedTwiceArea, Is.EqualTo(expected.Boundary.SignedTwiceArea));
            Assert.That(store.Current.NextBoundaryIdentity, Is.EqualTo(expected.NextBoundaryIdentity));
            Assert.That(initial.Revision, Is.EqualTo(1));
        }

        [Test]
        public void InvalidGeometryFaultsTheWorkerWithoutPublishingOrAcceptingLaterWork()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            TerritoryCompactSnapshot initial = TerritoryCompactApplySessionTests.BuildCompactRectangle(
                0,
                0,
                size * 5,
                size * 5);
            List<TerritoryTrailFragment> invalid = BuildFragments(
                1,
                new FixedTerritoryPoint(size, size),
                new FixedTerritoryPoint(size * 2, size));
            Assert.That(TerritoryCompactExpansionWorkItem.TryTakeOwnership(
                1,
                initial.Revision,
                invalid,
                out TerritoryCompactExpansionWorkItem item,
                out string reason), Is.True, reason);

            using var worker = new TerritoryCompactExpansionWorker(initial);
            Assert.That(worker.TryEnqueue(item, out reason), Is.True, reason);
            TerritoryCompactExpansionWorkResult result = WaitForResult(worker);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureReason, Is.Not.Empty);
            Assert.That(worker.IsFaulted, Is.True);

            List<TerritoryTrailFragment> later = BuildFragments(
                2,
                new FixedTerritoryPoint(size * 4, size),
                new FixedTerritoryPoint(size * 7, size),
                new FixedTerritoryPoint(size * 4, size * 3));
            Assert.That(TerritoryCompactExpansionWorkItem.TryTakeOwnership(
                2,
                initial.Revision + 1,
                later,
                out TerritoryCompactExpansionWorkItem laterItem,
                out reason), Is.True, reason);
            Assert.That(worker.TryEnqueue(laterItem, out reason), Is.False);
            Assert.That(reason, Does.Contain("earlier failure"));
            Assert.That(initial.Revision, Is.EqualTo(1));
        }

        private static TerritoryCompactExpansionWorkResult WaitForResult(
            TerritoryCompactExpansionWorker worker)
        {
            var timeout = Stopwatch.StartNew();
            while (timeout.Elapsed < TimeSpan.FromSeconds(10))
            {
                if (worker.TryTakeCompleted(out TerritoryCompactExpansionWorkResult result))
                    return result;
                Thread.Sleep(1);
            }

            Assert.Fail("Timed out waiting for compact expansion worker completion.");
            return null;
        }

        private static TerritoryCompactSnapshot CompleteSynchronously(
            TerritoryCompactSnapshot source,
            TerritoryCompactExpansionWorkItem item)
        {
            Assert.That(TerritoryBoundaryLoopIndex.TryCreate(source, out TerritoryBoundaryLoopIndex index, out string reason), Is.True, reason);
            var expansion = new TerritoryChunkExpansionSession();
            Assert.That(expansion.TryBegin(index, item.SessionId, out reason), Is.True, reason);
            for (int i = 0; i < item.Fragments.Count; i++)
                Assert.That(expansion.TryAppendFragment(item.Fragments[i], out reason), Is.True, reason);
            Assert.That(expansion.TryComplete(item.SessionId, out TerritoryChunkExpansionPlan plan, out reason), Is.True, reason);

            var materializer = new TerritoryChunkExpansionMaterializationSession();
            Assert.That(materializer.TryBegin(source, index, plan, out reason), Is.True, reason);
            bool completed = false;
            while (!completed)
                Assert.That(materializer.TryStep(2048, out _, out completed, out reason), Is.True, reason);
            Assert.That(materializer.TryGetResult(out TerritoryChunkExpansionMaterialization materialization, out reason), Is.True, reason);

            var apply = new TerritoryCompactApplySession();
            Assert.That(apply.TryBegin(source, materialization, out reason), Is.True, reason);
            completed = false;
            while (!completed)
                Assert.That(apply.TryStep(2048, out _, out completed, out reason), Is.True, reason);
            Assert.That(apply.TryGetResult(out TerritoryCompactSnapshot candidate, out _, out reason), Is.True, reason);
            return candidate;
        }

        private static List<TerritoryTrailFragment> BuildFragments(
            ulong sessionId,
            params FixedTerritoryPoint[] points)
        {
            var trail = new TerritoryTrailSession();
            Assert.That(trail.TryBegin(new TerritoryTrailSample(sessionId, 0, 0, points[0]), out string reason), Is.True, reason);
            for (int i = 1; i < points.Length; i++)
            {
                Assert.That(trail.TryAppendSample(
                    new TerritoryTrailSample(sessionId, (uint)i, i, points[i]),
                    out reason), Is.True, reason);
            }
            Assert.That(trail.TryCommit(sessionId, out reason), Is.True, reason);
            return new List<TerritoryTrailFragment>(trail.Fragments);
        }

        private static TerritoryCompactBoundarySegment FindOutermostUpwardEdge(
            TerritoryCompactSnapshot snapshot)
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
    }
}
