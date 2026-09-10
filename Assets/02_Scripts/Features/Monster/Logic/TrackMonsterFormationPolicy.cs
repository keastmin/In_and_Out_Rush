using System;

namespace ProjectIO.Monsters
{
    public static class TrackMonsterFormationPolicy
    {
        public const int NormalMonstersPerUnit = 5;

        public static int GetSpawnCount(TrackMonsterSpawnType spawnType, int spawnUnitCount)
        {
            int safeUnitCount = Math.Max(0, spawnUnitCount);
            return spawnType == TrackMonsterSpawnType.Normal
                ? safeUnitCount * NormalMonstersPerUnit
                : safeUnitCount;
        }

        public static TrackMonsterNormalSize GetNormalSize(int indexInUnit)
        {
            int normalizedIndex = ((indexInUnit % NormalMonstersPerUnit) + NormalMonstersPerUnit) % NormalMonstersPerUnit;
            if (normalizedIndex < 3)
            {
                return TrackMonsterNormalSize.Small;
            }

            return normalizedIndex == 3
                ? TrackMonsterNormalSize.Medium
                : TrackMonsterNormalSize.Large;
        }

        public static float GetCombinedMovementMultiplier(
            bool isOutsideTerritory,
            bool hasPermanentBoost,
            int strengthenCount,
            float strengthenMultiplier)
        {
            float outsideMultiplier = isOutsideTerritory ? 1.5f : 1f;
            float permanentMultiplier = hasPermanentBoost ? 1.5f : 1f;
            float accumulatedStrength = (float)Math.Pow(
                Math.Max(0f, strengthenMultiplier),
                Math.Max(0, strengthenCount));
            return outsideMultiplier * permanentMultiplier * accumulatedStrength;
        }
    }
}
