using Fusion;
using ProjectIO.RunnerSupply;
using UnityEngine;

namespace KIM.Dev
{
    public class SupplyTower : SupportTower, IRunnerInteractableTower
    {
        [Networked, Capacity(RunnerSupplyRules.Capacity)]
        private NetworkLinkedList<int> Supplies => default;

        protected override void TowerAwake() => SetId(TowerIDContainer.SUPPLY_TOWER_ID);

        public void Interact(PlayerRunner runner)
        {
            // F is already carried by the Runner's Fusion input to State Authority.
            if (!IsSpawnedInSimulation() || !HasStateAuthority || runner == null ||
                runner.Runner != Runner || !runner.HasStateAuthority || runner.IsDead)
                return;

            Collider targetCollider = GetComponent<Collider>();
            Vector3 origin = runner.transform.position + Vector3.up;
            Vector3 target = targetCollider != null ? targetCollider.ClosestPoint(origin) : transform.position;
            if ((target - origin).sqrMagnitude > 3.25f * 3.25f || Supplies.Count == 0)
                return;

            int received = 0;
            for (int i = 0; i < Supplies.Count;)
            {
                int productId = Supplies[i];
                if (runner.TryReceiveSupply(productId))
                {
                    Supplies.Remove(productId);
                    received++;
                }
                else i++;
            }

            runner.NotifySupplyResult(received, Supplies.Count);
            if (Supplies.Count == 0)
            {
                ReleaseGridOccupation();
                Runner.Despawn(Object);
            }
        }

        public bool TryLoadSupplies(int[] supplyArray)
        {
            if (!IsSpawnedInSimulation() || !HasStateAuthority || supplyArray == null ||
                supplyArray.Length == 0 || supplyArray.Length > RunnerSupplyRules.Capacity)
                return false;

            foreach (int id in supplyArray)
                if (id != RunnerSupplyRules.SkillId && id != RunnerSupplyRules.WeaponId &&
                    RunnerSupplyRules.GetItemType(id) == RunnerItemType.None)
                    return false;

            Supplies.Clear();
            foreach (int id in supplyArray) Supplies.Add(id);
            return true;
        }

        private bool IsSpawnedInSimulation() => Object != null && Object.IsValid && Object.IsInSimulation;
    }
}
