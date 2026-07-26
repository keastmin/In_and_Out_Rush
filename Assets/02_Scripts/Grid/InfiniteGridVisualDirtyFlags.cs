using System;

namespace KIM.Dev
{
    [Flags]
    public enum InfiniteGridVisualDirtyFlags
    {
        None = 0,
        Settings = 1 << 0,
        BaseState = 1 << 1,
        Preview = 1 << 2,
        Buff = 1 << 3,
        Territory = 1 << 4,
        Occupancy = 1 << 5,
        All = Settings | BaseState | Preview | Buff | Territory | Occupancy
    }
}
