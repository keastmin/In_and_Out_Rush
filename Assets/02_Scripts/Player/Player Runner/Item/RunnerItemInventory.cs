using System;

public class RunnerItemInventory
{
    public const int SlotCount = 5;

    private readonly RunnerItemSlot[] _slots =
    {
        new RunnerItemSlot(RunnerItemType.Lifeline, 3, 3),
        new RunnerItemSlot(RunnerItemType.Barrier, 3, 3),
        new RunnerItemSlot(RunnerItemType.Incinerator, 1, 1),
        new RunnerItemSlot(RunnerItemType.ElectricGrenade, 3, 3),
        new RunnerItemSlot(RunnerItemType.BiodecompositionDevice, 2, 2),
    };

    private readonly RunnerItemConsumer _consumer;

    public RunnerItemInventory(RunnerItemConsumer consumer)
    {
        _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
    }

    public RunnerItemSlot GetSlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return default(RunnerItemSlot);

        return _slots[slotIndex];
    }

    public bool TryUse(int slotIndex, object parameters = null)
    {
        if (!IsValidSlotIndex(slotIndex))
            return false;

        RunnerItemSlot slot = _slots[slotIndex];
        if (!slot.HasItem)
            return false;

        if (slot.ItemType == RunnerItemType.Lifeline)
            return false;

        if (!_consumer.TryUse(slot.ItemType, parameters))
            return false;

        slot.TryConsume();
        _slots[slotIndex] = slot;
        return true;
    }

    public bool TryConsume(RunnerItemType itemType)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            RunnerItemSlot slot = _slots[i];
            if (slot.ItemType != itemType)
                continue;

            if (!slot.TryConsume())
                return false;

            _slots[i] = slot;
            return true;
        }

        return false;
    }

    public bool TryAdd(int slotIndex, int amount)
    {
        if (!IsValidSlotIndex(slotIndex))
            return false;

        RunnerItemSlot slot = _slots[slotIndex];
        bool added = slot.TryAdd(amount);
        _slots[slotIndex] = slot;
        return added;
    }

    private static bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < SlotCount;
    }
}
