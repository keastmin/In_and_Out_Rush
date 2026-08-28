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
            ref int shotSequence,
            out RunnerWeaponHand hand)
        {
            if (ammunition <= 0)
            {
                hand = GetHand(shotSequence + 1);
                return false;
            }

            ammunition--;
            shotSequence++;
            hand = GetHand(shotSequence);
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

        public static RunnerWeaponHand GetHand(int shotSequence)
        {
            return (shotSequence & 1) == 1
                ? RunnerWeaponHand.Left
                : RunnerWeaponHand.Right;
        }
    }
}
