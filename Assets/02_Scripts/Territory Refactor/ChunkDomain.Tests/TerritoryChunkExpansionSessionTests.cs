using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryChunkExpansionSessionTests
    {
        private readonly TerritoryChunkStateBuilder _builder = new();

        [Test]
        public void ExactExternalTrailProducesLargerExpansionWithoutMovingPoints()
        {
            TerritoryBoundaryLoopIndex index = BuildRectangleIndex(0, 0, 1000, 1000);
            FixedTerritoryPoint[] path =
            {
                new(500, 500),
                new(1500, 500),
                new(1500, 800),
                new(500, 800)
            };

            TerritoryChunkExpansionPlan plan = Complete(index, 10, path);

            CollectionAssert.AreEqual(
                new[]
                {
                    new FixedTerritoryPoint(1000, 500),
                    new FixedTerritoryPoint(1500, 500),
                    new FixedTerritoryPoint(1500, 800),
                    new FixedTerritoryPoint(1000, 800)
                },
                plan.TrailPoints);
            Assert.That(plan.SourceRevision, Is.EqualTo(1));
            Assert.That(plan.SessionId, Is.EqualTo(10));
            Assert.That(plan.ResultAbsoluteTwiceArea, Is.EqualTo(2300000m));
            Assert.That(plan.Metrics.TerminalBoundarySegmentScans, Is.Zero);
            Assert.That(plan.Metrics.TerminalTrailPointScans, Is.Zero);
        }

        [Test]
        public void ExactCollinearMiddlePointsAreTheOnlyRemovedTrailShapeData()
        {
            TerritoryBoundaryLoopIndex index = BuildRectangleIndex(0, 0, 1000, 1000);
            FixedTerritoryPoint[] path =
            {
                new(500, 400),
                new(1100, 400),
                new(1300, 400),
                new(1500, 400),
                new(1500, 600),
                new(1400, 700),
                new(500, 700)
            };

            TerritoryChunkExpansionPlan plan = Complete(index, 20, path);

            CollectionAssert.AreEqual(
                new[]
                {
                    new FixedTerritoryPoint(1000, 400),
                    new FixedTerritoryPoint(1500, 400),
                    new FixedTerritoryPoint(1500, 600),
                    new FixedTerritoryPoint(1400, 700),
                    new FixedTerritoryPoint(1000, 700)
                },
                plan.TrailPoints);
        }

        [Test]
        public void SharedChunkEdgeContactsAreProcessedOnceAcrossAdjacentFragments()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            TerritoryBoundaryLoopIndex index = BuildRectangleIndex(-size, -size, size, size);
            TerritoryChunkExpansionPlan plan = CompleteFromSamples(
                index,
                25,
                new FixedTerritoryPoint(0, 0),
                new FixedTerritoryPoint(size + 500, 0),
                new FixedTerritoryPoint(size + 500, 800),
                new FixedTerritoryPoint(0, 800));

            CollectionAssert.AreEqual(
                new[]
                {
                    new FixedTerritoryPoint(size, 0),
                    new FixedTerritoryPoint(size + 500, 0),
                    new FixedTerritoryPoint(size + 500, 800),
                    new FixedTerritoryPoint(size, 800)
                },
                plan.TrailPoints);
        }

        [Test]
        public void ConvexBoundaryCornerCrossingAndInsideTailRemainValid()
        {
            TerritoryBoundaryLoopIndex index = BuildRectangleIndex(0, 0, 1000, 1000);
            TerritoryChunkExpansionPlan plan = Complete(
                index,
                26,
                new FixedTerritoryPoint(500, 500),
                new FixedTerritoryPoint(1500, 1500),
                new FixedTerritoryPoint(1500, 700),
                new FixedTerritoryPoint(500, 700));

            Assert.That(plan.ExitPoint, Is.EqualTo(new FixedTerritoryPoint(1000, 1000)));
            Assert.That(plan.EntryPoint, Is.EqualTo(new FixedTerritoryPoint(1000, 700)));
            CollectionAssert.AreEqual(
                new[]
                {
                    new FixedTerritoryPoint(1000, 1000),
                    new FixedTerritoryPoint(1500, 1500),
                    new FixedTerritoryPoint(1500, 700),
                    new FixedTerritoryPoint(1000, 700)
                },
                plan.TrailPoints);
        }

        [Test]
        public void ConcaveBoundaryUsesExactTrailAndStillSelectsTheGrowingCandidate()
        {
            FixedTerritoryPoint[] polygon =
            {
                new(0, 0),
                new(1500, 0),
                new(1500, 500),
                new(800, 500),
                new(800, 1000),
                new(1500, 1000),
                new(1500, 1500),
                new(0, 1500)
            };
            Assert.That(_builder.TryBuild(polygon, 1, out TerritoryChunkSnapshot snapshot, out string buildReason), Is.True, buildReason);
            Assert.That(TerritoryBoundaryLoopIndex.TryCreate(snapshot, out TerritoryBoundaryLoopIndex index, out string indexReason), Is.True, indexReason);

            TerritoryChunkExpansionPlan plan = Complete(
                index,
                27,
                new FixedTerritoryPoint(500, 750),
                new FixedTerritoryPoint(1200, 750),
                new FixedTerritoryPoint(1800, 750),
                new FixedTerritoryPoint(1800, 1200),
                new FixedTerritoryPoint(1200, 1200));

            Assert.That(plan.ResultAbsoluteTwiceArea, Is.GreaterThan(index.AbsoluteTwiceArea));
            CollectionAssert.AreEqual(
                new[]
                {
                    new FixedTerritoryPoint(800, 750),
                    new FixedTerritoryPoint(1800, 750),
                    new FixedTerritoryPoint(1800, 1200),
                    new FixedTerritoryPoint(1500, 1200)
                },
                plan.TrailPoints);
        }

        [Test]
        public void SelfIntersectionBurnsPendingPlanAndCannotComplete()
        {
            TerritoryBoundaryLoopIndex index = BuildRectangleIndex(0, 0, 1000, 1000);
            var session = new TerritoryChunkExpansionSession();
            Assert.That(session.TryBegin(index, 30, out _), Is.True);

            TerritoryTrailFragment fragment = Fragment(
                30,
                0,
                new FixedTerritoryPoint(500, 500),
                new FixedTerritoryPoint(1500, 500),
                new FixedTerritoryPoint(1200, 800),
                new FixedTerritoryPoint(1500, 800),
                new FixedTerritoryPoint(1200, 500));

            Assert.That(session.TryAppendFragment(fragment, out string reason), Is.False);
            Assert.That(reason, Does.Contain("previous path"));
            Assert.That(session.Status, Is.EqualTo(TerritoryTrailSessionStatus.Aborted));
            Assert.That(session.TryComplete(30, out TerritoryChunkExpansionPlan plan, out _), Is.False);
            Assert.That(plan, Is.Null);
        }

        [Test]
        public void BoundaryOverlapAndThirdCrossingNeverPublishAPlan()
        {
            TerritoryBoundaryLoopIndex index = BuildRectangleIndex(0, 0, 1000, 1000);
            var overlap = new TerritoryChunkExpansionSession();
            Assert.That(overlap.TryBegin(index, 40, out _), Is.True);
            Assert.That(overlap.TryAppendFragment(
                Fragment(40, 0, new FixedTerritoryPoint(500, 500), new FixedTerritoryPoint(1000, 500), new FixedTerritoryPoint(1000, 800)),
                out string overlapReason), Is.False);
            Assert.That(overlapReason, Does.Contain("overlaps"));

            var crossing = new TerritoryChunkExpansionSession();
            Assert.That(crossing.TryBegin(index, 41, out _), Is.True);
            Assert.That(crossing.TryAppendFragment(
                Fragment(
                    41,
                    0,
                    new FixedTerritoryPoint(500, 300),
                    new FixedTerritoryPoint(1500, 300),
                    new FixedTerritoryPoint(500, 500),
                    new FixedTerritoryPoint(1500, 700)),
                out string crossingReason), Is.False);
            Assert.That(crossingReason, Does.Contain("more than twice"));
            Assert.That(crossing.TryComplete(41, out TerritoryChunkExpansionPlan plan, out _), Is.False);
            Assert.That(plan, Is.Null);
        }

        [Test]
        public void SequenceGapIsRejectedWithoutConsumingExpectedFragment()
        {
            TerritoryBoundaryLoopIndex index = BuildRectangleIndex(0, 0, 1000, 1000);
            var session = new TerritoryChunkExpansionSession();
            Assert.That(session.TryBegin(index, 50, out _), Is.True);

            TerritoryTrailFragment valid = Fragment(50, 0, new FixedTerritoryPoint(500, 500), new FixedTerritoryPoint(1500, 500));
            TerritoryTrailFragment gap = Fragment(50, 1, new FixedTerritoryPoint(500, 500), new FixedTerritoryPoint(1500, 500));
            Assert.That(session.TryAppendFragment(gap, out string reason), Is.False);
            Assert.That(reason, Does.Contain("Expected fragment sequence 0"));
            Assert.That(session.TryAppendFragment(valid, out reason), Is.True, reason);
            Assert.That(session.Metrics.FragmentCount, Is.EqualTo(1));
        }

        [Test]
        public void AbortDiscardsPendingDataAndNewerSessionCanStartCleanly()
        {
            TerritoryBoundaryLoopIndex index = BuildRectangleIndex(0, 0, 1000, 1000);
            var session = new TerritoryChunkExpansionSession();
            Assert.That(session.TryBegin(index, 60, out _), Is.True);
            Assert.That(session.TryAppendFragment(
                Fragment(60, 0, new FixedTerritoryPoint(500, 500), new FixedTerritoryPoint(1500, 500)), out _), Is.True);
            Assert.That(session.TryAbort(60, out _), Is.True);
            Assert.That(session.Metrics.FragmentCount, Is.Zero);

            Assert.That(session.TryBegin(index, 61, out string reason), Is.True, reason);
            TerritoryChunkExpansionPlan plan = AppendAndComplete(
                session,
                61,
                new FixedTerritoryPoint(500, 500),
                new FixedTerritoryPoint(1500, 500),
                new FixedTerritoryPoint(1500, 800),
                new FixedTerritoryPoint(500, 800));
            Assert.That(plan.SessionId, Is.EqualTo(61));
        }

        [Test]
        public void IdenticalPeerRoleIndependentSessionsProduceIdenticalPlansAndMetrics()
        {
            TerritoryBoundaryLoopIndex index = BuildRectangleIndex(-500, -500, 1000, 1000);
            FixedTerritoryPoint[] path =
            {
                new(300, 300),
                new(1500, 300),
                new(1600, 700),
                new(300, 700)
            };

            TerritoryChunkExpansionPlan hostRole = Complete(index, 70, path);
            TerritoryChunkExpansionPlan clientRole = Complete(index, 70, path);

            Assert.That(clientRole.ExitPoint, Is.EqualTo(hostRole.ExitPoint));
            Assert.That(clientRole.EntryPoint, Is.EqualTo(hostRole.EntryPoint));
            Assert.That(clientRole.ExitBoundarySequence, Is.EqualTo(hostRole.ExitBoundarySequence));
            Assert.That(clientRole.EntryBoundarySequence, Is.EqualTo(hostRole.EntryBoundarySequence));
            Assert.That(clientRole.BoundaryForwardFromEntryToExit, Is.EqualTo(hostRole.BoundaryForwardFromEntryToExit));
            Assert.That(clientRole.ResultTwiceArea, Is.EqualTo(hostRole.ResultTwiceArea));
            Assert.That(clientRole.Metrics, Is.EqualTo(hostRole.Metrics));
            CollectionAssert.AreEqual(hostRole.TrailPoints, clientRole.TrailPoints);
        }

        [Test]
        public void ThousandWorldUnitStressKeepsTerminalScansAtZeroAndBoundaryChecksLocal()
        {
            int worldSize = 1000 * FixedTerritoryPoint.UnitsPerWorldUnit;
            TerritoryBoundaryLoopIndex index = BuildRectangleIndex(0, 0, worldSize, worldSize);
            var trail = new TerritoryTrailSession();
            const ulong sessionId = 80;

            Assert.That(trail.TryBegin(new TerritoryTrailSample(sessionId, 0, 0, new FixedTerritoryPoint(worldSize / 2, worldSize / 2)), out _), Is.True);
            Assert.That(trail.TryAppendSample(new TerritoryTrailSample(sessionId, 1, 1, new FixedTerritoryPoint(worldSize + 4096, worldSize / 2)), out _), Is.True);
            Assert.That(trail.TryAppendSample(new TerritoryTrailSample(sessionId, 2, 2, new FixedTerritoryPoint(worldSize + 4096, worldSize - 4096)), out _), Is.True);
            Assert.That(trail.TryAppendSample(new TerritoryTrailSample(sessionId, 3, 3, new FixedTerritoryPoint(worldSize / 2, worldSize - 4096)), out _), Is.True);
            Assert.That(trail.TryCommit(sessionId, out _), Is.True);

            var expansion = new TerritoryChunkExpansionSession();
            Assert.That(expansion.TryBegin(index, sessionId, out _), Is.True);
            foreach (TerritoryTrailFragment fragment in trail.Fragments)
                Assert.That(expansion.TryAppendFragment(fragment, out string appendReason), Is.True, appendReason);
            Assert.That(expansion.TryComplete(sessionId, out TerritoryChunkExpansionPlan plan, out string reason), Is.True, reason);

            Assert.That(index.SegmentCount, Is.GreaterThan(400));
            Assert.That(plan.TrailPoints.Count, Is.GreaterThanOrEqualTo(4));
            Assert.That(plan.Metrics.TerminalBoundarySegmentScans, Is.Zero);
            Assert.That(plan.Metrics.TerminalTrailPointScans, Is.Zero);
            Assert.That(plan.Metrics.BoundaryCandidateChecks,
                Is.LessThan((long)plan.Metrics.TrailPointCount * 64));
        }

        private TerritoryBoundaryLoopIndex BuildRectangleIndex(int minX, int minY, int maxX, int maxY)
        {
            FixedTerritoryPoint[] polygon =
            {
                new(minX, minY),
                new(maxX, minY),
                new(maxX, maxY),
                new(minX, maxY)
            };
            Assert.That(_builder.TryBuild(polygon, 1, out TerritoryChunkSnapshot snapshot, out string buildReason), Is.True, buildReason);
            Assert.That(TerritoryBoundaryLoopIndex.TryCreate(snapshot, out TerritoryBoundaryLoopIndex index, out string indexReason), Is.True, indexReason);
            return index;
        }

        private static TerritoryChunkExpansionPlan Complete(
            TerritoryBoundaryLoopIndex index,
            ulong sessionId,
            params FixedTerritoryPoint[] path)
        {
            var session = new TerritoryChunkExpansionSession();
            Assert.That(session.TryBegin(index, sessionId, out string beginReason), Is.True, beginReason);
            return AppendAndComplete(session, sessionId, path);
        }

        private static TerritoryChunkExpansionPlan AppendAndComplete(
            TerritoryChunkExpansionSession session,
            ulong sessionId,
            params FixedTerritoryPoint[] path)
        {
            Assert.That(session.TryAppendFragment(Fragment(sessionId, 0, path), out string appendReason), Is.True, appendReason);
            Assert.That(session.TryComplete(sessionId, out TerritoryChunkExpansionPlan plan, out string completeReason), Is.True, completeReason);
            return plan;
        }

        private static TerritoryChunkExpansionPlan CompleteFromSamples(
            TerritoryBoundaryLoopIndex index,
            ulong sessionId,
            params FixedTerritoryPoint[] path)
        {
            var trail = new TerritoryTrailSession();
            Assert.That(trail.TryBegin(new TerritoryTrailSample(sessionId, 0, 0, path[0]), out string beginReason), Is.True, beginReason);
            for (int i = 1; i < path.Length; i++)
            {
                Assert.That(trail.TryAppendSample(
                    new TerritoryTrailSample(sessionId, (uint)i, i, path[i]), out string appendReason), Is.True, appendReason);
            }
            Assert.That(trail.TryCommit(sessionId, out string commitReason), Is.True, commitReason);

            var expansion = new TerritoryChunkExpansionSession();
            Assert.That(expansion.TryBegin(index, sessionId, out beginReason), Is.True, beginReason);
            foreach (TerritoryTrailFragment fragment in trail.Fragments)
                Assert.That(expansion.TryAppendFragment(fragment, out string fragmentReason), Is.True, fragmentReason);
            Assert.That(expansion.TryComplete(sessionId, out TerritoryChunkExpansionPlan plan, out string completeReason), Is.True, completeReason);
            return plan;
        }

        private static TerritoryTrailFragment Fragment(
            ulong sessionId,
            uint sequence,
            params FixedTerritoryPoint[] points)
        {
            TerritoryChunkCoordinate chunk = TerritoryChunkCoordinate.FromPoint(points[0]);
            Assert.That(points.All(chunk.ContainsClosed), Is.True, "Test fragment must remain in one closed Chunk.");
            return new TerritoryTrailFragment(sessionId, sequence, chunk, 0, (uint)points.Length - 1, points);
        }
    }
}
