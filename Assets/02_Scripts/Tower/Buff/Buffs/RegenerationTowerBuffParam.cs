using System;

namespace KIM.Dev
{
    [Serializable]
    public class RegenerationTowerBuffParam : IBuffParam
    {
        public float ShieldAmount;
        public float TotalHealCharge;
        public float RemainingHealCharge;
        public float HealPerSecond;
    }
}
