using System.Collections.Generic;
using UnityEngine;

public class RunnerItemConsumer
{
    private readonly Dictionary<RunnerItemType, IItemConsumptionStrategy> _consumptionStrategies = new();

    public RunnerItemConsumer(BarrierWave barrierPrefab, Transform barrierMuzzle)
    {
        _consumptionStrategies[RunnerItemType.Barrier] =
            new SpawnBarrierStrategy(barrierPrefab, barrierMuzzle);
    }

    public void Use(RunnerItemType itemType, object parameters = null)
    {
        if (!_consumptionStrategies.TryGetValue(itemType, out IItemConsumptionStrategy strategy))
            throw new KeyNotFoundException($"No consumption strategy found for item type: {itemType}");

        strategy.TryUse(parameters);
    }

    public bool TryUse(RunnerItemType itemType, object parameters = null)
    {
        return _consumptionStrategies.TryGetValue(itemType, out IItemConsumptionStrategy strategy)
            && strategy.TryUse(parameters);
    }
}
