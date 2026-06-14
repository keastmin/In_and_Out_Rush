using System;
using System.Collections.Generic;

public class RunnerItemInventory
{
    public const int SlotCount = 5;

    private readonly RunnerItemSlot[] _slots;

    private readonly RunnerItemConsumer _consumer;

    public RunnerItemInventory(
        RunnerItemConsumer consumer,
        IReadOnlyList<RunnerItemDefinition> definitions)
    {
        _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
        _slots = CreateSlots(definitions);
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

    private static RunnerItemSlot[] CreateSlots(IReadOnlyList<RunnerItemDefinition> definitions)
    {
        RunnerItemDefinition[] defaults = CreateDefaultDefinitions();
        var slots = new RunnerItemSlot[SlotCount];

        for (int slotIndex = 0; slotIndex < SlotCount; slotIndex++)
        {
            RunnerItemType itemType = (RunnerItemType)(slotIndex + 1);
            RunnerItemDefinition definition = FindDefinition(definitions, itemType, defaults[slotIndex]);
            slots[slotIndex] = new RunnerItemSlot(
                itemType,
                definition.InitialCount,
                definition.MaxCount);
        }

        return slots;
    }

    private static RunnerItemDefinition FindDefinition(
        IReadOnlyList<RunnerItemDefinition> definitions,
        RunnerItemType itemType,
        RunnerItemDefinition fallback)
    {
        if (definitions == null)
            return fallback;

        for (int i = 0; i < definitions.Count; i++)
        {
            if (definitions[i].ItemType == itemType)
                return definitions[i];
        }

        return fallback;
    }

    public static RunnerItemDefinition[] CreateDefaultDefinitions()
    {
        return new[]
        {
            new RunnerItemDefinition(RunnerItemType.Lifeline, 3, 3),
            new RunnerItemDefinition(RunnerItemType.Barrier, 3, 3),
            new RunnerItemDefinition(RunnerItemType.Incinerator, 1, 1),
            new RunnerItemDefinition(RunnerItemType.ElectricGrenade, 3, 3),
            new RunnerItemDefinition(RunnerItemType.BiodecompositionDevice, 2, 2),
        };
    }
}
