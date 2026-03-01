using Dev.Local;
using UnityEngine;

public class LocalWorldMonsterSpawnSystem : MonoBehaviour
{
    [SerializeField] Dev.Local.TerritorySystem territorySystem;
    [SerializeField] Transform monsterParentTransform;
    [SerializeField] LocalWorldMonster monsterPrefab;
    [SerializeField] int spawnCount;
    [SerializeField] int spawnRadius;
    [SerializeField] Transform playerTransform;

    public void SpawnMonsters()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            var randomSpawnPosition = monsterParentTransform.position + Random.insideUnitSphere * spawnRadius;
            randomSpawnPosition.y = 0; // y축 고정

            if (StageInstance.Instance.Territory.IsPointInPolygon(randomSpawnPosition)) { i--; continue; }

            var monster = Instantiate(monsterPrefab, randomSpawnPosition, Quaternion.identity, monsterParentTransform);
            monster.name = $"Monster_{i}";
            monster.SetTerritory(StageInstance.Instance.Territory);
            monster.SetPlayerTransform(playerTransform);
            monster.SetPatrolPivotPosition(randomSpawnPosition);
            monster.Initialize();
            StageInstance.Instance.TerritoryExpansion.OnTerritoryExpanded += monster.OnTerritoryExpanded; // TODO: 밖에서 주입
        }
    }
}