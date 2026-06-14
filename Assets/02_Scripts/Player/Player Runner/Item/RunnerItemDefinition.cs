using System;

[Serializable]
public struct RunnerItemDefinition
{
    public RunnerItemDefinition(RunnerItemType itemType, int initialCount, int maxCount)
    {
        ItemType = itemType;
        InitialCount = initialCount;
        MaxCount = maxCount;
    }

    public RunnerItemType ItemType;
    public int InitialCount;
    public int MaxCount;
}
