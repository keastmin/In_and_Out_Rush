using UnityEngine;

public sealed class SpawnElectricGrenadeStrategy : IItemConsumptionStrategy
{
    private readonly ElectricGrenadeProjectile _grenadePrefab;
    private readonly Transform _grenadeMuzzle;

    public SpawnElectricGrenadeStrategy(
        ElectricGrenadeProjectile grenadePrefab,
        Transform grenadeMuzzle)
    {
        _grenadePrefab = grenadePrefab;
        _grenadeMuzzle = grenadeMuzzle;
    }

    public RunnerItemType ItemType => RunnerItemType.ElectricGrenade;

    public bool TryUse(RunnerItemUseContext context)
    {
        PlayerRunner playerRunner = context.User;
        if (playerRunner == null || !playerRunner.HasStateAuthority)
            return false;

        if (_grenadePrefab == null)
        {
            Debug.LogWarning($"{nameof(PlayerRunner)} requires an electric grenade prefab.", playerRunner);
            return false;
        }

        if (_grenadeMuzzle == null)
        {
            Debug.LogWarning($"{nameof(PlayerRunner)} requires an electric grenade muzzle.", playerRunner);
            return false;
        }

        Vector3 targetPosition = context.TargetPosition;
        if (targetPosition == default)
            targetPosition = _grenadeMuzzle.position + playerRunner.transform.forward;

        Vector3 lookDirection = targetPosition - _grenadeMuzzle.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude <= 0.0001f)
            lookDirection = playerRunner.transform.forward;
        if (lookDirection.sqrMagnitude <= 0.0001f)
            lookDirection = Vector3.forward;

        ElectricGrenadeProjectile grenade = playerRunner.Runner.Spawn(
            _grenadePrefab,
            _grenadeMuzzle.position,
            Quaternion.LookRotation(lookDirection.normalized, Vector3.up));

        grenade.Initialize(_grenadeMuzzle.position, targetPosition);
        return true;
    }
}
