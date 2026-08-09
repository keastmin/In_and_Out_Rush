using System.Collections.Generic;
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

    private readonly List<SandTomb> _placedSandTombs = new();

    public override void SetUp()
    {
        if (!Object.HasStateAuthority) { return; }
        if (StageBootstrapper.Instance != null && StageBootstrapper.Instance.PlayerRunner != null)
            playerTransform = StageBootstrapper.Instance.PlayerRunner.transform;
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

        CachePlacedSandTombs();
        StageBootstrapper stageBootstrapper = ResolveStageBootstrapper();
        SacredZoneSystem sacredZoneSystem = ResolveSacredZoneSystem(stageBootstrapper);

        var spawnGroups = spawnTable.SpawnGroups;
        for (int i = 0; i < spawnGroups.Count; i++)
            SpawnMonsterGroup(spawnGroups[i], territory, sacredZoneSystem, stageBootstrapper);
    }

    void SpawnMonsterGroup(
        WorldMonsterSpawnGroup spawnGroup,
        Territory territory,
        SacredZoneSystem sacredZoneSystem,
        StageBootstrapper stageBootstrapper)
    {
        if (!IsValidSpawnGroup(spawnGroup))
            return;

        WorldMonster prefab = spawnGroup.Prefab;
        SandTomb sandTombPrefab = prefab as SandTomb;
        if (sandTombPrefab != null && !CanSpawnSandTombs(sacredZoneSystem, stageBootstrapper))
        {
            Debug.LogWarning(
                "Sand tomb spawn skipped. Sacred zone and sanctuary placement sources are not ready.");
            return;
        }

        for (int i = 0; i < spawnGroup.SpawnCount; i++)
        {
            if (!TryGetSpawnPosition(
                    spawnGroup.SpawnRadius,
                    territory,
                    sandTombPrefab,
                    sacredZoneSystem,
                    stageBootstrapper,
                    out Vector3 randomSpawnPosition))
            {
                Debug.LogWarning($"World monster spawn skipped. Could not find a valid spawn position for {prefab.name}.");
                continue;
            }

            int spawnSequence = i;
            var monster = Runner.Spawn(prefab, randomSpawnPosition, Quaternion.identity, PlayerRef.None, (runner, obj) =>
            {
                obj.name = $"{prefab.name}_{spawnSequence}";
                // obj.transform.SetParent(monsterParentTransform);
            });

            if (monster == null)
            {
                Debug.LogWarning($"World monster spawn failed for {prefab.name}.");
                continue;
            }

            monster.SetTerritory(territory);
            monster.SetPlayerTransform(playerTransform);
            monster.SetPatrolPivotPosition(randomSpawnPosition);
            monster.Initialize();
            monster.RegisterTerritoryExpansion(territorySystem);

            if (monster is SandTomb spawnedSandTomb)
                _placedSandTombs.Add(spawnedSandTomb);
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

    bool TryGetSpawnPosition(
        float spawnRadius,
        Territory territory,
        SandTomb sandTombPrefab,
        SacredZoneSystem sacredZoneSystem,
        StageBootstrapper stageBootstrapper,
        out Vector3 spawnPosition)
    {
        float safeSpawnRadius = Mathf.Max(0f, spawnRadius);
        for (int i = 0; i < MaxSpawnPositionAttempts; i++)
        {
            Vector2 randomSpawnPosition2d = Random.insideUnitCircle * safeSpawnRadius;
            if (territory.IsPointInPolygon(randomSpawnPosition2d))
                continue;

            spawnPosition = new Vector3(randomSpawnPosition2d.x, 0f, randomSpawnPosition2d.y);
            if (sandTombPrefab != null &&
                IsSandTombPositionBlocked(spawnPosition, sandTombPrefab, sacredZoneSystem, stageBootstrapper))
            {
                continue;
            }

            return true;
        }

        spawnPosition = default;
        return false;
    }

    private bool IsSandTombPositionBlocked(
        Vector3 position,
        SandTomb sandTombPrefab,
        SacredZoneSystem sacredZoneSystem,
        StageBootstrapper stageBootstrapper)
    {
        float candidateRadius = sandTombPrefab.SpawnExclusionRadius;
        if (sacredZoneSystem.IsCircleOverlappingSacredZone(position, candidateRadius))
            return true;

        if (stageBootstrapper.IsCircleOverlappingAnySanctuary(position, candidateRadius))
            return true;

        for (int i = 0; i < _placedSandTombs.Count; i++)
        {
            SandTomb placedSandTomb = _placedSandTombs[i];
            if (placedSandTomb == null)
                continue;

            float requiredDistance = candidateRadius + placedSandTomb.SpawnExclusionRadius;
            if (Vector3.SqrMagnitude(position - placedSandTomb.transform.position) <=
                requiredDistance * requiredDistance)
            {
                return true;
            }
        }

        return false;
    }

    private static bool CanSpawnSandTombs(
        SacredZoneSystem sacredZoneSystem,
        StageBootstrapper stageBootstrapper)
    {
        return sacredZoneSystem != null &&
               sacredZoneSystem.IsInitialized &&
               stageBootstrapper != null;
    }

    private void CachePlacedSandTombs()
    {
        _placedSandTombs.Clear();
        SandTomb[] existingSandTombs =
            UnityEngine.Object.FindObjectsByType<SandTomb>(FindObjectsSortMode.None);

        for (int i = 0; i < existingSandTombs.Length; i++)
        {
            if (existingSandTombs[i] != null)
                _placedSandTombs.Add(existingSandTombs[i]);
        }
    }

    private static StageBootstrapper ResolveStageBootstrapper()
    {
        return StageBootstrapper.Instance != null
            ? StageBootstrapper.Instance
            : UnityEngine.Object.FindFirstObjectByType<StageBootstrapper>();
    }

    private static SacredZoneSystem ResolveSacredZoneSystem(StageBootstrapper stageBootstrapper)
    {
        if (stageBootstrapper != null && stageBootstrapper.SacredZoneSystem != null)
            return stageBootstrapper.SacredZoneSystem;

        return UnityEngine.Object.FindFirstObjectByType<SacredZoneSystem>();
    }

    Territory ResolveTerritory()
    {
        if (territorySystem == null && StageBootstrapper.Instance != null)
            territorySystem = StageBootstrapper.Instance.TerritorySystem;

        return territorySystem != null ? territorySystem.Territory : null;
    }
}
