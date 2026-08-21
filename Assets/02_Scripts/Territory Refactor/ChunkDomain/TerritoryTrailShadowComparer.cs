using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public static class TerritoryTrailShadowComparer
    {
        public static TerritoryTrailShadowComparison Compare(
            IReadOnlyList<FixedTerritoryPoint> legacyPoints,
            IReadOnlyList<TerritoryTrailSample> shadowSamples)
        {
            if (legacyPoints == null)
                throw new ArgumentNullException(nameof(legacyPoints));
            if (shadowSamples == null)
                throw new ArgumentNullException(nameof(shadowSamples));

            int sharedCount = Math.Min(legacyPoints.Count, shadowSamples.Count);
            for (int index = 0; index < sharedCount; index++)
            {
                if (legacyPoints[index] != shadowSamples[index].Point)
                {
                    return new TerritoryTrailShadowComparison(
                        false,
                        legacyPoints.Count,
                        shadowSamples.Count,
                        index);
                }
            }

            bool isMatch = legacyPoints.Count == shadowSamples.Count;
            return new TerritoryTrailShadowComparison(
                isMatch,
                legacyPoints.Count,
                shadowSamples.Count,
                isMatch ? -1 : sharedCount);
        }
    }
}
