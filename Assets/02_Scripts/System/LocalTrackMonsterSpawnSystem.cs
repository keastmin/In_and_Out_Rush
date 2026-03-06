using System.Collections;
using Dev;
using UnityEngine;

public class LocalTrackMonsterSpawnSystem : MonoBehaviour
{
    [SerializeField] Transform monsterParentTransform;
    [SerializeField] LocalTrackMonster monsterPrefab;
    [SerializeField] float spawnInterval;
    [SerializeField] int spawnCount;

    public void SpawnMonsters(Track track)
    {
        StartCoroutine(MonsterSpawnRoutine(track));
    }

    IEnumerator MonsterSpawnRoutine(Track track)
    {
        var startPosition = track.Vertices[0];

        for (int i = 0; i < spawnCount; i++)
        {
            var monster = Instantiate(monsterPrefab, startPosition, Quaternion.identity, monsterParentTransform);
            monster.name = $"Monster_{i}";
            monster.SetTrack(track);
            monster.Initialize();

            yield return new WaitForSeconds(spawnInterval);
        }
    }
}