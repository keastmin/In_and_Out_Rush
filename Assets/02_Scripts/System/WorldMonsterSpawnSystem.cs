using Dev.Network;
using Fusion;
using UnityEngine;

public class WorldMonsterSpawnSystem : NetworkSystemBase
{
    [SerializeField] TerritorySystem territorySystem;
    [SerializeField] Transform monsterParentTransform;
    [SerializeField] WorldMonster[] monsterPrefabs;
    [SerializeField] int spawnCount;
    [SerializeField] int spawnRadius;
    [SerializeField] Transform playerTransform;

    public override void SetUp()
    {
        if (!Object.HasStateAuthority) { return; }
        playerTransform = StageBootstrapper.Instance.PlayerRunner.transform;
        SpawnMonsters();
    }

    public void SpawnMonsters()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            var randomSpawnPosition2d = Random.insideUnitCircle * spawnRadius;
            var randomSpawnPosition = new Vector3(randomSpawnPosition2d.x, 0, randomSpawnPosition2d.y);
            if (territorySystem.Territory.IsPointInPolygon(randomSpawnPosition2d)) { i--; continue; }

            var monsterPrefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
            var monster = Runner.Spawn(monsterPrefab, randomSpawnPosition, Quaternion.identity, PlayerRef.None, (runner, obj) =>
            {
                obj.name = $"Monster_{i}";
                obj.transform.SetParent(monsterParentTransform);
            });

            monster.SetTerritory(territorySystem.Territory);
            monster.SetPlayerTransform(playerTransform);
            monster.SetPatrolPivotPosition(randomSpawnPosition);
            monster.Initialize();
            monster.RegisterTerritoryExpansion(territorySystem);
        }
    }
}
