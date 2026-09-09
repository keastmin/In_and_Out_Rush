namespace ProjectIO.Territory
{
    public readonly struct TerritoryTrailShadowComparison
    {
        public TerritoryTrailShadowComparison(
            bool isMatch,
            int legacyPointCount,
            int shadowSampleCount,
            int firstMismatchIndex)
        {
            IsMatch = isMatch;
            LegacyPointCount = legacyPointCount;
            ShadowSampleCount = shadowSampleCount;
            FirstMismatchIndex = firstMismatchIndex;
        }

        public bool IsMatch { get; }
        public int LegacyPointCount { get; }
        public int ShadowSampleCount { get; }
        public int FirstMismatchIndex { get; }
    }
}
