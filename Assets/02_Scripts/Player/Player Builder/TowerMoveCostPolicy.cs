using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class TowerMoveCostPolicy
    {
        private ResourceSystem _resourceSystem;

        public void Initialize(ResourceSystem resourceSystem)
        {
            _resourceSystem = resourceSystem;
        }

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

            return CanAccessResourceSystem(_resourceSystem) &&
                   _resourceSystem.IsResourceSufficient(cost);
        }

        public bool TryPay(Cost cost)
        {
            if (cost.Mineral <= 0 && cost.Gas <= 0)
                return true;

            if (!CanAccessResourceSystem(_resourceSystem) ||
                !_resourceSystem.Object.HasStateAuthority ||
                !_resourceSystem.IsResourceSufficient(cost))
            {
                return false;
            }

            _resourceSystem.Mineral -= cost.Mineral;
            _resourceSystem.Gas -= cost.Gas;
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
