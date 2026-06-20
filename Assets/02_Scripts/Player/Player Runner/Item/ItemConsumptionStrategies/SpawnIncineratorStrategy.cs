using UnityEngine;

public sealed class SpawnIncineratorStrategy : IItemConsumptionStrategy
{
    private readonly IncineratorDrone _dronePrefab;
    private IncineratorDrone _activeDrone;

    public SpawnIncineratorStrategy(IncineratorDrone dronePrefab)
    {
        _dronePrefab = dronePrefab;
    }

    public RunnerItemType ItemType => RunnerItemType.Incinerator;

    public bool TryUse(RunnerItemUseContext context)
    {
        PlayerRunner playerRunner = context.User;
        if (playerRunner == null || !playerRunner.HasStateAuthority)
            return false;

        if (_activeDrone != null && _activeDrone.IsActive)
            return false;

        if (_dronePrefab == null)
        {
            Debug.LogWarning($"{nameof(PlayerRunner)} requires an incinerator drone prefab.", playerRunner);
            return false;
        }

        Vector3 spawnPosition = playerRunner.transform.TransformPoint(_dronePrefab.LocalOffset);
        IncineratorDrone drone = playerRunner.Runner.Spawn(
            _dronePrefab,
            spawnPosition,
            playerRunner.transform.rotation);

        if (drone == null || !drone.Initialize(playerRunner))
        {
            if (drone != null && drone.Object != null && drone.Object.IsValid)
                playerRunner.Runner.Despawn(drone.Object);

            return false;
        }

        _activeDrone = drone;
        return true;
    }
}
