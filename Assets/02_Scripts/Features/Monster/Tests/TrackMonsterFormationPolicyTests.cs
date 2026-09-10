using NUnit.Framework;

namespace ProjectIO.Monsters.Tests
{
    public sealed class TrackMonsterFormationPolicyTests
    {
        [Test]
        public void NormalUnits_ExpandToSmallSmallSmallMediumLargeInOrder()
        {
            var expected = new[]
            {
                TrackMonsterNormalSize.Small,
                TrackMonsterNormalSize.Small,
                TrackMonsterNormalSize.Small,
                TrackMonsterNormalSize.Medium,
                TrackMonsterNormalSize.Large,
                TrackMonsterNormalSize.Small,
                TrackMonsterNormalSize.Small,
                TrackMonsterNormalSize.Small,
                TrackMonsterNormalSize.Medium,
                TrackMonsterNormalSize.Large
            };

            var actual = new TrackMonsterNormalSize[10];
            for (int i = 0; i < actual.Length; i++)
            {
                actual[i] = TrackMonsterFormationPolicy.GetNormalSize(i);
            }

            CollectionAssert.AreEqual(expected, actual);
            Assert.That(
                TrackMonsterFormationPolicy.GetSpawnCount(TrackMonsterSpawnType.Normal, 2),
                Is.EqualTo(10));
        }

        [TestCase(TrackMonsterSpawnType.ElitePredator)]
        [TestCase(TrackMonsterSpawnType.EliteWalker)]
        [TestCase(TrackMonsterSpawnType.Boss)]
        public void SpecialUnit_SpawnsOneMonster(TrackMonsterSpawnType spawnType)
        {
            Assert.That(TrackMonsterFormationPolicy.GetSpawnCount(spawnType, 3), Is.EqualTo(3));
        }

        [TestCase(false, false, 1f)]
        [TestCase(true, false, 1.5f)]
        [TestCase(false, true, 1.5f)]
        [TestCase(true, true, 2.25f)]
        public void MovementMultipliers_ComposeMultiplicatively(
            bool isOutsideTerritory,
            bool hasPermanentBoost,
            float expected)
        {
            float actual = TrackMonsterFormationPolicy.GetCombinedMovementMultiplier(
                isOutsideTerritory,
                hasPermanentBoost,
                strengthenCount: 0,
                strengthenMultiplier: 1.2f);

            Assert.That(actual, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void ExistingStrengthening_StacksWithNewMovementMultipliers()
        {
            float actual = TrackMonsterFormationPolicy.GetCombinedMovementMultiplier(
                isOutsideTerritory: true,
                hasPermanentBoost: true,
                strengthenCount: 2,
                strengthenMultiplier: 1.2f);

            Assert.That(actual, Is.EqualTo(2.25f * 1.2f * 1.2f).Within(0.0001f));
        }
    }
}
