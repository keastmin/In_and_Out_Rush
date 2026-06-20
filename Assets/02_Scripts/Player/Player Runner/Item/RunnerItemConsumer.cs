using System;
using System.Collections.Generic;

public sealed class RunnerItemConsumer : IRunnerItemConsumer
{
    private readonly Dictionary<RunnerItemType, IItemConsumptionStrategy> _consumptionStrategies = new();

    public RunnerItemConsumer(IEnumerable<IItemConsumptionStrategy> strategies)
    {
        if (strategies == null)
            throw new ArgumentNullException(nameof(strategies));

        foreach (IItemConsumptionStrategy strategy in strategies)
        {
            if (strategy == null)
                continue;

            if (!_consumptionStrategies.TryAdd(strategy.ItemType, strategy))
                throw new ArgumentException(
                    $"A consumption strategy for {strategy.ItemType} is already registered.",
                    nameof(strategies));
        }
    }

    public bool TryUse(RunnerItemType itemType, RunnerItemUseContext context)
    {
        return _consumptionStrategies.TryGetValue(itemType, out IItemConsumptionStrategy strategy)
            && strategy.TryUse(context);
    }
}
