using System;

namespace KIM.Dev
{
    [Flags]
    public enum TowerCapability
    {
        None = 0,
        Move = 1 << 0,
        Sell = 1 << 1,
        AssignProperty = 1 << 2,
        IndividualUpgrade = 1 << 3
    }
}
