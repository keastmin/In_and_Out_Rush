using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryChunkStateBuilderTests
    {
        private readonly TerritoryChunkStateBuilder _builder = new();

        [Test]
        public void LargeRectangleClassifiesInteriorFullBoundaryAndExteriorEmpty()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            FixedTerritoryPoint[] polygon = Rectangle(100, 100, size * 3 - 100, size * 3 - 100);

            Assert.That(_builder.TryBuild(polygon, 1, out TerritoryChunkSnapshot snapshot, out _), Is.True);

            Assert.That(snapshot.GetFill(new TerritoryChunkCoordinate(1, 1)), Is.EqualTo(TerritoryChunkFill.Full));
            Assert.That(snapshot.GetFill(new TerritoryChunkCoordinate(0, 0)), Is.EqualTo(TerritoryChunkFill.Boundary));
            Assert.That(snapshot.GetFill(new TerritoryChunkCoordinate(2, 2)), Is.EqualTo(TerritoryChunkFill.Boundary));
            Assert.That(snapshot.GetFill(new TerritoryChunkCoordinate(3, 3)), Is.EqualTo(TerritoryChunkFill.Empty));
            Assert.That(snapshot.TryGetCoverage(new TerritoryChunkCoordinate(1, 1), out TerritoryChunkCoverage full), Is.True);
            Assert.That(full.Segments, Is.Empty);
        }

        [Test]
        public void NegativeCoordinatesAndHalfOpenEdgesRemainDeterministic()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            FixedTerritoryPoint[] polygon = Rectangle(-size, -size, size, size);

            Assert.That(_builder.TryBuild(polygon, 7, out TerritoryChunkSnapshot snapshot, out _), Is.True);

            Assert.That(snapshot.GetFill(new TerritoryChunkCoordinate(0, 0)), Is.EqualTo(TerritoryChunkFill.Full));
            Assert.That(snapshot.GetFill(new TerritoryChunkCoordinate(-1, -1)), Is.EqualTo(TerritoryChunkFill.Boundary));
            Assert.That(snapshot.GetFill(new TerritoryChunkCoordinate(1, 0)), Is.EqualTo(TerritoryChunkFill.Boundary));
            Assert.That(snapshot.GetFill(new TerritoryChunkCoordinate(-2, -2)), Is.EqualTo(TerritoryChunkFill.Empty));
        }

        [Test]
        public void ChunkLocalBoundarySegmentsReconstructTheExactFixedPolygonPath()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            FixedTerritoryPoint[] polygon =
            {
                new(100, 200),
                new(size * 2 + 333, 500),
                new(size * 2 + 100, size * 2 + 700),
                new(-400, size + 200)
            };

            Assert.That(_builder.TryBuild(polygon, 3, out TerritoryChunkSnapshot snapshot, out _), Is.True);

            var ordered = snapshot.Chunks.Values
                .Where(coverage => coverage.Fill == TerritoryChunkFill.Boundary)
                .SelectMany(coverage => coverage.Segments.Select(segment => (coverage.Chunk, Segment: segment)))
                .OrderBy(entry => entry.Segment.Sequence)
                .ToArray();

            Assert.That(ordered, Is.Not.Empty);
            Assert.That(ordered[0].Segment.Start.ToGlobal(ordered[0].Chunk), Is.EqualTo(polygon[0]));
            for (int i = 0; i < ordered.Length; i++)
            {
                Assert.That(ordered[i].Segment.Sequence, Is.EqualTo(i));
                FixedTerritoryPoint end = ordered[i].Segment.End.ToGlobal(ordered[i].Chunk);
                var next = ordered[(i + 1) % ordered.Length];
                FixedTerritoryPoint nextStart = next.Segment.Start.ToGlobal(next.Chunk);
                Assert.That(end, Is.EqualTo(nextStart));
            }
        }

        [Test]
        public void ConcavePolygonKeepsItsNotchEmpty()
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            FixedTerritoryPoint[] polygon =
            {
                new(100, 100),
                new(size * 5 - 100, 100),
                new(size * 5 - 100, size * 2 + 100),
                new(size * 2 + 100, size * 2 + 100),
                new(size * 2 + 100, size * 5 - 100),
                new(100, size * 5 - 100)
            };

            Assert.That(_builder.TryBuild(polygon, 4, out TerritoryChunkSnapshot snapshot, out _), Is.True);

            Assert.That(snapshot.GetFill(new TerritoryChunkCoordinate(1, 3)), Is.EqualTo(TerritoryChunkFill.Full));
            Assert.That(snapshot.GetFill(new TerritoryChunkCoordinate(3, 1)), Is.EqualTo(TerritoryChunkFill.Full));
            Assert.That(snapshot.GetFill(new TerritoryChunkCoordinate(3, 3)), Is.EqualTo(TerritoryChunkFill.Empty));
        }

        [Test]
        public void SelfIntersectingAndDegeneratePolygonsAreRejected()
        {
            FixedTerritoryPoint[] bowTie =
            {
                new(0, 0),
                new(1000, 1000),
                new(0, 1000),
                new(1000, 0)
            };
            FixedTerritoryPoint[] line =
            {
                new(0, 0),
                new(1000, 0),
                new(2000, 0)
            };

            Assert.That(_builder.TryBuild(bowTie, 1, out _, out string intersectionReason), Is.False);
            Assert.That(intersectionReason, Does.Contain("area").Or.Contain("simple"));
            Assert.That(_builder.TryBuild(line, 1, out _, out string lineReason), Is.False);
            Assert.That(lineReason, Does.Contain("three").Or.Contain("area"));
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
