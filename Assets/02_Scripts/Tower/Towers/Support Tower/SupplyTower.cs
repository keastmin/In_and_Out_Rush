using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public class SupplyTower : SupportTower, IRunnerInteractableTower
    {
        private const int _maxSlots = 10;

        [Networked, Capacity(_maxSlots), OnChangedRender(nameof(OnEmptySupplies))]
        private NetworkLinkedList<int> Supplies => default;

        public override void Spawned()
        {
            base.Spawned();

            // 빌더만 실행
            if (NetworkManager.Instance.Registry.RefToPosition[Runner.LocalPlayer] == PlayerPosition.Builder)
            {
                // 러너에게 빌더가 가지고 있는 보급품 목록 전달 - RPC로 호스트가 공유 변수를 초기화 하도록 함
                int[] supplyNumArray = SupplyTowerManager.Instance.GetSupplyNumArray();
                RPC_SetSuppliesList(supplyNumArray);

                // Laboratory UI의 보급 슬롯을 초기화하는 로직
                SupplyTowerManager.Instance.RevertLaboratorySupplySlotRevert();
            }
        }

        /// <summary>
        /// 러너에게 보급물자를 전달하는 인터페이스 함수
        /// </summary>
        /// <param name="runner">전달 받을 플레이어 러너</param>
        public void Interact(PlayerRunner runner)
        {
            foreach (var suppliesNum in Supplies)
            {
                if (SupplyTowerManager.Instance.NumToSupplies.TryGetValue(suppliesNum, out var supply))
                    runner.Supply(supply());
            }

            RPC_RunnerGetSupplies();
        }

        /// <summary>
        /// 호스트에게 보급품 공유변수 초기화를 요청하는 함수
        /// </summary>
        /// <param name="supplyArray">보급품 목록</param>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_SetSuppliesList(int[] supplyArray)
        {
            if (HasStateAuthority)
            {
                for (int i = 0; i < supplyArray.Length; i++)
                {
                    Supplies.Add(supplyArray[i]);
                }
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RunnerGetSupplies()
        {
            if (HasStateAuthority)
                Supplies.Clear();
        }

        private void OnEmptySupplies()
        {
            if (HasStateAuthority && Supplies.Count == 0)
            {
                Runner.Despawn(this.Object);
            }
        }
    }
}