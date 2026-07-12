using Fusion;
using System;
using TMPro;
using UnityEngine;

namespace KIM.Dev
{
    public class ResourceSystem : NetworkBehaviour
    {
        public static ResourceSystem Instance;

        [Networked, OnChangedRender(nameof(OnChangedMineralCount))] public int Mineral { get; set; }
        [Networked, OnChangedRender(nameof(OnChangedGasCount))] public int Gas { get; set; }

        public event Action<int> OnChangedMineral; // 미네랄이 변경될 때 호출되는 Action
        public event Action<int> OnChangedGas; // 가스가 변경될 때 호출되는 Action

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            // 자신이 빌더라면 작동
            if (Input.GetKeyDown(KeyCode.Space) && NetworkManager.Instance.Registry.RefToPosition[Runner.LocalPlayer] == PlayerPosition.Builder)
            {
                RPC_GetMineral(50);
                RPC_GetGas(50);
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_GetMineral(int mineral)
        {
            Mineral += mineral;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_GetGas(int gas)
        {
            Gas += gas;
        }

        public void OnChangedMineralCount()
        {
            OnChangedMineral?.Invoke(Mineral);
        }

        public void OnChangedGasCount()
        {
            OnChangedGas?.Invoke(Gas);
        }

        /// <summary>
        /// 자원이 충분한지 확인하는 함수
        /// </summary>
        /// <param name="cost">비용</param>
        /// <returns>자원이 충분하면 true, 아니면 false를 반환</returns>
        public bool IsResourceSufficient(Cost cost)
        {
            return (Mineral >= cost.Mineral && Gas >= cost.Gas);
        }

        /// <summary>
        /// 자원을 차감하는 함수
        /// </summary>
        /// <param name="cost">차감할 자원</param>
        public void DeductCost(Cost cost)
        {
            RPC_DeductCost(cost);
        }

        /// <summary>
        /// RPC로 자원 차감을 호스트에게 요청
        /// </summary>
        /// <param name="cost">차감할 자원</param>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_DeductCost(Cost cost)
        {
            if (HasStateAuthority)
            {
                Mineral = Mathf.Max(0, Mineral - cost.Mineral);
                Gas = Mathf.Max(0, Gas - cost.Gas);
            }
        }
    }

}