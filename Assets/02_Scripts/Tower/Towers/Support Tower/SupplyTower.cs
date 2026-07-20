using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public class SupplyTower : SupportTower, IRunnerInteractableTower
    {
        private const int MaxSlots = 10;

        [Networked, Capacity(MaxSlots), OnChangedRender(nameof(OnSuppliesChanged))]
        private NetworkLinkedList<int> Supplies => default;

        protected override void TowerAwake()
        {
            SetId(TowerIDContainer.SUPPLY_TOWER_ID);
        }

        /// <summary>
        /// 러너에게 보급물자를 전달하는 인터페이스 함수
        /// </summary>
        /// <param name="runner">전달 받을 플레이어 러너</param>
        public void Interact(PlayerRunner runner)
        {
            if (runner == null || !IsSpawnedInSimulation())
                return;

            if (Supplies.Count == 0)
                return;

            var supplyManager = SupplyTowerManager.Instance;
            if (supplyManager == null)
                return;

            foreach (var suppliesNum in Supplies)
            {
                if (supplyManager.NumToSupplies.TryGetValue(suppliesNum, out var supply))
                {
                    runner.Supply(supply());
                }
            }

            RPC_RunnerGetSupplies();
        }

        public bool TryLoadSupplies(int[] supplyArray)
        {
            if (!IsSpawnedInSimulation() || !HasStateAuthority || supplyArray == null || supplyArray.Length == 0)
                return false;

            Supplies.Clear();

            var supplyManager = SupplyTowerManager.Instance;
            for (int i = 0; i < supplyArray.Length && Supplies.Count < MaxSlots; i++)
            {
                int supplyNum = supplyArray[i];
                if (supplyManager != null && !supplyManager.NumToSupplies.ContainsKey(supplyNum))
                {
                    continue;
                }

                Supplies.Add(supplyNum);
            }

            return Supplies.Count > 0;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RunnerGetSupplies()
        {
            if (!IsSpawnedInSimulation() || !HasStateAuthority)
                return;

            Supplies.Clear();
            DespawnIfEmpty();
        }

        private void OnSuppliesChanged()
        {
            DespawnIfEmpty();
        }

        private void DespawnIfEmpty()
        {
            if (!IsSpawnedInSimulation() || !HasStateAuthority || Supplies.Count > 0)
                return;

            ReleaseGridOccupation();
            Runner.Despawn(Object);
        }

        private bool IsSpawnedInSimulation()
        {
            return Object != null && Object.IsValid && Object.IsInSimulation;
        }
    }
}