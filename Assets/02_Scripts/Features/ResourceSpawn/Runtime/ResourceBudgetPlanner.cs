using System;
using System.Collections.Generic;

namespace ProjectIO.ResourceSpawn
{
    public static class ResourceBudgetPlanner
    {
        public static IReadOnlyList<int> CreatePlan(
            int budget,
            IReadOnlyList<int> optionAmounts,
            Func<int, int> selectCandidateIndex)
        {
            var selectedOptionIndexes = new List<int>();
            if (budget <= 0 || optionAmounts == null || optionAmounts.Count == 0)
                return selectedOptionIndexes;

            if (selectCandidateIndex == null)
                throw new ArgumentNullException(nameof(selectCandidateIndex));

            ValidateOptionAmounts(optionAmounts);

            bool[] fillableBudgets = CreateFillableBudgetTable(budget, optionAmounts);
            int remainingBudget = GetMaxFillableBudget(fillableBudgets);

            while (remainingBudget > 0)
            {
                List<int> candidateOptionIndexes = CollectCandidateOptionIndexes(
                    remainingBudget,
                    optionAmounts,
                    fillableBudgets);
                if (candidateOptionIndexes.Count == 0)
                    break;

                int selectedCandidateIndex = selectCandidateIndex(candidateOptionIndexes.Count);
                if (selectedCandidateIndex < 0 || selectedCandidateIndex >= candidateOptionIndexes.Count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(selectCandidateIndex),
                        selectedCandidateIndex,
                        "Candidate selector returned an index outside the candidate range.");
                }

                int selectedOptionIndex = candidateOptionIndexes[selectedCandidateIndex];
                selectedOptionIndexes.Add(selectedOptionIndex);
                remainingBudget -= optionAmounts[selectedOptionIndex];
            }

            return selectedOptionIndexes;
        }

        private static void ValidateOptionAmounts(IReadOnlyList<int> optionAmounts)
        {
            for (int i = 0; i < optionAmounts.Count; i++)
            {
                if (optionAmounts[i] <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(optionAmounts),
                        optionAmounts[i],
                        "Every resource option amount must be greater than zero.");
                }
            }
        }

        private static bool[] CreateFillableBudgetTable(
            int budget,
            IReadOnlyList<int> optionAmounts)
        {
            var fillableBudgets = new bool[budget + 1];
            fillableBudgets[0] = true;

            for (int value = 1; value <= budget; value++)
            {
                for (int i = 0; i < optionAmounts.Count; i++)
                {
                    int amount = optionAmounts[i];
                    if (value >= amount && fillableBudgets[value - amount])
                    {
                        fillableBudgets[value] = true;
                        break;
                    }
                }
            }

            return fillableBudgets;
        }

        private static int GetMaxFillableBudget(IReadOnlyList<bool> fillableBudgets)
        {
            for (int budget = fillableBudgets.Count - 1; budget >= 0; budget--)
            {
                if (fillableBudgets[budget])
                    return budget;
            }

            return 0;
        }

        private static List<int> CollectCandidateOptionIndexes(
            int remainingBudget,
            IReadOnlyList<int> optionAmounts,
            IReadOnlyList<bool> fillableBudgets)
        {
            var candidateOptionIndexes = new List<int>();
            for (int i = 0; i < optionAmounts.Count; i++)
            {
                int nextBudget = remainingBudget - optionAmounts[i];
                if (nextBudget >= 0 && fillableBudgets[nextBudget])
                    candidateOptionIndexes.Add(i);
            }

            return candidateOptionIndexes;
        }
    }
}
