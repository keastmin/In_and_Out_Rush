using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldMonsterSpawnTable", menuName = "Scriptable Objects/World Monster Spawn Table")]
public sealed class WorldMonsterSpawnTable : ScriptableObject
{
    [SerializeField] private List<WorldMonsterSpawnGroup> spawnGroups = new();

    public IReadOnlyList<WorldMonsterSpawnGroup> SpawnGroups => spawnGroups;
    public bool HasSpawnGroups => spawnGroups != null && spawnGroups.Count > 0;
}

[global::System.Serializable]
public sealed class WorldMonsterSpawnGroup
{
    [SerializeField] private WorldMonster prefab;
    [SerializeField, Min(0)] private int spawnCount;
    [SerializeField, Min(0f)] private float spawnRadius;

    public WorldMonster Prefab => prefab;
    public int SpawnCount => Mathf.Max(0, spawnCount);
    public float SpawnRadius => Mathf.Max(0f, spawnRadius);
}
