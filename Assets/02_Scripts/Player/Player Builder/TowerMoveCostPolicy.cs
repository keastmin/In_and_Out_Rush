using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class TowerMoveCostPolicy
    {
        public Cost CalculateCost(
            IReadOnlyList<Tower> towers,
            InfiniteGrid gridManager,
            int mineralCostPerTower)
        {
            if (towers == null || gridManager == null)
                return default;

            int paidTowerCount = 0;
            for (int i = 0; i < towers.Count; i++)
            {
                Tower tower = towers[i];
                if (tower != null && !gridManager.HasFreeTrackRelocation(tower))
                    paidTowerCount++;
            }

            int mineralCost = paidTowerCount * Mathf.Max(0, mineralCostPerTower);
            return new Cost(mineralCost, 0);
        }

        public bool CanAfford(Cost cost)
        {
            if (cost.Mineral <= 0 && cost.Gas <= 0)
                return true;

            ResourceSystem resourceSystem = ResourceSystem.Instance;
            return CanAccessResourceSystem(resourceSystem) &&
                   resourceSystem.IsResourceSufficient(cost);
        }

        public bool TryPay(Cost cost)
        {
            if (cost.Mineral <= 0 && cost.Gas <= 0)
                return true;

            ResourceSystem resourceSystem = ResourceSystem.Instance;
            if (!CanAccessResourceSystem(resourceSystem) ||
                !resourceSystem.Object.HasStateAuthority ||
                !resourceSystem.IsResourceSufficient(cost))
            {
                return false;
            }

            resourceSystem.Mineral -= cost.Mineral;
            resourceSystem.Gas -= cost.Gas;
            return true;
        }

        private static bool CanAccessResourceSystem(ResourceSystem resourceSystem)
        {
            return resourceSystem != null &&
                   resourceSystem.Object != null &&
                   resourceSystem.Object.IsValid &&
                   resourceSystem.Object.IsInSimulation;
        }
    }
}
