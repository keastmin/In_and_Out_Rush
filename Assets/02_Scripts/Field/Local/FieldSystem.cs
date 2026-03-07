using UnityEngine;

namespace Dev.Local
{
    public class FieldSystem : System
    {
        [SerializeField] private LocalWorldMonster[] _monsterPrefabs;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private int _spawnCount;
        [SerializeField] private int _spawnRadius;
        [SerializeField] private Transform _playerTransform;

        public void SpawnMonsters(out LocalWorldMonster[] spawnedMonsters)
        {
            spawnedMonsters = new LocalWorldMonster[_spawnCount];

            for (int i = 0; i < _spawnCount; i++)
            {
                var randomSpawnPosition = _spawnPoint.position + Random.insideUnitSphere * _spawnRadius;
                randomSpawnPosition.y = 0; // y축 고정

                if (StageInstance.Instance.Territory.IsPointInPolygon(randomSpawnPosition)) { i--; continue; }

                var monsterPrefab = _monsterPrefabs[Random.Range(0, _monsterPrefabs.Length)];
                var monster = Instantiate(monsterPrefab, randomSpawnPosition, Quaternion.identity);
                monster.name = $"Monster_{i}";
                monster.SetTerritory(StageInstance.Instance.Territory);
                monster.SetPlayerTransform(_playerTransform);
                monster.SetPatrolPivotPosition(randomSpawnPosition);
                monster.Initialize();

                spawnedMonsters[i] = monster;
            }
        }
    }
}