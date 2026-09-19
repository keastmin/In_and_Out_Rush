using System;
using KIM.Dev;
using UnityEngine;

namespace ProjectIO.RunnerSupply
{
    [Serializable]
    public sealed class RunnerSupplyDefinition
    {
        [Tooltip("Skill=1, Weapon=2, Item sheet IDs=7000..7004")]
        public int Id;
        public string Name;
        public Sprite Icon;
        public Cost Cost;
        public RunnerItemType ItemType;
        public TowerPropertiesType RequiredCenter;
        [TextArea] public string Description;
        public bool IsItem => ItemType != RunnerItemType.None;
    }
}
