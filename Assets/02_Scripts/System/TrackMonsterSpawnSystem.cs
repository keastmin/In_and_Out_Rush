using System.Collections;
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

    public override void SetUp()
    {
        if (!Object.HasStateAuthority) { return; }
        SpawnMonsters(trackSystem.Track);
    }

    public void SpawnMonsters(Track track)
    {
        if (!Object.HasStateAuthority) { return; }
        StartCoroutine(MonsterSpawnRoutine(track));
    }

    IEnumerator MonsterSpawnRoutine(Track track)
    {
        var startPosition = track.Vertices[0];

        for (int i = 0; i < spawnCount; i++)
        {
            var monster = Runner.Spawn(monsterPrefab, startPosition, Quaternion.identity, PlayerRef.None, (runner, obj) =>
            {
                obj.name = $"Monster_{i}";
                obj.transform.SetParent(monsterParentTransform);
            });

            monster.SetTrack(track);
            monster.Initialize();
            monster.SetTrackMonsterPriority(i); // 몬스터의 우선순위 할당

            yield return new WaitForSeconds(spawnInterval);
        }
    }
}