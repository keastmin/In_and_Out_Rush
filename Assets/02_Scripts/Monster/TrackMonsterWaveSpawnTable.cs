using System.Collections.Generic;
using ProjectIO.Monsters;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "TrackMonsterWaveSpawnTable", menuName = "Scriptable Objects/Track Monster Wave Spawn Table")]
public sealed class TrackMonsterWaveSpawnTable : ScriptableObject
{
    [Header("Normal")]
    [SerializeField] private TrackMonster normalSmallPrefab;
    [SerializeField] private TrackMonster normalMediumPrefab;
    [SerializeField] private TrackMonster normalLargePrefab;

    [Header("Special")]
    [SerializeField] private TrackMonster elitePredatorPrefab;
    [SerializeField] private TrackMonster eliteWalkerPrefab;
    [SerializeField] private TrackMonster bossPrefab;
    [SerializeField] private TrackMonster internalizedPrefab;

    [SerializeField] private List<TrackMonsterWaveData> waves = new();

    public IReadOnlyList<TrackMonsterWaveData> Waves => waves;
    public TrackMonster InternalizedPrefab => internalizedPrefab;

    public TrackMonster GetNormalPrefab(TrackMonsterNormalSize size)
    {
        return size switch
        {
            TrackMonsterNormalSize.Small => normalSmallPrefab,
            TrackMonsterNormalSize.Medium => normalMediumPrefab,
            TrackMonsterNormalSize.Large => normalLargePrefab,
            _ => null
        };
    }

    public TrackMonster GetSpecialPrefab(TrackMonsterSpawnType spawnType)
    {
        return spawnType switch
        {
            TrackMonsterSpawnType.ElitePredator => elitePredatorPrefab,
            TrackMonsterSpawnType.EliteWalker => eliteWalkerPrefab,
            TrackMonsterSpawnType.Boss => bossPrefab,
            _ => null
        };
    }

    public bool TryGetWave(int waveNumber, out TrackMonsterWaveData wave)
    {
        wave = null;
        if (waves == null)
            return false;

        for (int i = 0; i < waves.Count; i++)
        {
            TrackMonsterWaveData candidate = waves[i];
            if (candidate != null && candidate.WaveNumber == waveNumber)
            {
                wave = candidate;
                return true;
            }
        }

        return false;
    }
}

[global::System.Serializable]
public sealed class TrackMonsterWaveData
{
    [SerializeField, Min(1)] private int waveNumber = 1;
    [SerializeField] private List<TrackMonsterSpawnGroup> spawnGroups = new();

    public int WaveNumber => Mathf.Max(1, waveNumber);
    public IReadOnlyList<TrackMonsterSpawnGroup> SpawnGroups => spawnGroups;
    public bool HasSpawnGroups => spawnGroups != null && spawnGroups.Count > 0;
}

[global::System.Serializable]
public sealed class TrackMonsterSpawnGroup
{
    [SerializeField] private TrackMonsterSpawnType spawnType;
    [SerializeField, Min(0f)] private float delayFromWaveStartSeconds;
    [FormerlySerializedAs("spawnCount")]
    [SerializeField, Min(0)] private int spawnUnitCount;
    [SerializeField, Min(0f)] private float spawnIntervalSeconds;
    [SerializeField, Min(1)] private int repeatCount = 1;
    [SerializeField, Min(0f)] private float repeatIntervalSeconds;

    public TrackMonsterSpawnType SpawnType => spawnType;
    public float DelayFromWaveStartSeconds => Mathf.Max(0f, delayFromWaveStartSeconds);
    public int SpawnUnitCount => Mathf.Max(0, spawnUnitCount);
    public float SpawnIntervalSeconds => Mathf.Max(0f, spawnIntervalSeconds);
    public int RepeatCount => Mathf.Max(1, repeatCount);
    public float RepeatIntervalSeconds => Mathf.Max(0f, repeatIntervalSeconds);
}
