using NUnit.Framework;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryTrailShadowComparerTests
    {
        [Test]
        public void IdenticalPointOrderMatches()
        {
            FixedTerritoryPoint[] legacyPoints =
            {
                new(1, 2),
                new(3, 4),
                new(5, 6)
            };
            TerritoryTrailSample[] shadowSamples = Samples(legacyPoints);

            TerritoryTrailShadowComparison result =
                TerritoryTrailShadowComparer.Compare(legacyPoints, shadowSamples);

            Assert.That(result.IsMatch, Is.True);
            Assert.That(result.LegacyPointCount, Is.EqualTo(3));
            Assert.That(result.ShadowSampleCount, Is.EqualTo(3));
            Assert.That(result.FirstMismatchIndex, Is.EqualTo(-1));
        }

        [Test]
        public void CoordinateMismatchReportsFirstDifferentIndex()
        {
            FixedTerritoryPoint[] legacyPoints =
            {
                new(1, 2),
                new(3, 4),
                new(5, 6)
            };
            TerritoryTrailSample[] shadowSamples = Samples(
                new FixedTerritoryPoint(1, 2),
                new FixedTerritoryPoint(30, 40),
                new FixedTerritoryPoint(50, 60));

            TerritoryTrailShadowComparison result =
                TerritoryTrailShadowComparer.Compare(legacyPoints, shadowSamples);

            Assert.That(result.IsMatch, Is.False);
            Assert.That(result.FirstMismatchIndex, Is.EqualTo(1));
        }

        [Test]
        public void CountMismatchReportsSharedPrefixLength()
        {
            FixedTerritoryPoint[] legacyPoints =
            {
                new(1, 2),
                new(3, 4),
                new(5, 6)
            };
            TerritoryTrailSample[] shadowSamples = Samples(legacyPoints[0], legacyPoints[1]);

            TerritoryTrailShadowComparison result =
                TerritoryTrailShadowComparer.Compare(legacyPoints, shadowSamples);

            Assert.That(result.IsMatch, Is.False);
            Assert.That(result.LegacyPointCount, Is.EqualTo(3));
            Assert.That(result.ShadowSampleCount, Is.EqualTo(2));
            Assert.That(result.FirstMismatchIndex, Is.EqualTo(2));
        }

        private static TerritoryTrailSample[] Samples(params FixedTerritoryPoint[] points)
        {
            var samples = new TerritoryTrailSample[points.Length];
            for (int index = 0; index < points.Length; index++)
                samples[index] = new TerritoryTrailSample(1, (uint)index, index, points[index]);

            return samples;
        }
    }
}
