using System;
using ProjectIO.RunnerSupply;
using UnityEngine;

namespace KIM.Dev
{
    public class SupplyTowerManager : MonoBehaviour
    {
        public static SupplyTowerManager Instance { get; private set; }
        public RunnerSupplyNetwork Network { get; private set; }
        public int PendingSupplyCount => Network != null ? Network.Count : 0;
        public bool HasPendingSupplies => PendingSupplyCount > 0;
        public const int SKILL_SUPPLY_NUM = RunnerSupplyRules.SkillId;
        public const int WEAPON_SUPPLY_NUM = RunnerSupplyRules.WeaponId;

        private void Awake() => Instance = this;
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Initialize(RunnerSupplyNetwork network) => Network = network;
        public int[] PeekSupplyNumArray() => Network != null ? Network.Snapshot() : Array.Empty<int>();
        public bool MatchesPendingSupplies(int[] expected) => Network != null && Network.MatchesPending(expected);
        public bool TryConsumePendingSupplies(int[] expected) => Network != null && Network.ConsumeLoadedSupplies(expected);
    }
}
