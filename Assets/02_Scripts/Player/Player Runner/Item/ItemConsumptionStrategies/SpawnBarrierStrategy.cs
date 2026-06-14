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

    public bool TryUse(object parameters = null)
    {
        if (parameters is not PlayerRunner playerRunner || !playerRunner.HasStateAuthority)
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

        Vector3 direction = _barrierMuzzle.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector3.forward;

        direction.Normalize();
        BarrierWave barrier = playerRunner.Runner.Spawn(
            _barrierPrefab,
            _barrierMuzzle.position,
            Quaternion.LookRotation(direction, Vector3.up));

        barrier.Initialize(direction);
        return true;
    }
}
