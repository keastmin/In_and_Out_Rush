public struct RunnerItemSlot
{
    public RunnerItemSlot(RunnerItemType itemType, int count, int maxCount)
    {
        ItemType = itemType;
        MaxCount = maxCount < -1 ? 0 : maxCount;
        Count = System.Math.Max(0, count);

        if (MaxCount >= 0)
            Count = System.Math.Min(Count, MaxCount);
    }

    public RunnerItemType ItemType { get; }
    public int Count { get; private set; }
    public int MaxCount { get; }
    public bool HasItem => ItemType != RunnerItemType.None && Count > 0;
    public bool HasUnlimitedCapacity => MaxCount == -1;

    public bool TryAdd(int amount)
    {
        if (amount <= 0)
            return false;

        if (HasUnlimitedCapacity)
        {
            Count = amount > int.MaxValue - Count ? int.MaxValue : Count + amount;
            return true;
        }

        if (Count >= MaxCount)
            return false;

        Count = System.Math.Min(Count + amount, MaxCount);
        return true;
    }

    public bool TryConsume()
    {
        if (!HasItem)
            return false;

        Count--;
        return true;
    }
}
