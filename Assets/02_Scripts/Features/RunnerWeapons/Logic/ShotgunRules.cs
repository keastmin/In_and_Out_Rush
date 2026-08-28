using System;

namespace ProjectIO.RunnerWeapons
{
    public static class ShotgunRules
    {
        public static float GetBaseDamage(
            float distance,
            float nearRange,
            float middleRange,
            float maximumRange,
            float nearDamage,
            float middleDamage,
            float farDamage)
        {
            if (distance < 0f || distance > Math.Max(0f, maximumRange))
                return 0f;

            if (distance <= Math.Max(0f, nearRange))
                return Math.Max(0f, nearDamage);

            if (distance <= Math.Max(nearRange, middleRange))
                return Math.Max(0f, middleDamage);

            return Math.Max(0f, farDamage);
        }

        public static bool IsInsideCone(
            float forwardX,
            float forwardY,
            float offsetX,
            float offsetY,
            float maximumRange,
            float fullConeDegrees)
        {
            double forwardLengthSquared = forwardX * forwardX + forwardY * forwardY;
            double offsetLengthSquared = offsetX * offsetX + offsetY * offsetY;
            double safeRange = Math.Max(0f, maximumRange);
            if (forwardLengthSquared <= double.Epsilon ||
                offsetLengthSquared <= double.Epsilon ||
                offsetLengthSquared > safeRange * safeRange)
            {
                return false;
            }

            double dot = forwardX * offsetX + forwardY * offsetY;
            double denominator = Math.Sqrt(forwardLengthSquared * offsetLengthSquared);
            double cosine = Math.Max(-1d, Math.Min(1d, dot / denominator));
            double halfAngleRadians = Math.Max(0f, fullConeDegrees) * 0.5d * Math.PI / 180d;
            return cosine + 0.000001d >= Math.Cos(halfAngleRadians);
        }

        public static bool IsTargetHit(
            bool isRunning,
            float randomSample,
            float runningAccuracy)
        {
            if (!isRunning)
                return true;

            return Clamp01(randomSample) < Clamp01(runningAccuracy);
        }

        public static float GetPelletAngleDegrees(
            int seed,
            int shotSequence,
            int pelletIndex,
            float fullConeDegrees)
        {
            uint hash = unchecked((uint)seed);
            hash ^= unchecked((uint)shotSequence) * 0x9E3779B9u;
            hash ^= unchecked((uint)pelletIndex + 1u) * 0x85EBCA6Bu;
            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            hash *= 0x846CA68Bu;
            hash ^= hash >> 16;

            float sample = (hash & 0x00FFFFFFu) / 16777216f;
            float halfAngle = Math.Max(0f, fullConeDegrees) * 0.5f;
            return (sample * 2f - 1f) * halfAngle;
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }
    }
}
