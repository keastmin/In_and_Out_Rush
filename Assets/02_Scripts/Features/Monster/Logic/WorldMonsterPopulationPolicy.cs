using System;

namespace ProjectIO.Monsters
{
    public sealed class WorldMonsterPopulationPolicy
    {
        private bool hasCulled;
        private bool hasReducedHealth;

        public bool TryBeginCull(float elapsedTime, float triggerTime = 900f)
        {
            if (hasCulled || !(elapsedTime >= Math.Max(0f, triggerTime)))
                return false;

            hasCulled = true;
            return true;
        }

        public bool TryBeginHealthReduction(float elapsedTime, float triggerTime = 1500f)
        {
            if (hasReducedHealth || !(elapsedTime >= Math.Max(0f, triggerTime)))
                return false;

            hasReducedHealth = true;
            return true;
        }

        public void Reset()
        {
            hasCulled = false;
            hasReducedHealth = false;
        }

        public static float SampleNormalizedRadius(float sample)
        {
            if (sample <= 0f) return 0f;
            if (sample >= 1f) return 1f;

            // Area density 1 + 2r gives radial CDF (3r^2 + 4r^3) / 7.
            // Invert it without rejection, preserving the placement attempt budget.
            double lower = 0;
            double upper = 1;
            for (int i = 0; i < 24; i++)
            {
                double radius = (lower + upper) * 0.5;
                double cumulative = radius * radius * (3 + 4 * radius) / 7;
                if (cumulative < sample)
                    lower = radius;
                else
                    upper = radius;
            }

            return (float)((lower + upper) * 0.5);
        }

        public static float GetCullProbability(float distance, float spawnRadius)
        {
            float radius = spawnRadius > 0f
                ? Math.Max(0f, Math.Min(1f, distance / spawnRadius))
                : 0f;
            return 1f - 1f / (1f + 2f * radius);
        }

        public static float ReduceCurrentHealth(float currentHealth)
            => Math.Max(0f, currentHealth) * 0.25f;
    }
}
