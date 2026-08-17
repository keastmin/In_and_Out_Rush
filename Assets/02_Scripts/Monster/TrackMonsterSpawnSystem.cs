using System.Collections;
using System.Collections.Generic;
using Dev;
using Dev.Network;
using Fusion;
using UnityEngine;

public class TrackMonsterSpawnSystem : Dev.Network.System
{
    const float TrackMonsterSettlementInterval = 0.2f;

    [SerializeField] TrackSystem trackSystem;
    [SerializeField] TerritorySystem territorySystem;
    [SerializeField] Transform monsterParentTransform;
    [SerializeField] TrackMonster monsterPrefab;
    [SerializeField] TrackMonsterWaveSpawnTable waveSpawnTable;
    [SerializeField] float spawnInterval;
    [SerializeField] int spawnCount;
    [SerializeField] float strengthenMultiplier = 1.2f;

    readonly List<TrackMonster> aliveTrackMonsters = new();
    readonly List<Coroutine> monsterSpawnRoutines = new();
    int spawnSequence;
    int spawnPrioritySequence;
    int strengthenCount;

    protected override void OnSetUp()
    {
        if (!Object.HasStateAuthority) { return; }
        ResolveTerritorySystem();
    }

    public void SpawnMonsters(Track track)
    {
        SpawnMonsters(track, 0);
    }

    public void SpawnMonsters(Track track, int waveNumber)
    {
        if (!Object.HasStateAuthority) { return; }
        if (track == null || track.Vertices == null || track.Vertices.Length == 0)
        {
            Debug.LogWarning("Track monster spawn skipped. Track is not ready.");
            return;
        }

        StopMonsterSpawnRoutine();
        spawnPrioritySequence = 0;

        if (!TryGetWaveData(waveNumber, out TrackMonsterWaveData waveData))
        {
            Debug.LogWarning($"Track monster wave data missing for wave {waveNumber}. Fallback spawn settings will be used.");
            StartTrackedMonsterSpawnRoutine(FallbackMonsterSpawnRoutine(track));
            return;
        }

        IReadOnlyList<TrackMonsterSpawnGroup> spawnGroups = waveData.SpawnGroups;
        for (int i = 0; i < spawnGroups.Count; i++)
        {
            TrackMonsterSpawnGroup spawnGroup = spawnGroups[i];
            if (spawnGroup == null || spawnGroup.SpawnCount <= 0)
                continue;

            StartTrackedMonsterSpawnRoutine(MonsterSpawnGroupRoutine(track, spawnGroup));
        }

        if (monsterSpawnRoutines.Count <= 0)
        {
            Debug.LogWarning($"Track monster wave {waveNumber} has no valid spawn groups. Fallback spawn settings will be used.");
            StartTrackedMonsterSpawnRoutine(FallbackMonsterSpawnRoutine(track));
        }
    }

    public void SpawnInternalizedMonsters(Track track, int count)
    {
        if (!Object.HasStateAuthority || count <= 0) { return; }
        if (track == null || track.Vertices == null || track.Vertices.Length == 0)
        {
            Debug.LogWarning("Internalized track monster spawn skipped. Track is not ready.");
            return;
        }

        for (int i = 0; i < count; i++)
            SpawnTrackMonster(track, monsterPrefab, true, i);
    }

    public void StrengthenTrackMonsters()
    {
        if (!Object.HasStateAuthority) { return; }

        strengthenCount++;
        CleanupDestroyedTrackMonsters();
        Debug.Log($"New track monsters will be strengthened. Count: {strengthenCount}, multiplier: {strengthenMultiplier}");
    }

    public void SettleTrackMonstersCascade(PlayerRunner runner)
    {
        if (Object == null || !Object.HasStateAuthority) { return; }
        if (runner == null)
        {
            Debug.LogWarning("Track monster settlement skipped. PlayerRunner is missing.");
            return;
        }

        StopMonsterSpawnRoutine();
        CleanupDestroyedTrackMonsters();
        var trackMonsters = GetTrackMonsterSnapshot();
        if (trackMonsters.Count <= 0)
            return;

        StartCoroutine(TrackMonsterSettlementRoutine(trackMonsters, runner));
    }

    IEnumerator FallbackMonsterSpawnRoutine(Track track)
    {
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnTrackMonster(track, monsterPrefab, false, spawnPrioritySequence++);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    IEnumerator MonsterSpawnGroupRoutine(Track track, TrackMonsterSpawnGroup spawnGroup)
    {
        if (spawnGroup.DelayFromWaveStartSeconds > 0f)
            yield return new WaitForSeconds(spawnGroup.DelayFromWaveStartSeconds);

        TrackMonster prefab = spawnGroup.Prefab != null ? spawnGroup.Prefab : monsterPrefab;
        for (int repeatIndex = 0; repeatIndex < spawnGroup.RepeatCount; repeatIndex++)
        {
            for (int spawnIndex = 0; spawnIndex < spawnGroup.SpawnCount; spawnIndex++)
            {
                SpawnTrackMonster(track, prefab, false, spawnPrioritySequence++);
                if (spawnIndex < spawnGroup.SpawnCount - 1 && spawnGroup.SpawnIntervalSeconds > 0f)
                    yield return new WaitForSeconds(spawnGroup.SpawnIntervalSeconds);
            }

            if (repeatIndex < spawnGroup.RepeatCount - 1 && spawnGroup.RepeatIntervalSeconds > 0f)
                yield return new WaitForSeconds(spawnGroup.RepeatIntervalSeconds);
        }
    }

    TrackMonster SpawnTrackMonster(Track track, TrackMonster prefab, bool internalized, int priority)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Track monster spawn skipped. Monster prefab is missing.");
            return null;
        }

