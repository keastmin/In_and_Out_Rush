public struct RunnerItemSlot
{
    public RunnerItemSlot(RunnerItemType itemType, int count, int maxCount)
    {
        ItemType = itemType;
        Count = count;
        MaxCount = maxCount;
    }

    public RunnerItemType ItemType { get; }
    public int Count { get; private set; }
    public int MaxCount { get; }
    public bool HasItem => ItemType != RunnerItemType.None && Count > 0;

    public bool TryAdd(int amount)
    {
        if (amount <= 0 || Count >= MaxCount)
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
