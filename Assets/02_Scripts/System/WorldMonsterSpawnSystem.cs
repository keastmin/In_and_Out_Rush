using Dev.Network;
using Fusion;
using UnityEngine;

public class WorldMonsterSpawnSystem : NetworkSystemBase
{
    const int MaxSpawnPositionAttempts = 100;

    [SerializeField] TerritorySystem territorySystem;
    [SerializeField] Transform monsterParentTransform;
    [SerializeField] WorldMonsterSpawnTable spawnTable;
    [SerializeField] Transform playerTransform;

    public override void SetUp()
    {
        if (!Object.HasStateAuthority) { return; }
        if (StageBootstrapper.Instance != null && StageBootstrapper.Instance.PlayerRunner != null)
            playerTransform = StageBootstrapper.Instance.PlayerRunner.transform;

        SpawnMonsters();
    }

    public void SpawnMonsters()
    {
        if (!Object.HasStateAuthority) { return; }
        if (spawnTable == null || !spawnTable.HasSpawnGroups)
        {
            Debug.LogWarning("World monster spawn skipped. Spawn table is missing or empty.");
            return;
        }

        Territory territory = ResolveTerritory();
        if (territory == null)
        {
            Debug.LogWarning("World monster spawn skipped. Territory is missing.");
            return;
        }

        var spawnGroups = spawnTable.SpawnGroups;
        for (int i = 0; i < spawnGroups.Count; i++)
            SpawnMonsterGroup(spawnGroups[i], territory);
    }

    void SpawnMonsterGroup(WorldMonsterSpawnGroup spawnGroup, Territory territory)
    {
        if (!IsValidSpawnGroup(spawnGroup))
            return;

        WorldMonster prefab = spawnGroup.Prefab;
        for (int i = 0; i < spawnGroup.SpawnCount; i++)
        {
            if (!TryGetSpawnPosition(spawnGroup.SpawnRadius, territory, out Vector3 randomSpawnPosition))
            {
                Debug.LogWarning($"World monster spawn skipped. Could not find a valid spawn position for {prefab.name}.");
                continue;
            }

            int spawnSequence = i;
            var monster = Runner.Spawn(prefab, randomSpawnPosition, Quaternion.identity, PlayerRef.None, (runner, obj) =>
            {
                obj.name = $"{prefab.name}_{spawnSequence}";
                obj.transform.SetParent(monsterParentTransform);
            });

            monster.SetTerritory(territory);
            monster.SetPlayerTransform(playerTransform);
            monster.SetPatrolPivotPosition(randomSpawnPosition);
            monster.Initialize();
            monster.RegisterTerritoryExpansion(territorySystem);
        }
    }

    bool IsValidSpawnGroup(WorldMonsterSpawnGroup spawnGroup)
    {
        if (spawnGroup == null)
        {
            Debug.LogWarning("World monster spawn group skipped. Spawn group is missing.");
            return false;
        }

        if (spawnGroup.Prefab == null)
        {
            Debug.LogWarning("World monster spawn group skipped. Monster prefab is missing.");
            return false;
        }

        if (spawnGroup.SpawnCount <= 0)
        {
            Debug.LogWarning($"World monster spawn group skipped. Spawn count must be greater than 0 for {spawnGroup.Prefab.name}.");
            return false;
        }

        return true;
    }

    bool TryGetSpawnPosition(float spawnRadius, Territory territory, out Vector3 spawnPosition)
    {
        for (int i = 0; i < MaxSpawnPositionAttempts; i++)
        {
            Vector2 randomSpawnPosition2d = Random.insideUnitCircle * spawnRadius;
            if (territory.IsPointInPolygon(randomSpawnPosition2d))
                continue;

            spawnPosition = new Vector3(randomSpawnPosition2d.x, 0f, randomSpawnPosition2d.y);
            return true;
        }

        spawnPosition = default;
        return false;
    }

    Territory ResolveTerritory()
    {
        if (territorySystem == null && StageBootstrapper.Instance != null)
            territorySystem = StageBootstrapper.Instance.TerritorySystem;

        return territorySystem != null ? territorySystem.Territory : null;
    }
}