        var startPosition = track.Vertices[0];
        int sequence = spawnSequence++;

        var monster = Runner.Spawn(prefab, startPosition, Quaternion.identity, PlayerRef.None, (runner, obj) =>
        {
            obj.name = internalized ? $"Internalized Monster_{sequence}" : $"Monster_{sequence}";
            obj.transform.SetParent(monsterParentTransform);
        });

        monster.SetTerritory(ResolveTerritory());
        monster.SetTrack(track);
        monster.Initialize();
        monster.SetTrackMonsterPriority(priority);
        monster.SetInternalized(internalized);
        monster.SetSpawnOrder(sequence);
        RegisterTrackMonster(monster);
        if (!internalized)
            ApplyCurrentStrength(monster);

        return monster;
    }

    bool TryGetWaveData(int waveNumber, out TrackMonsterWaveData waveData)
    {
        waveData = null;
        return waveNumber > 0 &&
               waveSpawnTable != null &&
               waveSpawnTable.TryGetWave(waveNumber, out waveData) &&
               waveData != null &&
               waveData.HasSpawnGroups;
    }

    void StartTrackedMonsterSpawnRoutine(IEnumerator routine)
    {
        Coroutine coroutine = null;
        IEnumerator TrackRoutine()
        {
            yield return routine;
            monsterSpawnRoutines.Remove(coroutine);
        }

        coroutine = StartCoroutine(TrackRoutine());
        monsterSpawnRoutines.Add(coroutine);
    }

    Territory ResolveTerritory()
    {
        ResolveTerritorySystem();
        return territorySystem != null ? territorySystem.Territory : null;
    }

    void ResolveTerritorySystem()
    {
        if (territorySystem == null && StageBootstrapper.Instance != null)
            territorySystem = StageBootstrapper.Instance.TerritorySystem;
    }

    void RegisterTrackMonster(TrackMonster monster)
    {
        if (monster == null) { return; }

        aliveTrackMonsters.Add(monster);
        monster.OnDestroyed += HandleTrackMonsterDestroyed;
    }

    void HandleTrackMonsterDestroyed(TrackMonster monster)
    {
        if (monster == null) { return; }

        monster.OnDestroyed -= HandleTrackMonsterDestroyed;
        aliveTrackMonsters.Remove(monster);
    }

    void ApplyCurrentStrength(TrackMonster monster)
    {
        if (monster == null || strengthenCount <= 0) { return; }

        monster.ApplyStatMultiplier(Mathf.Pow(strengthenMultiplier, strengthenCount));
    }

    void StopMonsterSpawnRoutine()
    {
        if (monsterSpawnRoutines.Count <= 0)
            return;

        for (int i = 0; i < monsterSpawnRoutines.Count; i++)
        {
            Coroutine routine = monsterSpawnRoutines[i];
            if (routine != null)
                StopCoroutine(routine);
        }

        monsterSpawnRoutines.Clear();
    }

    void CleanupDestroyedTrackMonsters()
    {
        for (int i = aliveTrackMonsters.Count - 1; i >= 0; i--)
        {
            if (aliveTrackMonsters[i] == null || !aliveTrackMonsters[i].CanAccessNetworkState)
                aliveTrackMonsters.RemoveAt(i);
        }
    }

    List<TrackMonster> GetTrackMonsterSnapshot()
    {
        var trackMonsters = new List<TrackMonster>();
        for (int i = 0; i < aliveTrackMonsters.Count; i++)
        {
            TrackMonster monster = aliveTrackMonsters[i];
            if (!CanSettleTrackMonster(monster))
                continue;

            trackMonsters.Add(monster);
        }

        trackMonsters.Sort((left, right) => left.SpawnOrder.CompareTo(right.SpawnOrder));
        return trackMonsters;
    }

    IEnumerator TrackMonsterSettlementRoutine(List<TrackMonster> monsters, PlayerRunner runner)
    {
        for (int i = 0; i < monsters.Count; i++)
        {
            TrackMonster monster = monsters[i];
            if (!CanSettleTrackMonster(monster))
                continue;

            float damage = monster.CompletionDamage;
            runner.TakeTrackCompletionDamage(damage);
            Debug.Log($"{monster.name} settled and dealt {damage} damage to PlayerRunner.");

            monster.DestroyMonster();

            if (i < monsters.Count - 1)
                yield return new WaitForSeconds(TrackMonsterSettlementInterval);
        }
    }

    static bool CanSettleTrackMonster(TrackMonster monster)
    {
        return monster != null &&
               monster.CanAccessNetworkState &&
               monster.Object.HasStateAuthority;
    }
}
