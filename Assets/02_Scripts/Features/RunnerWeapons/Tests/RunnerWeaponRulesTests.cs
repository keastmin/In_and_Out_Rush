using NUnit.Framework;

namespace ProjectIO.RunnerWeapons.Tests
{
    public sealed class RunnerWeaponRulesTests
    {
        [Test]
        public void TryConsumeShot_ConsumesSixteenRoundsAndAlternatesHands()
        {
            int ammunition = 16;
            int shotSequence = 0;

            for (int shotIndex = 0; shotIndex < 16; shotIndex++)
            {
                bool consumed = RunnerWeaponRules.TryConsumeShot(
                    ref ammunition,
                    ref shotSequence,
                    out RunnerWeaponHand hand);

                Assert.That(consumed, Is.True);
                Assert.That(
                    hand,
                    Is.EqualTo(shotIndex % 2 == 0
                        ? RunnerWeaponHand.Left
                        : RunnerWeaponHand.Right));
            }

            Assert.That(ammunition, Is.Zero);
            Assert.That(shotSequence, Is.EqualTo(16));

            bool consumedEmpty = RunnerWeaponRules.TryConsumeShot(
                ref ammunition,
                ref shotSequence,
                out _);

            Assert.That(consumedEmpty, Is.False);
            Assert.That(shotSequence, Is.EqualTo(16));
        }

        [Test]
        public void ReloadRules_AllowPartialAndEmptyMagazinesButRejectFullOrActiveReload()
        {
            Assert.That(RunnerWeaponRules.CanStartReload(8, 16, false), Is.True);
            Assert.That(RunnerWeaponRules.CanStartReload(0, 16, false), Is.True);
            Assert.That(RunnerWeaponRules.CanStartReload(16, 16, false), Is.False);
            Assert.That(RunnerWeaponRules.CanStartReload(8, 16, true), Is.False);
            Assert.That(RunnerWeaponRules.CompleteReload(16), Is.EqualTo(16));
        }

        [Test]
        public void AutomaticReload_StartsOnlyForAnEmptyInactiveMagazine()
        {
            Assert.That(
                RunnerWeaponRules.ShouldStartAutomaticReload(0, 16, false),
                Is.True);
            Assert.That(
                RunnerWeaponRules.ShouldStartAutomaticReload(1, 16, false),
                Is.False);
            Assert.That(
                RunnerWeaponRules.ShouldStartAutomaticReload(0, 16, true),
                Is.False);
        }

        [Test]
        public void PartialReload_DoesNotResetAlternatingShotSequence()
        {
            int ammunition = 13;
            int shotSequence = 3;

            ammunition = RunnerWeaponRules.CompleteReload(16);
            bool consumed = RunnerWeaponRules.TryConsumeShot(
                ref ammunition,
                ref shotSequence,
                out RunnerWeaponHand hand);

            Assert.That(consumed, Is.True);
            Assert.That(hand, Is.EqualTo(RunnerWeaponHand.Right));
            Assert.That(shotSequence, Is.EqualTo(4));
        }

        [Test]
        public void TimingRules_ApplyAttackAndReloadSpeedScalers()
        {
            Assert.That(
                RunnerWeaponRules.GetShotInterval(4f, 1f),
                Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(
                RunnerWeaponRules.GetShotInterval(4f, 1.5f),
                Is.EqualTo(1f / 6f).Within(0.0001f));
            Assert.That(
                RunnerWeaponRules.GetReloadDuration(2.5f, 1f),
                Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(
                RunnerWeaponRules.GetReloadDuration(2.5f, 1.5f),
                Is.EqualTo(2.5f / 1.5f).Within(0.0001f));
        }
    }
}
