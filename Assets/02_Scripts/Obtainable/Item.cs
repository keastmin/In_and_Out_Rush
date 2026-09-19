public class Item : IObtainable
{
    public Item(RunnerItemType itemType) => ItemType = itemType;
    public RunnerItemType ItemType { get; }
}
