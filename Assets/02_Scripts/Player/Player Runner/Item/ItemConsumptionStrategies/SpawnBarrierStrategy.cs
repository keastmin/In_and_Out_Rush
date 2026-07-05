using UnityEngine;

public sealed class SpawnBarrierStrategy : IItemConsumptionStrategy
{
    private readonly BarrierWave _barrierPrefab;
    private readonly Transform _barrierMuzzle;

    public SpawnBarrierStrategy(BarrierWave barrierPrefab, Transform barrierMuzzle)
    {
        _barrierPrefab = barrierPrefab;
        _barrierMuzzle = barrierMuzzle;
    }

    public RunnerItemType ItemType => RunnerItemType.Barrier;

    public bool TryUse(RunnerItemUseContext context)
    {
        PlayerRunner playerRunner = context.User;
        if (playerRunner == null || !playerRunner.HasStateAuthority)
            return false;

        if (_barrierPrefab == null)
        {
            Debug.LogWarning($"{nameof(PlayerRunner)} requires a barrier prefab.", playerRunner);
            return false;
        }

        if (_barrierMuzzle == null)
        {
            Debug.LogWarning($"{nameof(PlayerRunner)} requires a barrier muzzle.", playerRunner);
            return false;
        }

        BarrierWave barrier = playerRunner.Runner.Spawn(
            _barrierPrefab,
            _barrierMuzzle.position,
            Quaternion.identity);

        barrier.Initialize();
        return true;
    }
}
