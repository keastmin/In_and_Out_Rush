using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryChunkExpansionMaterializationSessionTests
    {
        private readonly TerritoryChunkStateBuilder _builder = new();

        [Test]
        public void ExactAddedRegionKeepsTrailAndCompressesInteriorByRow()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            BuildRectangle(0, 0, size * 4, size * 8, out TerritoryChunkSnapshot snapshot, out TerritoryBoundaryLoopIndex index);
            TerritoryChunkExpansionPlan plan = CompletePlan(
                index,
                10,
                new FixedTerritoryPoint(size * 3, size * 2 + 300),
                new FixedTerritoryPoint(size * 8, size * 2 + 300),
                new FixedTerritoryPoint(size * 8, size * 5 + 300),
                new FixedTerritoryPoint(size * 3, size * 5 + 300));

            TerritoryChunkExpansionMaterialization result = Materialize(snapshot, index, plan, 3);

            Assert.That(result.SourceRevision, Is.EqualTo(snapshot.Revision));
            Assert.That(result.SessionId, Is.EqualTo(plan.SessionId));
            Assert.That(result.AddedAbsoluteTwiceArea,
                Is.EqualTo(plan.ResultAbsoluteTwiceArea - index.AbsoluteTwiceArea));
            Assert.That(result.FullRuns.Count, Is.GreaterThan(0));
            Assert.That(result.Metrics.FullChunkCount, Is.GreaterThan(result.FullRuns.Count));
            Assert.That(result.FullRuns, Is.Ordered.By("Y").Then.By("MinimumX"));
            Assert.That(result.BoundaryEdits.Sum(edit => edit.AddedTrailSegments.Count), Is.GreaterThan(3));
            Assert.That(result.BoundaryEdits.Sum(edit => edit.RemovedSourceSequences.Count), Is.GreaterThan(0));
            Assert.That(result.BoundaryEdits.Sum(edit => edit.RemovedSourceIds.Count), Is.GreaterThan(0));
            AssertExactTrailEndpoints(result, plan.ExitPoint, plan.EntryPoint);
            AssertZeroSourceWideWork(result.Metrics);
        }

        [Test]
        public void OneUnitAndLargeBudgetsProduceIdenticalImmutableResultAndMetrics()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            BuildRectangle(-size * 4, -size * 4, size * 2, size * 5, out TerritoryChunkSnapshot snapshot, out TerritoryBoundaryLoopIndex index);
            TerritoryChunkExpansionPlan plan = CompletePlan(
                index,
                20,
                new FixedTerritoryPoint(size, -size * 2),
                new FixedTerritoryPoint(size * 4, -size),
                new FixedTerritoryPoint(size * 4, size * 3),
                new FixedTerritoryPoint(size, size * 4));

            TerritoryChunkExpansionMaterialization one = Materialize(snapshot, index, plan, 1);
            TerritoryChunkExpansionMaterialization many = Materialize(snapshot, index, plan, 10000);

            Assert.That(many.Metrics, Is.EqualTo(one.Metrics));
            Assert.That(many.AddedAbsoluteTwiceArea, Is.EqualTo(one.AddedAbsoluteTwiceArea));
            AssertMaterializationsEqual(one, many);
        }

        [Test]
        public void ContactsOnOneSourceSequenceMaterializeOnlyTheReplacedSubArc()
        {
            BuildRectangle(0, 0, 1000, 1000, out TerritoryChunkSnapshot snapshot, out TerritoryBoundaryLoopIndex index);
            TerritoryChunkExpansionPlan plan = CompletePlan(
                index,
                25,
                new FixedTerritoryPoint(500, 200),
                new FixedTerritoryPoint(1500, 200),
                new FixedTerritoryPoint(1500, 800),
                new FixedTerritoryPoint(500, 800));

            TerritoryChunkExpansionMaterialization result = Materialize(snapshot, index, plan, 1);

            Assert.That(plan.ExitBoundarySequence, Is.EqualTo(plan.EntryBoundarySequence));
            Assert.That(result.FullRuns, Is.Empty);
            Assert.That(result.BoundaryEdits.Sum(edit => edit.RemovedSourceSequences.Count), Is.EqualTo(1));
            AssertExactTrailEndpoints(result, plan.ExitPoint, plan.EntryPoint);
            AssertZeroSourceWideWork(result.Metrics);
        }

        [Test]
        public void ClockwiseSourceUsesTheOppositeArcDirectionWithoutChangingTrailShape()
        {
            FixedTerritoryPoint[] clockwise =
            {
                new(0, 0),
                new(0, 1000),
                new(1000, 1000),
                new(1000, 0)
            };
            Assert.That(_builder.TryBuild(clockwise, 1, out TerritoryChunkSnapshot snapshot, out string reason), Is.True, reason);
            Assert.That(TerritoryBoundaryLoopIndex.TryCreate(snapshot, out TerritoryBoundaryLoopIndex index, out reason), Is.True, reason);
            TerritoryChunkExpansionPlan plan = CompletePlan(
                index,
                27,
                new FixedTerritoryPoint(500, 200),
                new FixedTerritoryPoint(1500, 200),
                new FixedTerritoryPoint(1500, 800),
                new FixedTerritoryPoint(500, 800));

            TerritoryChunkExpansionMaterialization result = Materialize(snapshot, index, plan, 2);

            Assert.That(plan.BoundaryForwardFromEntryToExit, Is.False);
            AssertExactTrailEndpoints(result, plan.ExitPoint, plan.EntryPoint);
            Assert.That(result.BoundaryEdits.Sum(edit => edit.RemovedSourceSequences.Count), Is.EqualTo(1));
            AssertZeroSourceWideWork(result.Metrics);
        }

        [Test]
        public void ResultIsHiddenUntilCompletionAndAbortBurnsCandidateWithoutChangingSource()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            BuildRectangle(0, 0, size * 4, size * 4, out TerritoryChunkSnapshot snapshot, out TerritoryBoundaryLoopIndex index);
            TerritoryChunkExpansionPlan plan = CompletePlan(
                index,
                30,
                new FixedTerritoryPoint(size * 3, size),
                new FixedTerritoryPoint(size * 6, size),
                new FixedTerritoryPoint(size * 6, size * 3),
                new FixedTerritoryPoint(size * 3, size * 3));
            int sourceCount = snapshot.Chunks.Count;

            var session = new TerritoryChunkExpansionMaterializationSession();
            Assert.That(session.TryBegin(snapshot, index, plan, out string beginReason), Is.True, beginReason);
            Assert.That(session.TryStep(2, out int used, out bool completed, out string stepReason), Is.True, stepReason);
            Assert.That(used, Is.EqualTo(2));
            Assert.That(completed, Is.False);
            Assert.That(session.TryGetResult(out TerritoryChunkExpansionMaterialization pending, out _), Is.False);
            Assert.That(pending, Is.Null);
            Assert.That(session.TryAbort(plan.SessionId, out string abortReason), Is.True, abortReason);
            Assert.That(session.Status, Is.EqualTo(TerritoryTrailSessionStatus.Aborted));
            Assert.That(session.TryGetResult(out TerritoryChunkExpansionMaterialization aborted, out _), Is.False);
            Assert.That(aborted, Is.Null);
            Assert.That(snapshot.Chunks.Count, Is.EqualTo(sourceCount));
            Assert.That(snapshot.Revision, Is.EqualTo(1));
        }

        [Test]
        public void RevisionMismatchIsRejectedBeforeAnyCandidateStarts()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            BuildRectangle(0, 0, size * 4, size * 4, out TerritoryChunkSnapshot snapshot, out TerritoryBoundaryLoopIndex index);
            TerritoryChunkExpansionPlan plan = CompletePlan(
                index,
                40,
                new FixedTerritoryPoint(size * 3, size),
                new FixedTerritoryPoint(size * 5, size),
                new FixedTerritoryPoint(size * 5, size * 3),
                new FixedTerritoryPoint(size * 3, size * 3));
            var staleSnapshot = new TerritoryChunkSnapshot(2, snapshot.Chunks);
            var session = new TerritoryChunkExpansionMaterializationSession();

            Assert.That(session.TryBegin(staleSnapshot, index, plan, out string reason), Is.False);
            Assert.That(reason, Does.Contain("revisions must match"));
            Assert.That(session.Status, Is.EqualTo(TerritoryTrailSessionStatus.Inactive));
        }

        [Test]
        public void CompletedResultRemainsImmutableWhenSessionIsReused()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            BuildRectangle(0, 0, size * 4, size * 5, out TerritoryChunkSnapshot snapshot, out TerritoryBoundaryLoopIndex index);
            TerritoryChunkExpansionPlan firstPlan = CompletePlan(
                index,
                45,
                new FixedTerritoryPoint(size * 3, size),
                new FixedTerritoryPoint(size * 7, size),
                new FixedTerritoryPoint(size * 7, size * 3),
                new FixedTerritoryPoint(size * 3, size * 3));
            TerritoryChunkExpansionPlan secondPlan = CompletePlan(
                index,
                46,
                new FixedTerritoryPoint(size, size * 4),
                new FixedTerritoryPoint(size, size * 7),
                new FixedTerritoryPoint(size * 3, size * 7),
                new FixedTerritoryPoint(size * 3, size * 4));
            var session = new TerritoryChunkExpansionMaterializationSession();

            TerritoryChunkExpansionMaterialization first = CompleteExistingSession(session, snapshot, index, firstPlan, 5);
            TerritoryChunkFillRun[] originalRuns = first.FullRuns.ToArray();
            TerritoryChunkBoundaryEdit[] originalEdits = first.BoundaryEdits.ToArray();
            CompleteExistingSession(session, snapshot, index, secondPlan, 5);

            CollectionAssert.AreEqual(originalRuns, first.FullRuns);
            CollectionAssert.AreEqual(originalEdits, first.BoundaryEdits);
        }

        [Test]
        public void ThousandWorldUnitStressUsesRowRunsAndNeverScansOrRenumbersSource()
        {
            int world = 1000 * FixedTerritoryPoint.UnitsPerWorldUnit;
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            BuildRectangle(0, 0, world, world, out TerritoryChunkSnapshot snapshot, out TerritoryBoundaryLoopIndex index);
            TerritoryChunkExpansionPlan plan = CompletePlan(
                index,
                50,
                new FixedTerritoryPoint(world - size, size * 8),
                new FixedTerritoryPoint(world + size * 4, size * 8),
                new FixedTerritoryPoint(world + size * 4, world - size * 8),
                new FixedTerritoryPoint(world - size, world - size * 8));

            TerritoryChunkExpansionMaterialization result = Materialize(snapshot, index, plan, 37);

            Assert.That(index.SegmentCount, Is.GreaterThan(400));
            Assert.That(result.Metrics.FullChunkCount, Is.GreaterThan(result.Metrics.FullRunCount * 2L));
            Assert.That(result.Metrics.FullRunCount, Is.LessThanOrEqualTo(125));
            Assert.That(result.Metrics.ReplacedBoundarySegmentsRead, Is.LessThan(index.SegmentCount));
            AssertZeroSourceWideWork(result.Metrics);
        }

        private void BuildRectangle(
            int minimumX,
            int minimumY,
            int maximumX,
            int maximumY,
            out TerritoryChunkSnapshot snapshot,
            out TerritoryBoundaryLoopIndex index)
        {
            FixedTerritoryPoint[] polygon =
            {
                new(minimumX, minimumY),
                new(maximumX, minimumY),
                new(maximumX, maximumY),
                new(minimumX, maximumY)
            };
            Assert.That(_builder.TryBuild(polygon, 1, out snapshot, out string buildReason), Is.True, buildReason);
            Assert.That(TerritoryBoundaryLoopIndex.TryCreate(snapshot, out index, out string indexReason), Is.True, indexReason);
        }

        private static TerritoryChunkExpansionPlan CompletePlan(
            TerritoryBoundaryLoopIndex index,
            ulong sessionId,
            params FixedTerritoryPoint[] samples)
        {
            var trail = new TerritoryTrailSession();
            Assert.That(trail.TryBegin(new TerritoryTrailSample(sessionId, 0, 0, samples[0]), out string reason), Is.True, reason);
            for (int i = 1; i < samples.Length; i++)
            {
                Assert.That(trail.TryAppendSample(
                    new TerritoryTrailSample(sessionId, (uint)i, i, samples[i]), out reason), Is.True, reason);
            }
            Assert.That(trail.TryCommit(sessionId, out reason), Is.True, reason);

            var expansion = new TerritoryChunkExpansionSession();
            Assert.That(expansion.TryBegin(index, sessionId, out reason), Is.True, reason);
            foreach (TerritoryTrailFragment fragment in trail.Fragments)
                Assert.That(expansion.TryAppendFragment(fragment, out reason), Is.True, reason);
            Assert.That(expansion.TryComplete(sessionId, out TerritoryChunkExpansionPlan plan, out reason), Is.True, reason);
            return plan;
        }

        private static TerritoryChunkExpansionMaterialization Materialize(
            TerritoryChunkSnapshot snapshot,
            TerritoryBoundaryLoopIndex index,
            TerritoryChunkExpansionPlan plan,
            int budget)
        {
            var session = new TerritoryChunkExpansionMaterializationSession();
            return CompleteExistingSession(session, snapshot, index, plan, budget);
        }

        private static TerritoryChunkExpansionMaterialization CompleteExistingSession(
            TerritoryChunkExpansionMaterializationSession session,
            TerritoryChunkSnapshot snapshot,
            TerritoryBoundaryLoopIndex index,
            TerritoryChunkExpansionPlan plan,
            int budget)
        {
            Assert.That(session.TryBegin(snapshot, index, plan, out string reason), Is.True, reason);
            bool completed = false;
            int calls = 0;
            while (!completed && calls++ < 1000000)
            {
                Assert.That(session.TryStep(budget, out int used, out completed, out reason), Is.True, reason);
                Assert.That(used, Is.InRange(1, budget));
                Assert.That(session.LastStepWorkUnits, Is.EqualTo(used));
                if (!completed)
                    Assert.That(session.TryGetResult(out _, out _), Is.False);
            }

            Assert.That(completed, Is.True, "Materialization exceeded its deterministic test work bound.");
            Assert.That(session.TryGetResult(out TerritoryChunkExpansionMaterialization result, out reason), Is.True, reason);
            return result;
        }

        private static void AssertExactTrailEndpoints(
            TerritoryChunkExpansionMaterialization result,
            FixedTerritoryPoint exit,
            FixedTerritoryPoint entry)
        {
            TerritoryChunkBoundarySegment[] ordered = result.BoundaryEdits
                .SelectMany(edit => edit.AddedTrailSegments.Select(segment => (edit.Chunk, segment)))
                .OrderBy(pair => pair.segment.Sequence)
                .Select(pair => new TerritoryChunkBoundarySegment(
                    pair.segment.Sequence,
                    pair.segment.Start,
                    pair.segment.End))
                .ToArray();
            var global = result.BoundaryEdits
                .SelectMany(edit => edit.AddedTrailSegments.Select(segment => new
                {
                    segment.Sequence,
                    Start = segment.Start.ToGlobal(edit.Chunk),
                    End = segment.End.ToGlobal(edit.Chunk)
                }))
                .OrderBy(segment => segment.Sequence)
                .ToArray();

            Assert.That(ordered.Length, Is.GreaterThan(0));
            Assert.That(global[0].Start, Is.EqualTo(exit));
            Assert.That(global[global.Length - 1].End, Is.EqualTo(entry));
            for (int i = 1; i < global.Length; i++)
                Assert.That(global[i].Start, Is.EqualTo(global[i - 1].End));
        }

        private static void AssertMaterializationsEqual(
            TerritoryChunkExpansionMaterialization expected,
            TerritoryChunkExpansionMaterialization actual)
        {
            CollectionAssert.AreEqual(expected.FullRuns, actual.FullRuns);
            Assert.That(actual.BoundaryEdits.Count, Is.EqualTo(expected.BoundaryEdits.Count));
            for (int i = 0; i < expected.BoundaryEdits.Count; i++)
            {
                TerritoryChunkBoundaryEdit left = expected.BoundaryEdits[i];
                TerritoryChunkBoundaryEdit right = actual.BoundaryEdits[i];
                Assert.That(right.Chunk, Is.EqualTo(left.Chunk));
                Assert.That(right.CenterInsideAddedRegion, Is.EqualTo(left.CenterInsideAddedRegion));
                CollectionAssert.AreEqual(left.AddedTrailSegments, right.AddedTrailSegments);
                CollectionAssert.AreEqual(left.ReplacedBoundarySegments, right.ReplacedBoundarySegments);
                CollectionAssert.AreEqual(left.RemovedSourceSequences, right.RemovedSourceSequences);
                CollectionAssert.AreEqual(left.RemovedSourceIds, right.RemovedSourceIds);
            }
        }

        private static void AssertZeroSourceWideWork(TerritoryChunkExpansionMaterializationMetrics metrics)
        {
            Assert.That(metrics.SourceBoundaryFullScans, Is.Zero);
            Assert.That(metrics.SourceFullChunkScans, Is.Zero);
            Assert.That(metrics.SourceBoundaryRenumberings, Is.Zero);
        }
    }
}
