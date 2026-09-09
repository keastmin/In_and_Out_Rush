using NUnit.Framework;

namespace ProjectIO.RunnerWeapons.Tests
{
    public sealed class RunnerWeaponRulesTests
    {
        [Test]
        public void TryConsumeShot_ConsumesThirtyRoundsAndTracksSequence()
        {
            int ammunition = 30;
            int shotSequence = 0;

            for (int shotIndex = 0; shotIndex < 30; shotIndex++)
            {
                bool consumed = RunnerWeaponRules.TryConsumeShot(
                    ref ammunition,
                    ref shotSequence);

                Assert.That(consumed, Is.True);
                Assert.That(shotSequence, Is.EqualTo(shotIndex + 1));
            }

            Assert.That(ammunition, Is.Zero);
            Assert.That(shotSequence, Is.EqualTo(30));

            bool consumedEmpty = RunnerWeaponRules.TryConsumeShot(
                ref ammunition,
                ref shotSequence);

            Assert.That(consumedEmpty, Is.False);
            Assert.That(shotSequence, Is.EqualTo(30));
        }

        [Test]
        public void AlternatingHand_UsesLeftThenRightWithoutChangingSequence()
        {
            Assert.That(
                RunnerWeaponRules.GetAlternatingHand(1),
                Is.EqualTo(RunnerWeaponHand.Left));
            Assert.That(
                RunnerWeaponRules.GetAlternatingHand(2),
                Is.EqualTo(RunnerWeaponHand.Right));
            Assert.That(
                RunnerWeaponRules.GetAlternatingHand(31),
                Is.EqualTo(RunnerWeaponHand.Left));
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
        public void IncrementalReload_LoadsOneRoundAndCanBeInterruptedAfterFirstShell()
        {
            int ammunition = 0;

            Assert.That(
                RunnerWeaponRules.ShouldContinueIncrementalReload(ammunition, 5),
                Is.True);
            Assert.That(
                RunnerWeaponRules.CanInterruptReloadToFire(ammunition, true),
                Is.False);

            ammunition = RunnerWeaponRules.LoadNextRound(ammunition, 5);

            Assert.That(ammunition, Is.EqualTo(1));
            Assert.That(
                RunnerWeaponRules.ShouldContinueIncrementalReload(ammunition, 5),
                Is.True);
            Assert.That(
                RunnerWeaponRules.CanInterruptReloadToFire(ammunition, true),
                Is.True);

            for (int shellIndex = 1; shellIndex < 5; shellIndex++)
                ammunition = RunnerWeaponRules.LoadNextRound(ammunition, 5);

            Assert.That(ammunition, Is.EqualTo(5));
            Assert.That(
                RunnerWeaponRules.ShouldContinueIncrementalReload(ammunition, 5),
                Is.False);
            Assert.That(RunnerWeaponRules.LoadNextRound(ammunition, 5), Is.EqualTo(5));
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
                ref shotSequence);

            Assert.That(consumed, Is.True);
            Assert.That(
                RunnerWeaponRules.GetAlternatingHand(shotSequence),
                Is.EqualTo(RunnerWeaponHand.Right));
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
            Assert.That(
                RunnerWeaponRules.GetShotInterval(8f, 1f),
                Is.EqualTo(0.125f).Within(0.0001f));
            Assert.That(
                RunnerWeaponRules.GetReloadDuration(3f, 1f),
                Is.EqualTo(3f).Within(0.0001f));
        }

        [Test]
        public void RunningSpread_UsesBoundedTriangularSamplesAndStopsWhenNotRunning()
        {
            Assert.That(
                RunnerWeaponRules.GetRunningSpreadDegrees(true, 0f, 0f, 8f),
                Is.EqualTo(-8f));
            Assert.That(
                RunnerWeaponRules.GetRunningSpreadDegrees(true, 1f, 1f, 8f),
                Is.EqualTo(8f));
            Assert.That(
                RunnerWeaponRules.GetRunningSpreadDegrees(true, 0f, 1f, 8f),
                Is.Zero);
            Assert.That(
                RunnerWeaponRules.GetRunningSpreadDegrees(false, 1f, 1f, 8f),
                Is.Zero);
            Assert.That(
                RunnerWeaponRules.GetRunningSpreadDegrees(true, -1f, 2f, 8f),
                Is.Zero);
        }
    }
}
