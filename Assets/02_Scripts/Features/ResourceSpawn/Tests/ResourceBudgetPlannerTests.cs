using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectIO.ResourceSpawn.Tests
{
    public sealed class ResourceBudgetPlannerTests
    {
        [Test]
        public void CreatePlan_FillsExactBudgetWhenCombinationExists()
        {
            IReadOnlyList<int> plan = ResourceBudgetPlanner.CreatePlan(
                7,
                new[] { 4, 3 },
                _ => 0);

            Assert.That(GetTotalAmount(plan, new[] { 4, 3 }), Is.EqualTo(7));
        }

        [Test]
        public void CreatePlan_UsesLargestFillableBudgetWhenExactBudgetDoesNotExist()
        {
            IReadOnlyList<int> plan = ResourceBudgetPlanner.CreatePlan(
                5,
                new[] { 3 },
                _ => 0);

            Assert.That(GetTotalAmount(plan, new[] { 3 }), Is.EqualTo(3));
        }

        [Test]
        public void CreatePlan_RejectsSelectorIndexOutsideCandidateRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ResourceBudgetPlanner.CreatePlan(
                    4,
                    new[] { 2 },
                    candidateCount => candidateCount));
        }

        private static int GetTotalAmount(
            IReadOnlyList<int> selectedOptionIndexes,
            IReadOnlyList<int> optionAmounts)
        {
            int totalAmount = 0;
            for (int i = 0; i < selectedOptionIndexes.Count; i++)
                totalAmount += optionAmounts[selectedOptionIndexes[i]];

            return totalAmount;
        }
    }
}
