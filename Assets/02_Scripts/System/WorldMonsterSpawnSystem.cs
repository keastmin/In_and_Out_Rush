using System.Collections.Generic;
using Dev.Network;
using Fusion;
using KIM.Dev;
using UnityEngine;

public class WorldMonsterSpawnSystem : NetworkSystemBase, IWorldObstacleConsumer
{
    const int MaxSpawnPositionAttempts = 100;

    [Header("Chunk Streaming")]
    [SerializeField, Min(1f)] float chunkSize = 32f;
    [SerializeField, Min(0)] int activeChunkRadius = 1;
    [SerializeField, Min(0.1f)] float dormantWanderRadius = 8f;
    [SerializeField, Min(0.05f)] float streamingRefreshInterval = 0.25f;
    [SerializeField, Min(1)] int maxSpawnsPerRefresh = 8;

    [SerializeField] TerritorySystem territorySystem;
    [SerializeField] Transform monsterParentTransform;
    [SerializeField] WorldMonsterSpawnTable spawnTable;
    [SerializeField] Transform playerTransform;

    private readonly List<SandTomb> _placedSandTombs = new();
    private readonly List<WorldMonsterSpawnRecord> _spawnRecords = new();
    private IReadOnlyList<WorldObstacle> _worldObstacles;
    private TickTimer _streamingRefreshTimer;
    private bool _hasPreparedSpawnRecords;

    public void InitializeWorldObstacles(IReadOnlyList<WorldObstacle> worldObstacles)
    {
        _worldObstacles = worldObstacles;
    }

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

        if (_hasPreparedSpawnRecords)
            return;

        CachePlacedSandTombs();
        StageBootstrapper stageBootstrapper = ResolveStageBootstrapper();
        SacredZoneSystem sacredZoneSystem = ResolveSacredZoneSystem(stageBootstrapper);

        var spawnGroups = spawnTable.SpawnGroups;
        for (int i = 0; i < spawnGroups.Count; i++)
            PrepareMonsterGroup(spawnGroups[i], territory, sacredZoneSystem, stageBootstrapper);

