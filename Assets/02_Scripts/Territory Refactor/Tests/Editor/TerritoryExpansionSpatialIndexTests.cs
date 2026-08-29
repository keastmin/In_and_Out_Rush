using System.Collections.Generic;
using NUnit.Framework;
using ProjectIO.Territory;
using UnityEngine;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryExpansionSpatialIndexTests
    {
        [Test]
        public void ExitAndReentryProduceTheSameExpandedBoundaryContract()
        {
            List<Vector2> source = Rectangle();
            Vector2[] trail =
            {
                new(9f, 2f), new(14f, 2f), new(14f, 8f), new(9f, 8f)
            };

            Assert.That(TerritoryExpansionCalculator.TryCalculate(
                source,
                trail,
                out TerritoryMeshData result), Is.True);
            Assert.That(Mathf.Abs(TerritoryPolygonTriangulator.Area(result.Vertices)),
                Is.GreaterThan(Mathf.Abs(TerritoryPolygonTriangulator.Area(source))));
            Assert.That(result.Triangles.Count, Is.EqualTo((result.Vertices.Count - 2) * 3));
            Assert.That(TerritorySpatialIndex.IsSimplePolygon(result.Vertices), Is.True);
        }

        [Test]
        public void LongDiagonalPathFindsBoundaryWithoutPolygonTimesPathScan()
        {
            List<Vector2> source = CreateCircle(2048, 100f);
            Vector2[] trail =
            {
                new(99f, 0f), new(180f, 80f), new(150f, 120f), new(0f, 99f)
            };

            Assert.That(TerritoryExpansionCalculator.TryCalculate(
                source,
                trail,
                out TerritoryMeshData result), Is.True);
            Assert.That(result.Vertices.Count, Is.GreaterThan(3));
        }

        [Test]
        public void BoundaryOverlapAndSelfIntersectingCandidateAreRejected()
        {
            List<Vector2> source = Rectangle();
            Assert.That(TerritoryExpansionCalculator.TryCalculate(
                source,
                new[] { new Vector2(0f, 2f), new Vector2(0f, 8f) },
                out _), Is.False);

            Assert.That(TerritorySpatialIndex.IsSimplePolygon(new[]
            {
                new Vector2(0f, 0f), new Vector2(10f, 10f),
                new Vector2(0f, 10f), new Vector2(10f, 0f)
            }), Is.False);
        }

        private static List<Vector2> Rectangle()
            => new()
            {
                new(0f, 0f), new(0f, 10f), new(10f, 10f), new(10f, 0f)
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
    }
}
