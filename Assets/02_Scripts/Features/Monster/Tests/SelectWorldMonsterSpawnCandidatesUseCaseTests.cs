using System.Collections.Generic;
using NUnit.Framework;
using ProjectIO.Monsters.UseCases;

namespace ProjectIO.Monsters.Tests
{
    public sealed class SelectWorldMonsterSpawnCandidatesUseCaseTests
    {
        private readonly SelectWorldMonsterSpawnCandidatesUseCase _useCase = new();
        private readonly List<int> _selectedIndexes = new();

        [Test]
        public void Execute_SelectsEligibleCandidatesInSourceOrderUpToLimit()
        {
            var candidates = new[]
            {
                CreateEligibleCandidate(10),
                CreateEligibleCandidate(11),
                CreateEligibleCandidate(12)
            };

            _useCase.Execute(candidates, 2, _selectedIndexes);

            CollectionAssert.AreEqual(new[] { 0, 1 }, _selectedIndexes);
        }

        [Test]
        public void Execute_ExcludesEveryIneligibleCandidateState()
        {
            var candidates = new[]
            {
                new WorldMonsterSpawnCandidate(1, true, false, false, true),
                new WorldMonsterSpawnCandidate(2, false, true, false, true),
                new WorldMonsterSpawnCandidate(3, false, false, true, true),
                new WorldMonsterSpawnCandidate(4, false, false, false, false),
                CreateEligibleCandidate(5)
            };

            _useCase.Execute(candidates, 5, _selectedIndexes);

            CollectionAssert.AreEqual(new[] { 4 }, _selectedIndexes);
        }

        [Test]
        public void Execute_SelectsSameRecordIdOnlyOncePerRefresh()
        {
            var candidates = new[]
            {
                CreateEligibleCandidate(7),
                CreateEligibleCandidate(7),
                CreateEligibleCandidate(8)
            };

            _useCase.Execute(candidates, 3, _selectedIndexes);

            CollectionAssert.AreEqual(new[] { 0, 2 }, _selectedIndexes);
        }

        [Test]
        public void Execute_ZeroLimitClearsPreviousSelection()
        {
            _selectedIndexes.Add(99);

            _useCase.Execute(new[] { CreateEligibleCandidate(1) }, 0, _selectedIndexes);

            Assert.That(_selectedIndexes, Is.Empty);
        }

        [Test]
        public void CandidatePolicy_RequiresEverySpawnCondition()
        {
            Assert.That(
                WorldMonsterSpawnCandidatePolicy.CanSpawn(CreateEligibleCandidate(1)),
                Is.True);
            Assert.That(
                WorldMonsterSpawnCandidatePolicy.CanSpawn(
                    new WorldMonsterSpawnCandidate(1, false, false, true, true)),
                Is.False);
        }

        private static WorldMonsterSpawnCandidate CreateEligibleCandidate(int recordId)
        {
            return new WorldMonsterSpawnCandidate(
                recordId,
                isDestroyed: false,
                hasActiveMonster: false,
                isInsideTerritory: false,
                isWithinActiveChunkRange: true);
        }
    }
}