        _hasPreparedSpawnRecords = true;
        RefreshChunkStreaming(territory);
        _streamingRefreshTimer = TickTimer.CreateFromSeconds(Runner, streamingRefreshInterval);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || !_hasPreparedSpawnRecords)
            return;

        if (!_streamingRefreshTimer.ExpiredOrNotRunning(Runner))
            return;

        Territory territory = ResolveTerritory();
        if (territory != null)
            RefreshChunkStreaming(territory);

        _streamingRefreshTimer = TickTimer.CreateFromSeconds(Runner, streamingRefreshInterval);
    }

    public override void TearDown()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            for (int i = 0; i < _spawnRecords.Count; i++)
            {
                WorldMonster activeMonster = _spawnRecords[i].ActiveMonster;
                if (activeMonster != null && activeMonster.Object != null && activeMonster.Object.IsValid)
                    Runner.Despawn(activeMonster.Object);
            }
        }

        _spawnRecords.Clear();
        _worldObstacles = null;
        _hasPreparedSpawnRecords = false;
        _streamingRefreshTimer = default;
    }

    void PrepareMonsterGroup(
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

            _spawnRecords.Add(new WorldMonsterSpawnRecord(
                prefab,
                randomSpawnPosition,
                _spawnRecords.Count + 1));
        }
    }

    private void RefreshChunkStreaming(Territory territory)
    {
        if (playerTransform == null && StageBootstrapper.Instance != null && StageBootstrapper.Instance.PlayerRunner != null)
            playerTransform = StageBootstrapper.Instance.PlayerRunner.transform;

        if (playerTransform == null)
            return;

        Vector2Int playerChunk = GetChunk(playerTransform.position);
        int remainingSpawns = maxSpawnsPerRefresh;

        for (int i = 0; i < _spawnRecords.Count; i++)
        {
            WorldMonsterSpawnRecord record = _spawnRecords[i];
            if (record.IsDestroyed)
                continue;

            WorldMonster activeMonster = record.ActiveMonster;
            if (activeMonster != null && (activeMonster.Object == null || !activeMonster.Object.IsValid))
            {
                record.ActiveMonster = null;
                record.IsDestroyed = true;
                continue;
            }

            Vector3 currentPosition = activeMonster != null
                ? activeMonster.transform.position
                : record.Position;
            if (activeMonster == null &&
                territory.IsPointInPolygon(new Vector2(currentPosition.x, currentPosition.z)))
            {
                record.ActiveMonster = null;
                record.IsDestroyed = true;

                if (activeMonster != null)
                    activeMonster.DestroyMonster();

                continue;
            }

            bool shouldBeActive = IsWithinActiveChunkRange(record.Position, playerChunk);
            if (shouldBeActive)
            {
                if (activeMonster == null && remainingSpawns-- > 0)
                    SpawnRecord(record, territory, i);

                continue;
            }

            if (activeMonster != null)
            {
                record.SetPosition(activeMonster.transform.position);
                record.ActiveMonster = null;
                Runner.Despawn(activeMonster.Object);
            }

            record.AdvanceDormantPosition(Runner.SimulationTime, dormantWanderRadius);
            if (territory.IsPointInPolygon(new Vector2(record.Position.x, record.Position.z)))
                record.IsDestroyed = true;
        }
    }

    private void SpawnRecord(WorldMonsterSpawnRecord record, Territory territory, int spawnSequence)
    {
        Vector3 spawnPosition = record.Position;
        WorldMonster monster = Runner.Spawn(record.Prefab, spawnPosition, Quaternion.identity, PlayerRef.None, (runner, obj) =>
        {
            obj.name = $"{record.Prefab.name}_{spawnSequence}";
            WorldMonster spawnedMonster = obj.GetComponent<WorldMonster>();
            if (spawnedMonster == null)
                return;

            spawnedMonster.SetTerritory(territory);
            spawnedMonster.SetPlayerTransform(playerTransform);
            spawnedMonster.SetPatrolPivotPosition(record.PivotPosition);
            spawnedMonster.SetWorldObstacles(_worldObstacles);
            spawnedMonster.RegisterTerritoryExpansion(territorySystem);
        });

        if (monster == null)
        {
            Debug.LogWarning($"World monster spawn failed for {record.Prefab.name}.");
            return;
        }

        record.ActiveMonster = monster;
    }

    private Vector2Int GetChunk(Vector3 position)
    {
        float safeChunkSize = Mathf.Max(1f, chunkSize);
        return new Vector2Int(
            Mathf.FloorToInt(position.x / safeChunkSize),
            Mathf.FloorToInt(position.z / safeChunkSize));
    }

    private bool IsWithinActiveChunkRange(Vector3 position, Vector2Int playerChunk)
    {
        Vector2Int monsterChunk = GetChunk(position);
        return Mathf.Abs(monsterChunk.x - playerChunk.x) <= activeChunkRadius &&
               Mathf.Abs(monsterChunk.y - playerChunk.y) <= activeChunkRadius;
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

    private sealed class WorldMonsterSpawnRecord
    {
        public WorldMonsterSpawnRecord(WorldMonster prefab, Vector3 spawnPosition, int seed)
        {
            Prefab = prefab;
            PivotPosition = spawnPosition;
            Position = spawnPosition;
            Seed = seed;
        }

        public WorldMonster Prefab { get; }
        public Vector3 PivotPosition { get; }
        public Vector3 Position { get; private set; }
        public int Seed { get; }
        public WorldMonster ActiveMonster { get; set; }
        public bool IsDestroyed { get; set; }

        public void AdvanceDormantPosition(float simulationTime, float wanderRadius)
        {
            float phase = simulationTime * 0.17f + Seed * 0.6180339f;
            float radius = Mathf.Max(0f, wanderRadius) * (0.35f + 0.15f * Mathf.Sin(phase * 0.71f));
            Vector3 offset = new(
                Mathf.Cos(phase) * radius,
                0f,
                Mathf.Sin(phase * 1.23f) * radius);

            Position = PivotPosition + offset;
        }

        public void SetPosition(Vector3 position)
        {
            Position = position;
        }
    }
}
