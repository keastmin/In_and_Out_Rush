using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectIO.Territory;
using UnityEngine;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritorySpatialIndexTests
    {
        private const float Epsilon = TerritorySpatialIndex.Epsilon;

        [Test]
        public void ChunkQuadtreeQueryMatchesReferenceAtConcaveAndNegativeBoundaries()
        {
            List<Vector2> polygon = new()
            {
                new(-24f, -16f), new(24f, -16f), new(24f, 16f), new(8f, 16f),
                new(8f, 0f), new(-8f, 0f), new(-8f, 16f), new(-24f, 16f)
            };
            Vector2[] points =
            {
                new(0f, -8f), new(0f, 8f), new(-16f, 8f), new(-24f, 0f),
                new(8f, 4f), new(-8f - Epsilon * 0.5f, 4f),
                new(-8f - Epsilon * 2f, 4f), new(24f, 16f), new(30f, 0f)
            };

            var index = new TerritorySpatialIndex();
            Assert.That(index.Rebuild(polygon), Is.True);
            Assert.That(index.ChunkCount, Is.GreaterThan(0));
            Assert.That(index.NodeCount, Is.GreaterThanOrEqualTo(index.ChunkCount));
            foreach (Vector2 point in points)
                Assert.That(index.Contains(point), Is.EqualTo(ReferenceContains(point, polygon)), point.ToString());
        }

        [Test]
        public void FixedSeedRandomQueriesMatchReferenceForLargeConcavePolygon()
        {
            List<Vector2> polygon = CreateWavyPolygon(1024, 160f, 24f);
            var index = new TerritorySpatialIndex();
            Assert.That(index.Rebuild(polygon), Is.True);
            var random = new System.Random(20260829);
            for (int i = 0; i < 4096; i++)
            {
                Vector2 point = new(
                    Mathf.Lerp(-210f, 210f, (float)random.NextDouble()),
                    Mathf.Lerp(-210f, 210f, (float)random.NextDouble()));
                Assert.That(index.Contains(point), Is.EqualTo(ReferenceContains(point, polygon)), point.ToString());
            }
        }

        [Test]
        public void ThousandsOfEdgesInspectOnlySpatialCandidates()
        {
            List<Vector2> polygon = CreateCircle(4096, 240f);
            var index = new TerritorySpatialIndex();
            Assert.That(index.Rebuild(polygon), Is.True);

            for (int i = 0; i < 512; i++)
            {
                float x = Mathf.Lerp(-200f, 200f, i / 511f);
                Assert.That(index.Contains(new Vector2(x, 0.125f)), Is.True);
                Assert.That(index.LastCandidateEdgeCount, Is.LessThan(index.EdgeCount / 8));
            }

            Assert.That(index.CandidateEdgeInspectionCount,
                Is.LessThan(index.QueryCount * index.EdgeCount / 8));
        }

        [Test]
        public void ReplaceVerticesPublishesSnapshotAndMatchingIndexTogether()
        {
            var territory = new global::Territory();
            List<Vector2> source = Rectangle(-20f, -20f, -4f, -4f);
            territory.ReplaceVertices(source);
            source[0] = Vector2.zero;
            Assert.That(territory.IsPointInPolygon(new Vector2(-12f, -12f)), Is.True);

            territory.ReplaceVertices(Rectangle(4f, 4f, 20f, 20f));
            Assert.That(territory.IsPointInPolygon(new Vector2(-12f, -12f)), Is.False);
            Assert.That(territory.IsPointInPolygon(new Vector2(12f, 12f)), Is.True);
        }

        [Test]
        public void QueryDoesNotAllocateAfterCandidateBuffersAreWarm()
        {
            var index = new TerritorySpatialIndex();
            Assert.That(index.Rebuild(CreateCircle(512, 100f)), Is.True);
            for (int i = 0; i < 64; i++)
                index.Contains(new Vector2(i % 20, i % 16));

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1024; i++)
                index.Contains(new Vector2(i % 20, i % 16));
            long after = GC.GetAllocatedBytesForCurrentThread();
            Assert.That(after - before, Is.EqualTo(0));
        }

        private static List<Vector2> Rectangle(float minimumX, float minimumY, float maximumX, float maximumY)
            => new()
            {
                new(minimumX, minimumY), new(minimumX, maximumY),
                new(maximumX, maximumY), new(maximumX, minimumY)
            };

        private static List<Vector2> CreateCircle(int count, float radius)
        {
            var polygon = new List<Vector2>(count);
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.PI * 2f * i / count;
                polygon.Add(new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
            }

            return polygon;
        }

        private static List<Vector2> CreateWavyPolygon(int count, float radius, float wave)
        {
            var polygon = new List<Vector2>(count);
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.PI * 2f * i / count;
                float localRadius = radius + Mathf.Sin(angle * 11f) * wave;
                polygon.Add(new Vector2(Mathf.Cos(angle) * localRadius, Mathf.Sin(angle) * localRadius));
            }

            return polygon;
        }

        private static bool ReferenceContains(Vector2 point, IReadOnlyList<Vector2> polygon)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                if (TerritorySpatialIndex.PointOnSegment(point, polygon[i], polygon[(i + 1) % polygon.Count]))
                    return true;
            }

            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 current = polygon[i];
                Vector2 previous = polygon[j];
                if ((current.y > point.y) != (previous.y > point.y))
                {
                    float atX = (previous.x - current.x) * (point.y - current.y) /
                                (previous.y - current.y) + current.x;
                    if (point.x < atX)
                        inside = !inside;
                }
            }

            return inside;
        }
    }
}
