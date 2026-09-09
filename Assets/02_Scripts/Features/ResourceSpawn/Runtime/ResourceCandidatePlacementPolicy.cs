using System.Collections.Generic;

namespace ProjectIO.ResourceSpawn
{
    public static class ResourceCandidatePlacementPolicy
    {
        public readonly struct PlacedResource
        {
            public float X { get; }
            public float Z { get; }
            public float MinDistance { get; }

            public PlacedResource(float x, float z, float minDistance)
            {
                X = x;
                Z = z;
                MinDistance = minDistance;
            }
        }

        public static bool IsCandidateValid(
            float candidateX,
            float candidateZ,
            float candidateMinDistance,
            float obstacleClearance,
            IReadOnlyList<float> obstacleDistancesSquared,
            IReadOnlyList<PlacedResource> placedResources,
            IReadOnlyList<PlacedResource> zonePlacedResources)
        {
            if (!IsFarEnoughFromObstacles(obstacleClearance, obstacleDistancesSquared))
                return false;

            if (!IsFarEnoughFromResources(
                    candidateX,
                    candidateZ,
                    candidateMinDistance,
                    placedResources))
            {
                return false;
            }

            return IsFarEnoughFromResources(
                candidateX,
                candidateZ,
                candidateMinDistance,
                zonePlacedResources);
        }

        private static bool IsFarEnoughFromObstacles(
            float clearance,
            IReadOnlyList<float> obstacleDistancesSquared)
        {
            if (obstacleDistancesSquared == null)
                return true;

            float requiredDistance = clearance > 0f ? clearance : 0f;
            float requiredDistanceSquared = requiredDistance * requiredDistance;
            for (int i = 0; i < obstacleDistancesSquared.Count; i++)
            {
                if (obstacleDistancesSquared[i] <= requiredDistanceSquared)
                    return false;
            }

            return true;
        }

        private static bool IsFarEnoughFromResources(
            float candidateX,
            float candidateZ,
            float candidateMinDistance,
            IReadOnlyList<PlacedResource> placedResources)
        {
            if (placedResources == null)
                return true;

            float safeCandidateMinDistance = candidateMinDistance > 0f
                ? candidateMinDistance
                : 0f;

            for (int i = 0; i < placedResources.Count; i++)
            {
                PlacedResource placedResource = placedResources[i];
                float placedMinDistance = placedResource.MinDistance > 0f
                    ? placedResource.MinDistance
                    : 0f;
                float requiredDistance = safeCandidateMinDistance > placedMinDistance
                    ? safeCandidateMinDistance
                    : placedMinDistance;
                float offsetX = candidateX - placedResource.X;
                float offsetZ = candidateZ - placedResource.Z;
                float distanceSquared = offsetX * offsetX + offsetZ * offsetZ;

                if (distanceSquared < requiredDistance * requiredDistance)
                    return false;
            }

            return true;
        }
    }
}
