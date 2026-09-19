using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectIO.Monsters.Tests
{
    public sealed class RafflesiaAttackPatternTests
    {
        [Test]
        public void Cycle_WaitsFromLastVolleyAndReturnsToOctagon()
        {
            var pattern = new RafflesiaAttackPattern();
            var angles = new float[8];
            double[] times = { 0, 1.5, 1.75, 2, 2.25, 3.75, 4, 4.25, 4.5,
                6, 6.15, 6.3, 6.45, 6.6, 6.75, 6.9, 7.05, 8.55 };
            for (int i = 0; i < times.Length; i++)
            {
                if (i > 0)
                    Assert.That(pattern.TryEmit(times[i] - 0.001, angles), Is.Zero);
                int count = pattern.TryEmit(times[i] + 0.000001 * i, angles);
                Assert.That(count, Is.EqualTo(i == 0 || i == 17 ? 8 : i < 9 ? 4 : 2));
                if (i > 0 && i < 5)
                    CollectionAssert.AreEqual(new[] { 0f, 90f, 180f, 270f }, Slice(angles, count));
                if (i >= 5 && i < 9)
                    CollectionAssert.AreEqual(new[] { 45f, 135f, 225f, 315f }, Slice(angles, count));
            }
        }

        [Test]
        public void Rotation_EmitsSixteenDistinctDirectionsClockwiseFromLeftAndRight()
        {
            var pattern = new RafflesiaAttackPattern();
            var angles = new float[8];
            // Advance the first nine volleys; delayed ticks do not bunch emissions together.
            for (int i = 0; i < 9; i++)
                pattern.TryEmit(i * 10, angles);
            var directions = new HashSet<float>();
            for (int i = 0; i < 8; i++)
            {
                Assert.That(pattern.TryEmit(100 + i * 0.151, angles), Is.EqualTo(2));
                Assert.That(angles[0], Is.EqualTo((270f + i * 22.5f) % 360f));
                Assert.That(angles[1], Is.EqualTo(90f + i * 22.5f));
                directions.Add(angles[0]);
                directions.Add(angles[1]);
            }
            Assert.That(directions.Count, Is.EqualTo(16));
            Assert.That(pattern.TryEmit(101.21, angles), Is.Zero);
        }

        [TestCase(1)]
        [TestCase(4)]
        [TestCase(7)]
        [TestCase(12)]
        public void TargetLost_ResetRestartsImmediatelyAtWorldAxisOctagon(int emitted)
        {
            var pattern = new RafflesiaAttackPattern();
            var angles = new float[8];
            for (int i = 0; i < emitted; i++)
                pattern.TryEmit(i * 10, angles);
            pattern.Reset();
            Assert.That(pattern.TryEmit(emitted * 10, angles), Is.EqualTo(8));
            CollectionAssert.AreEqual(new[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f }, angles);
            Assert.That(pattern.TryEmit(emitted * 10 + 1.49, angles), Is.Zero);
        }

        [Test]
        public void DelayedTick_DoesNotCatchUpWithABurstOrShortenNextWait()
        {
            var pattern = new RafflesiaAttackPattern();
            var angles = new float[8];
            Assert.That(pattern.TryEmit(0, angles), Is.EqualTo(8));
            Assert.That(pattern.TryEmit(10, angles), Is.EqualTo(4));
            Assert.That(pattern.TryEmit(10, angles), Is.Zero);
            Assert.That(pattern.TryEmit(10.249, angles), Is.Zero);
            Assert.That(pattern.TryEmit(10.25, angles), Is.EqualTo(4));
        }

        private static float[] Slice(float[] angles, int count)
        {
            var result = new float[count];
            global::System.Array.Copy(angles, result, count);
            return result;
        }
    }
}
