using System.Linq;
using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryCompactApplySessionTests
    {
        [Test]
        public void OneUnitAndLargeBudgetsPublishTheSameExactPersistentCandidate()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            TerritoryCompactSnapshot source = BuildCompactRectangle(0, 0, size * 6, size * 8);
            TerritoryChunkExpansionMaterialization materialization = Materialize(
                source,
                10,
                new FixedTerritoryPoint(size * 5, size * 2 + 300),
                new FixedTerritoryPoint(size * 10, size * 2 + 300),
                new FixedTerritoryPoint(size * 10, size * 5 + 300),
                new FixedTerritoryPoint(size * 5, size * 5 + 300));

            CompleteApply(source, materialization, 1, out TerritoryCompactSnapshot one, out TerritoryCompactCommitResult oneResult);
            CompleteApply(source, materialization, 10000, out TerritoryCompactSnapshot many, out TerritoryCompactCommitResult manyResult);

            Assert.That(many.Revision, Is.EqualTo(one.Revision));
            Assert.That(many.Boundary.SignedTwiceArea, Is.EqualTo(one.Boundary.SignedTwiceArea));
            Assert.That(many.Boundary.Count, Is.EqualTo(one.Boundary.Count));
            Assert.That(many.NextBoundaryIdentity, Is.EqualTo(one.NextBoundaryIdentity));
            Assert.That(manyResult.Metrics.TotalWorkUnits, Is.EqualTo(oneResult.Metrics.TotalWorkUnits));
            Assert.That(oneResult.Metrics.SourceWideBoundaryScans, Is.Zero);
            Assert.That(oneResult.Metrics.SourceWideFullChunkScans, Is.Zero);
            Assert.That(oneResult.Metrics.GlobalBoundaryRenumbers, Is.Zero);
            Assert.That(oneResult.Metrics.ExpandedFullChunkObjects, Is.Zero);
            Assert.That(oneResult.Metrics.UnchangedNodeCopies, Is.Zero);
            Assert.That(source.Revision, Is.EqualTo(1));
            Assert.That(source.Boundary.AbsoluteTwiceArea + materialization.AddedAbsoluteTwiceArea,
                Is.EqualTo(one.Boundary.AbsoluteTwiceArea));
            Assert.That(one.FullRowRootIdentity, Is.Not.SameAs(source.FullRowRootIdentity));
        }

        [Test]
        public void AbortAndStaleSourceNeverPublishPartialState()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            TerritoryCompactSnapshot source = BuildCompactRectangle(0, 0, size * 5, size * 5);
            TerritoryChunkExpansionMaterialization materialization = Materialize(
                source,
                20,
                new FixedTerritoryPoint(size * 4, size),
                new FixedTerritoryPoint(size * 7, size),
                new FixedTerritoryPoint(size * 7, size * 3),
                new FixedTerritoryPoint(size * 4, size * 3));
            var session = new TerritoryCompactApplySession();

            Assert.That(session.TryBegin(source, materialization, out string reason), Is.True, reason);
            Assert.That(session.TryStep(2, out int used, out bool completed, out reason), Is.True, reason);
            Assert.That(used, Is.EqualTo(2));
            Assert.That(completed, Is.False);
            Assert.That(session.TryGetResult(out _, out _, out _), Is.False);
            Assert.That(session.TryAbort(out reason), Is.True, reason);
            Assert.That(session.TryGetResult(out _, out _, out _), Is.False);
            Assert.That(source.Revision, Is.EqualTo(1));

            TerritoryCompactSnapshot stale = BuildCompactRectangle(0, 0, size * 5, size * 5, 2);
            var staleSession = new TerritoryCompactApplySession();
            Assert.That(staleSession.TryBegin(stale, materialization, out reason), Is.False);
            Assert.That(reason, Does.Contain("revision"));
        }

        internal static TerritoryCompactSnapshot BuildCompactRectangle(
            int minimumX,
            int minimumY,
            int maximumX,
            int maximumY,
            ulong revision = 1)
        {
            var builder = new TerritoryChunkStateBuilder();
            FixedTerritoryPoint[] polygon =
            {
                new(minimumX, minimumY),
                new(maximumX, minimumY),
                new(maximumX, maximumY),
                new(minimumX, maximumY)
            };
            Assert.That(builder.TryBuild(polygon, revision, out TerritoryChunkSnapshot source, out string reason), Is.True, reason);
            var compactBuilder = new TerritoryCompactSnapshotBuilder();
            Assert.That(compactBuilder.TryBuild(source, out TerritoryCompactSnapshot compact, out reason), Is.True, reason);
            return compact;
        }

        internal static TerritoryChunkExpansionMaterialization Materialize(
            TerritoryCompactSnapshot source,
            ulong sessionId,
            params FixedTerritoryPoint[] samples)
        {
            Assert.That(TerritoryBoundaryLoopIndex.TryCreate(source, out TerritoryBoundaryLoopIndex index, out string reason), Is.True, reason);
            TerritoryChunkExpansionPlan plan = CompletePlan(index, sessionId, samples);
            Assert.That(plan.BoundaryForwardFromEntryToExit, Is.True);
            var session = new TerritoryChunkExpansionMaterializationSession();
            Assert.That(session.TryBegin(source, index, plan, out reason), Is.True, reason);
            bool completed = false;
            int guard = 0;
            while (!completed && guard++ < 1000000)
                Assert.That(session.TryStep(127, out _, out completed, out reason), Is.True, reason);
            Assert.That(completed, Is.True);
            Assert.That(session.TryGetResult(out TerritoryChunkExpansionMaterialization result, out reason), Is.True, reason);
            Assert.That(result.BoundarySplice, Is.Not.Null);
            Assert.That(result.BoundarySplice.RemovedIds, Is.Not.Empty);
            return result;
        }

        internal static TerritoryChunkExpansionPlan CompletePlan(
            TerritoryBoundaryLoopIndex index,
            ulong sessionId,
            params FixedTerritoryPoint[] samples)
        {
            var trail = new TerritoryTrailSession();
            Assert.That(trail.TryBegin(new TerritoryTrailSample(sessionId, 0, 0, samples[0]), out string reason), Is.True, reason);
            for (int i = 1; i < samples.Length; i++)
                Assert.That(trail.TryAppendSample(new TerritoryTrailSample(sessionId, (uint)i, i, samples[i]), out reason), Is.True, reason);
            Assert.That(trail.TryCommit(sessionId, out reason), Is.True, reason);

            var expansion = new TerritoryChunkExpansionSession();
            Assert.That(expansion.TryBegin(index, sessionId, out reason), Is.True, reason);
            foreach (TerritoryTrailFragment fragment in trail.Fragments)
                Assert.That(expansion.TryAppendFragment(fragment, out reason), Is.True, reason);
            Assert.That(expansion.TryComplete(sessionId, out TerritoryChunkExpansionPlan plan, out reason), Is.True, reason);
            Assert.That(plan.ExitBoundaryId.IsValid, Is.True);
            Assert.That(plan.EntryBoundaryId.IsValid, Is.True);
            return plan;
        }

        internal static void CompleteApply(
            TerritoryCompactSnapshot source,
            TerritoryChunkExpansionMaterialization materialization,
            int budget,
            out TerritoryCompactSnapshot candidate,
            out TerritoryCompactCommitResult result)
        {
            var session = new TerritoryCompactApplySession();
            Assert.That(session.TryBegin(source, materialization, out string reason), Is.True, reason);
            bool completed = false;
            int guard = 0;
            while (!completed && guard++ < 1000000)
            {
                Assert.That(session.TryStep(budget, out int used, out completed, out reason), Is.True, reason);
                Assert.That(used, Is.InRange(1, budget));
                Assert.That(session.LastStepWorkUnits, Is.EqualTo(used));
                if (!completed)
                    Assert.That(session.TryGetResult(out _, out _, out _), Is.False);
            }
            Assert.That(completed, Is.True);
            Assert.That(session.TryGetResult(out candidate, out result, out reason), Is.True, reason);
        }
    }
}
