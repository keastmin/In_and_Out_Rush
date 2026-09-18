using System;
using NUnit.Framework;

namespace ProjectIO.Monsters.Tests
{
    public sealed class WorldMonsterPopulationPolicyTests
    {
        [Test]
        public void TimedEvents_TriggerAtThresholdOnceAndResetForNextStage()
        {
            var policy = new WorldMonsterPopulationPolicy();
            Assert.That(policy.TryBeginCull(899.99f), Is.False);
            Assert.That(policy.TryBeginHealthReduction(1499.99f), Is.False);
            Assert.That(policy.TryBeginCull(900f), Is.True);
            Assert.That(policy.TryBeginCull(900f), Is.False);
            Assert.That(policy.TryBeginCull(1500f), Is.False);
            Assert.That(policy.TryBeginHealthReduction(1500f), Is.True);
            Assert.That(policy.TryBeginHealthReduction(1500f), Is.False);
            Assert.That(policy.TryBeginHealthReduction(2000f), Is.False);
            policy.Reset();
            Assert.That(policy.TryBeginCull(1600f), Is.True);
            Assert.That(policy.TryBeginHealthReduction(1600f), Is.True);
        }

        [Test]
        public void CustomTimes_TriggerAtConfiguredSecondsAndDoNotRepeatAfterEditing()
        {
            var policy = new WorldMonsterPopulationPolicy();
            Assert.That(policy.TryBeginCull(9.99f, 10f), Is.False);
            Assert.That(policy.TryBeginHealthReduction(19.99f, 20f), Is.False);
            Assert.That(policy.TryBeginCull(10f, 10f), Is.True);
            Assert.That(policy.TryBeginHealthReduction(20f, 20f), Is.True);
            Assert.That(policy.TryBeginCull(30f, 5f), Is.False);
            Assert.That(policy.TryBeginHealthReduction(30f, 5f), Is.False);
            Assert.That(policy.TryBeginCull(200f, 100f), Is.False);
            Assert.That(policy.TryBeginHealthReduction(200f, 100f), Is.False);
        }

        [Test]
        public void PendingEvent_CanBeMovedEarlierAndZeroTriggersImmediately()
        {
            var policy = new WorldMonsterPopulationPolicy();
            Assert.That(policy.TryBeginCull(15f, 900f), Is.False);
            Assert.That(policy.TryBeginCull(15f, 10f), Is.True);
            Assert.That(policy.TryBeginHealthReduction(0f, 0f), Is.True);
        }

        [TestCase(0f, 400f, 0f)]
        [TestCase(200f, 400f, 0.5f)]
        [TestCase(400f, 400f, 0.6666667f)]
        [TestCase(450f, 400f, 0.6666667f)]
        [TestCase(0f, 0f, 0f)]
        public void Culling_UsesClampedGroupRadius(float distance, float radius, float expected)
        {
            Assert.That(WorldMonsterPopulationPolicy.GetCullProbability(distance, radius),
                Is.EqualTo(expected).Within(0.00001f));
        }

        [Test]
        public void InitialDistribution_AndCullingProduceExpectedAreaDensities()
        {
            const int samples = 100000;
            const int bins = 5;
            var initial = new int[bins];
            var survivors = new int[bins];
            var random = new Random(1701);
            for (int i = 0; i < samples; i++)
            {
                float radius = WorldMonsterPopulationPolicy.SampleNormalizedRadius((float)random.NextDouble());
                int bin = Math.Min(bins - 1, (int)(radius * radius * bins));
                initial[bin]++;
                if (random.NextDouble() >= WorldMonsterPopulationPolicy.GetCullProbability(radius, 1f))
                    survivors[bin]++;
            }

            // Equal-area annuli: initial probability follows the integral of 1 + 2r.
            for (int i = 0; i < bins; i++)
            {
                double lower = Math.Sqrt((double)i / bins);
                double upper = Math.Sqrt((double)(i + 1) / bins);
                double expected = samples * (3 * (upper * upper - lower * lower) +
                    4 * (upper * upper * upper - lower * lower * lower)) / 7;
                Assert.That(initial[i], Is.EqualTo(expected).Within(samples * 0.005));
                // Thinning leaves 3/7 of the original population, uniformly over area.
                Assert.That(survivors[i], Is.EqualTo(samples * 3.0 / 7 / bins).Within(samples * 0.005));
            }
        }

        [Test]
        public void RadiusSampler_StaysInBoundsAndIncludesEndpoints()
        {
            Assert.That(WorldMonsterPopulationPolicy.SampleNormalizedRadius(0f), Is.Zero);
            Assert.That(WorldMonsterPopulationPolicy.SampleNormalizedRadius(1f), Is.EqualTo(1f));
            float previous = 0f;
            for (int i = 1; i <= 1000; i++)
            {
                float radius = WorldMonsterPopulationPolicy.SampleNormalizedRadius(i / 1000f);
                Assert.That(radius, Is.InRange(previous, 1f));
                previous = radius;
            }
        }

        [TestCase(10f, 2.5f)]
        [TestCase(3f, 0.75f)]
        [TestCase(0f, 0f)]
        public void Weakening_QuartersCurrentHealthWithoutRounding(float current, float expected)
        {
            Assert.That(WorldMonsterPopulationPolicy.ReduceCurrentHealth(current), Is.EqualTo(expected));
        }
    }
}
