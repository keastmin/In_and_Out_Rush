using System;

namespace ProjectIO.RunnerWeapons
{
    public static class RunnerWeaponRules
    {
        private const float MinimumSpeedScaler = 0.01f;

        public static bool CanStartReload(int ammunition, int magazineCapacity, bool isReloading)
        {
            return !isReloading &&
                   magazineCapacity > 0 &&
                   ammunition >= 0 &&
                   ammunition < magazineCapacity;
        }

        public static bool ShouldStartAutomaticReload(
            int ammunition,
            int magazineCapacity,
            bool isReloading)
        {
            return ammunition == 0 &&
                   CanStartReload(ammunition, magazineCapacity, isReloading);
        }

        public static bool TryConsumeShot(
            ref int ammunition,
            ref int shotSequence)
        {
            if (ammunition <= 0)
                return false;

            ammunition--;
            shotSequence++;
            return true;
        }

        public static int CompleteReload(int magazineCapacity)
        {
            return Math.Max(1, magazineCapacity);
        }

        public static float GetShotInterval(float shotsPerSecond, float attackSpeedScaler)
        {
            float safeShotsPerSecond = Math.Max(MinimumSpeedScaler, shotsPerSecond);
            float safeScaler = Math.Max(MinimumSpeedScaler, attackSpeedScaler);
            return 1f / (safeShotsPerSecond * safeScaler);
        }

        public static float GetReloadDuration(float baseDuration, float reloadSpeedScaler)
        {
            float safeDuration = Math.Max(0f, baseDuration);
            float safeScaler = Math.Max(MinimumSpeedScaler, reloadSpeedScaler);
            return safeDuration / safeScaler;
        }

        public static RunnerWeaponHand GetAlternatingHand(int shotSequence)
        {
            return (shotSequence & 1) == 1
                ? RunnerWeaponHand.Left
                : RunnerWeaponHand.Right;
        }

        public static float GetRunningSpreadDegrees(
            bool isRunning,
            float firstSample,
            float secondSample,
            float maximumSpreadDegrees)
        {
            if (!isRunning)
                return 0f;

            float safeMaximumSpread = Math.Max(0f, maximumSpreadDegrees);
            float centeredTriangularSample =
                Clamp01(firstSample) + Clamp01(secondSample) - 1f;
            return centeredTriangularSample * safeMaximumSpread;
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }
    }
}
