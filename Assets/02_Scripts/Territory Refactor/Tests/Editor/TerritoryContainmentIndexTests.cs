using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectIO.Territory;
using UnityEngine;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryContainmentIndexTests
    {
        private const float Epsilon = 0.0001f;

        [Test]
        public void IndexedQueryMatchesReferenceForBoundaryAndBucketCases()
        {
            List<Vector2> convex = new()
            {
                new(-16f, -8f), new(-16f, 8f), new(16f, 8f), new(16f, -8f)
            };
            List<Vector2> concave = new()
            {
                new(-12f, -8f), new(12f, -8f), new(12f, 8f), new(2f, 8f),
                new(2f, -2f), new(-2f, -2f), new(-2f, 8f), new(-12f, 8f)
            };

            AssertEquivalent(convex, new Vector2(0f, 0f));
            AssertEquivalent(convex, new Vector2(17f, 0f));
            AssertEquivalent(convex, new Vector2(-16f, 0f));
            AssertEquivalent(convex, new Vector2(0f, 8f));
            AssertEquivalent(convex, new Vector2(-16f - Epsilon * 0.5f, 0f));
            AssertEquivalent(convex, new Vector2(-16f - Epsilon * 2f, 0f));
            AssertEquivalent(concave, new Vector2(0f, 4f));
            AssertEquivalent(concave, new Vector2(-8f, 4f));
            AssertEquivalent(concave, new Vector2(2f, 3f));
            AssertEquivalent(concave, new Vector2(0f, 8f));

            List<Vector2> diagonal = new()
            {
                new(-24f, -8f), new(0f, 24f), new(24f, -8f), new(0f, -24f)
            };
            AssertEquivalent(diagonal, new Vector2(-6f, 0f));
            AssertEquivalent(diagonal, new Vector2(0f, 8f));
            AssertEquivalent(diagonal, new Vector2(0f, 8f + Epsilon * 2f));
        }

        [Test]
        public void FixedSeedRandomizedPointsMatchReferenceForConcavePolygon()
        {
            List<Vector2> polygon = new()
            {
                new(-30f, -20f), new(30f, -20f), new(30f, 20f), new(8f, 20f),
                new(8f, 0f), new(-8f, 0f), new(-8f, 20f), new(-30f, 20f)
            };
            var random = new System.Random(20260826);

            for (int i = 0; i < 4096; i++)
            {
                var point = new Vector2(
                    Mathf.Lerp(-40f, 40f, (float)random.NextDouble()),
                    Mathf.Lerp(-30f, 30f, (float)random.NextDouble()));
                AssertEquivalent(polygon, point);
            }
        }

        [Test]
        public void ThousandsOfVerticesUseFewerCandidateEdgesAndMatchReference()
        {
            List<Vector2> polygon = CreateCircle(4096, 240f);
            var index = new TerritoryContainmentIndex();
            index.Rebuild(polygon);
            Assert.That(index.IsValid, Is.True);

            var random = new System.Random(5101);
            for (int i = 0; i < 1024; i++)
            {
                var point = new Vector2(
                    Mathf.Lerp(-260f, 260f, (float)random.NextDouble()),
                    Mathf.Lerp(-260f, 260f, (float)random.NextDouble()));
                Assert.That(index.TryContains(point, polygon, out bool indexed), Is.True);
                Assert.That(indexed, Is.EqualTo(ReferenceContains(point, polygon)));
            }

            Assert.That(index.LastCandidateEdgeCount, Is.LessThan(index.EdgeCount));
            Assert.That(index.CandidateEdgeCount, Is.LessThan(index.QueryCount * index.EdgeCount));
        }

        [Test]
        public void ReplaceVerticesRebuildsAndDoesNotKeepThePreviousPolygon()
        {
            var territory = new global::Territory();
            List<Vector2> first = Rectangle(-20f, -20f, -4f, -4f);
            List<Vector2> second = Rectangle(4f, 4f, 20f, 20f);
            Vector2 oldOnlyPoint = new(-12f, -12f);
            Vector2 newOnlyPoint = new(12f, 12f);

            territory.ReplaceVertices(first);
            Assert.That(territory.IsPointInPolygon(oldOnlyPoint), Is.True);
            Assert.That(territory.IsPointInPolygon(newOnlyPoint), Is.False);

            territory.ReplaceVertices(second);
            Assert.That(territory.IsPointInPolygon(oldOnlyPoint), Is.False);
            Assert.That(territory.IsPointInPolygon(newOnlyPoint), Is.True);
        }

        [Test]
        public void PathologicalLongEdgesFallBackToReference()
        {
            List<Vector2> polygon = Rectangle(-4f, -10000f, 4f, 10000f);
            var index = new TerritoryContainmentIndex();
            index.Rebuild(polygon);

            Assert.That(index.IsValid, Is.False);
            Assert.That(index.TryContains(Vector2.zero, polygon, out _), Is.False);

            var territory = new global::Territory();
            territory.ReplaceVertices(polygon);
            Assert.That(territory.IsPointInPolygon(Vector2.zero), Is.EqualTo(ReferenceContains(Vector2.zero, polygon)));
            Assert.That(territory.IsPointInPolygon(new Vector2(6f, 0f)), Is.EqualTo(ReferenceContains(new Vector2(6f, 0f), polygon)));
        }

        [Test]
        public void IndexedQueriesDoNotAllocateManagedMemoryAfterWarmup()
        {
            List<Vector2> polygon = CreateCircle(256, 100f);
            var index = new TerritoryContainmentIndex();
            index.Rebuild(polygon);
            Assert.That(index.TryContains(new Vector2(1f, 1f), polygon, out _), Is.True);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1024; i++)
                Assert.That(index.TryContains(new Vector2(i % 20, i % 16), polygon, out _), Is.True);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(after - before, Is.EqualTo(0));
        }

        private static void AssertEquivalent(IReadOnlyList<Vector2> polygon, Vector2 point)
        {
            var index = new TerritoryContainmentIndex();
            index.Rebuild(polygon);
            Assert.That(index.TryContains(point, polygon, out bool indexed), Is.True);
            Assert.That(indexed, Is.EqualTo(ReferenceContains(point, polygon)));
        }

        private static List<Vector2> Rectangle(float minX, float minY, float maxX, float maxY)
            => new()
            {
                new(minX, minY), new(minX, maxY), new(maxX, maxY), new(maxX, minY)
            };

        private static List<Vector2> CreateCircle(int vertexCount, float radius)
        {
            var polygon = new List<Vector2>(vertexCount);
            for (int i = 0; i < vertexCount; i++)
            {
                float angle = Mathf.PI * 2f * i / vertexCount;
                polygon.Add(new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
            }

            return polygon;
        }

        private static bool ReferenceContains(Vector2 point, IReadOnlyList<Vector2> polygon)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                if (PointOnSegment(point, polygon[i], polygon[(i + 1) % polygon.Count]))
                    return true;
            }

            bool isInside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 pi = polygon[i];
                Vector2 pj = polygon[j];
                if ((pi.y > point.y) != (pj.y > point.y))
                {
                    float atX = (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x;
                    if (point.x < atX)
                        isInside = !isInside;
                }
            }

            return isInside;
        }

        private static bool PointOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 segment = b - a;
            if (Vector2.SqrMagnitude(segment) <= Epsilon * Epsilon)
                return Vector2.SqrMagnitude(point - a) <= Epsilon * Epsilon;

            float cross = segment.x * (point.y - a.y) - segment.y * (point.x - a.x);
            if (Mathf.Abs(cross) > Epsilon)
                return false;

            return point.x >= Mathf.Min(a.x, b.x) - Epsilon &&
                   point.x <= Mathf.Max(a.x, b.x) + Epsilon &&
                   point.y >= Mathf.Min(a.y, b.y) - Epsilon &&
                   point.y <= Mathf.Max(a.y, b.y) + Epsilon;
        }
    }
}
