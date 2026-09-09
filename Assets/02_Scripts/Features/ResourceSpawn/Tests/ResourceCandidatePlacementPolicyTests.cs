using System;
using NUnit.Framework;
using PlacedResource = ProjectIO.ResourceSpawn.ResourceCandidatePlacementPolicy.PlacedResource;

namespace ProjectIO.ResourceSpawn.Tests
{
    public sealed class ResourceCandidatePlacementPolicyTests
    {
        [Test]
        public void IsCandidateValid_RejectsObstacleAtClearanceBoundary()
        {
            bool isValid = ResourceCandidatePlacementPolicy.IsCandidateValid(
                0f,
                0f,
                1f,
                3f,
                new[] { 9f },
                Array.Empty<PlacedResource>(),
                Array.Empty<PlacedResource>());

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void IsCandidateValid_AcceptsObstacleBeyondClearanceBoundary()
        {
            bool isValid = ResourceCandidatePlacementPolicy.IsCandidateValid(
                0f,
                0f,
                1f,
                3f,
                new[] { 9.01f },
                Array.Empty<PlacedResource>(),
                Array.Empty<PlacedResource>());

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void IsCandidateValid_UsesLargerResourceMinimumDistance()
        {
            bool isValid = ResourceCandidatePlacementPolicy.IsCandidateValid(
                0f,
                0f,
                2f,
                0f,
                Array.Empty<float>(),
                new[] { new PlacedResource(3f, 3f, 5f) },
                Array.Empty<PlacedResource>());

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void IsCandidateValid_AcceptsResourceAtMinimumDistanceBoundary()
        {
            bool isValid = ResourceCandidatePlacementPolicy.IsCandidateValid(
                0f,
                0f,
                2f,
                0f,
                Array.Empty<float>(),
                new[] { new PlacedResource(3f, 4f, 5f) },
                Array.Empty<PlacedResource>());

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void IsCandidateValid_ChecksCommittedAndCurrentZoneResources()
        {
            var nearbyResource = new PlacedResource(1f, 0f, 2f);

            bool isValidWithCommittedResource = ResourceCandidatePlacementPolicy.IsCandidateValid(
                0f,
                0f,
                2f,
                0f,
                Array.Empty<float>(),
                new[] { nearbyResource },
                Array.Empty<PlacedResource>());
            bool isValidWithCurrentZoneResource = ResourceCandidatePlacementPolicy.IsCandidateValid(
                0f,
                0f,
                2f,
                0f,
                Array.Empty<float>(),
                Array.Empty<PlacedResource>(),
                new[] { nearbyResource });

            Assert.That(isValidWithCommittedResource, Is.False);
            Assert.That(isValidWithCurrentZoneResource, Is.False);
        }
    }
}
