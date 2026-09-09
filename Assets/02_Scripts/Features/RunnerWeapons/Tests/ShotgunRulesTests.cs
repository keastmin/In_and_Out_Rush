using System;
using NUnit.Framework;

namespace ProjectIO.RunnerWeapons.Tests
{
    public sealed class ShotgunRulesTests
    {
        [TestCase(0f, 36f)]
        [TestCase(3f, 36f)]
        [TestCase(3.0001f, 18f)]
        [TestCase(5f, 18f)]
        [TestCase(5.0001f, 9f)]
        [TestCase(7f, 9f)]
        [TestCase(7.0001f, 0f)]
        public void DamageBands_IncludeThreeFiveAndSevenMeterBoundaries(
            float distance,
            float expectedDamage)
        {
            Assert.That(
                ShotgunRules.GetBaseDamage(distance, 3f, 5f, 7f, 36f, 18f, 9f),
                Is.EqualTo(expectedDamage).Within(0.0001f));
        }

        [Test]
        public void Cone_IncludesTwelveDegreeAndSevenMeterBoundaries()
        {
            const double boundaryAngleRadians = 12d * Math.PI / 180d;
            float boundaryX = (float)Math.Sin(boundaryAngleRadians) * 7f;
            float boundaryY = (float)Math.Cos(boundaryAngleRadians) * 7f;

            Assert.That(
                ShotgunRules.IsInsideCone(0f, 1f, boundaryX, boundaryY, 7f, 24f),
                Is.True);
            Assert.That(
                ShotgunRules.IsInsideCone(0f, 1f, boundaryX, boundaryY, 6.99f, 24f),
                Is.False);

            const double outsideAngleRadians = 12.1d * Math.PI / 180d;
            Assert.That(
                ShotgunRules.IsInsideCone(
                    0f,
                    1f,
                    (float)Math.Sin(outsideAngleRadians) * 6f,
                    (float)Math.Cos(outsideAngleRadians) * 6f,
                    7f,
                    24f),
                Is.False);
        }

        [Test]
        public void RunningAccuracy_IsIndependentPerSampleAndStationaryAlwaysHits()
        {
            Assert.That(ShotgunRules.IsTargetHit(false, 0.99f, 0.5f), Is.True);
            Assert.That(ShotgunRules.IsTargetHit(true, 0.1f, 0.5f), Is.True);
            Assert.That(ShotgunRules.IsTargetHit(true, 0.9f, 0.5f), Is.False);
            Assert.That(ShotgunRules.IsTargetHit(true, 0.5f, 0.5f), Is.False);
        }

        [Test]
        public void PelletAngles_AreDeterministicDistinctAndInsideCone()
        {
            const int pelletCount = 8;
            float[] first = new float[pelletCount];
            float[] second = new float[pelletCount];

            for (int pelletIndex = 0; pelletIndex < pelletCount; pelletIndex++)
            {
                first[pelletIndex] = ShotgunRules.GetPelletAngleDegrees(
                    12345,
                    7,
                    pelletIndex,
                    24f);
                second[pelletIndex] = ShotgunRules.GetPelletAngleDegrees(
                    12345,
                    7,
                    pelletIndex,
                    24f);

                Assert.That(first[pelletIndex], Is.InRange(-12f, 12f));
                Assert.That(second[pelletIndex], Is.EqualTo(first[pelletIndex]));
            }

            Assert.That(first, Is.Unique);
            Assert.That(
                ShotgunRules.GetPelletAngleDegrees(12345, 8, 0, 24f),
                Is.Not.EqualTo(first[0]));
        }
    }
}
