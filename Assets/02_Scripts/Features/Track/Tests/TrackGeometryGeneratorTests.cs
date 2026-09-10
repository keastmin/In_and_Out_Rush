using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectIO.Tracks.Tests
{
    public sealed class TrackGeometryGeneratorTests
    {
        [Test]
        public void Ellipse_WithSameSeed_IsReproducible()
        {
            TrackPath first = TrackGeometryGenerator.CreateEllipse(Vector3.zero, 30, 16f, 8f, 0.1f, 1234);
            TrackPath second = TrackGeometryGenerator.CreateEllipse(Vector3.zero, 30, 16f, 8f, 0.1f, 1234);

            CollectionAssert.AreEqual(first.Vertices, second.Vertices);
            Assert.That(first.IsClosed, Is.True);
        }

        [TestCase(TrackAxis.Horizontal)]
        [TestCase(TrackAxis.Vertical)]
        [TestCase(TrackAxis.DiagonalPositive)]
        [TestCase(TrackAxis.DiagonalNegative)]
        public void PrimaryLine_HasRequestedLengthAndCenter(TrackAxis axis)
        {
            var center = new Vector3(9f, 2f, -4f);
            TrackPath line = TrackGeometryGenerator.CreateLine(center, 125f, axis, false);

            Assert.That(Vector3.Distance(line.Vertices[0], line.Vertices[1]), Is.EqualTo(125f).Within(0.001f));
            Assert.That(
                Vector3.Distance((line.Vertices[0] + line.Vertices[1]) * 0.5f, center),
                Is.LessThan(0.001f));
            Assert.That(line.IsClosed, Is.False);
        }

        [Test]
        public void PerpendicularStage_PreservesPrimaryAndAddsPerpendicularPath()
        {
            IReadOnlyList<TrackPath> primary = Create(TrackStage.PrimaryLine);
            IReadOnlyList<TrackPath> expanded = Create(TrackStage.PerpendicularLines);

            CollectionAssert.AreEqual(primary[0].Vertices, expanded[0].Vertices);
            Vector3 firstDirection = expanded[0].Vertices[1] - expanded[0].Vertices[0];
            Vector3 secondDirection = expanded[1].Vertices[1] - expanded[1].Vertices[0];
            Assert.That(Vector3.Dot(firstDirection.normalized, secondDirection.normalized), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void OpenPaths_DoNotCreatePhantomSegmentsBetweenPathsOrEndpoints()
        {
            IReadOnlyList<TrackPath> paths = Create(TrackStage.PerpendicularLines);
            IReadOnlyList<TrackSegment> segments = TrackGeometryGenerator.CreateSegments(paths);

            Assert.That(segments.Count, Is.EqualTo(2));
            Assert.That(segments[0].End, Is.Not.EqualTo(segments[1].Start));
        }

        private static IReadOnlyList<TrackPath> Create(TrackStage stage)
        {
            return TrackGeometryGenerator.CreatePaths(
                stage,
                Vector3.zero,
                30,
                16f,
                8f,
                0.1f,
                777,
                125f,
                TrackAxis.DiagonalPositive,
                false,
                true);
        }
    }
}
