using System;
using System.Collections.Generic;

namespace ProjectIO.Monsters.UseCases
{
    public sealed class SelectWorldMonsterSpawnCandidatesUseCase
    {
        private readonly HashSet<int> _selectedRecordIds = new();

        public void Execute(
            IReadOnlyList<WorldMonsterSpawnCandidate> candidates,
            int maxSelectionCount,
            List<int> selectedCandidateIndexes)
        {
            if (selectedCandidateIndexes == null)
                throw new ArgumentNullException(nameof(selectedCandidateIndexes));

            selectedCandidateIndexes.Clear();
            _selectedRecordIds.Clear();

            if (candidates == null || maxSelectionCount <= 0)
                return;

            for (int i = 0; i < candidates.Count; i++)
            {
                WorldMonsterSpawnCandidate candidate = candidates[i];
                if (!WorldMonsterSpawnCandidatePolicy.CanSpawn(candidate) ||
                    !_selectedRecordIds.Add(candidate.RecordId))
                {
                    continue;
                }

                selectedCandidateIndexes.Add(i);
                if (selectedCandidateIndexes.Count >= maxSelectionCount)
                    return;
            }
        }
    }
}
