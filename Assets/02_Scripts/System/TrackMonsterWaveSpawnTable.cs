using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrackMonsterWaveSpawnTable", menuName = "Scriptable Objects/Track Monster Wave Spawn Table")]
public sealed class TrackMonsterWaveSpawnTable : ScriptableObject
{
    [SerializeField] private List<TrackMonsterWaveData> waves = new();

    public IReadOnlyList<TrackMonsterWaveData> Waves => waves;

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
    [SerializeField] private TrackMonster prefab;
    [SerializeField, Min(0f)] private float delayFromWaveStartSeconds;
    [SerializeField, Min(0)] private int spawnCount;
    [SerializeField, Min(0f)] private float spawnIntervalSeconds;
    [SerializeField, Min(1)] private int repeatCount = 1;
    [SerializeField, Min(0f)] private float repeatIntervalSeconds;

    public TrackMonster Prefab => prefab;
    public float DelayFromWaveStartSeconds => Mathf.Max(0f, delayFromWaveStartSeconds);
    public int SpawnCount => Mathf.Max(0, spawnCount);
    public float SpawnIntervalSeconds => Mathf.Max(0f, spawnIntervalSeconds);
    public int RepeatCount => Mathf.Max(1, repeatCount);
    public float RepeatIntervalSeconds => Mathf.Max(0f, repeatIntervalSeconds);
}
