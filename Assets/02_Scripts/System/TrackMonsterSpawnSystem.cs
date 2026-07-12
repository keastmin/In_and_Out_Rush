using System.Collections;
using System.Collections.Generic;
using Dev;
using Fusion;
using UnityEngine;

public class TrackMonsterSpawnSystem : NetworkSystemBase
{
    [SerializeField] TrackSystem trackSystem;
    [SerializeField] Transform monsterParentTransform;
    [SerializeField] TrackMonster monsterPrefab;
    [SerializeField] float spawnInterval;
    [SerializeField] int spawnCount;
    [SerializeField] float strengthenMultiplier = 1.2f;

    readonly List<TrackMonster> aliveTrackMonsters = new();
    int spawnSequence;
    int strengthenCount;

    public override void SetUp()
    {
        if (!Object.HasStateAuthority) { return; }
    }

    public void SpawnMonsters(Track track)
    {
        if (!Object.HasStateAuthority) { return; }
        if (track == null || track.Vertices == null || track.Vertices.Length == 0)
        {
            Debug.LogWarning("Track monster spawn skipped. Track is not ready.");
            return;
        }

        StartCoroutine(MonsterSpawnRoutine(track));
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
            SpawnTrackMonster(track, true, i);
    }

    public void StrengthenTrackMonsters()
    {
        if (!Object.HasStateAuthority) { return; }

        strengthenCount++;
        CleanupDestroyedTrackMonsters();
        Debug.Log($"New track monsters will be strengthened. Count: {strengthenCount}, multiplier: {strengthenMultiplier}");
    }

    IEnumerator MonsterSpawnRoutine(Track track)
    {
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnTrackMonster(track, false, i);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    TrackMonster SpawnTrackMonster(Track track, bool internalized, int priority)
    {
        var startPosition = track.Vertices[0];
        int sequence = spawnSequence++;

        var monster = Runner.Spawn(monsterPrefab, startPosition, Quaternion.identity, PlayerRef.None, (runner, obj) =>
        {
            obj.name = internalized ? $"Internalized Monster_{sequence}" : $"Monster_{sequence}";
            obj.transform.SetParent(monsterParentTransform);
        });

        monster.SetTrack(track);
        monster.Initialize();
        monster.SetTrackMonsterPriority(priority);
        monster.SetInternalized(internalized);
        RegisterTrackMonster(monster);
        if (!internalized)
            ApplyCurrentStrength(monster);

        return monster;
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

    void CleanupDestroyedTrackMonsters()
    {
        for (int i = aliveTrackMonsters.Count - 1; i >= 0; i--)
        {
            if (aliveTrackMonsters[i] == null)
                aliveTrackMonsters.RemoveAt(i);
        }
    }
}
