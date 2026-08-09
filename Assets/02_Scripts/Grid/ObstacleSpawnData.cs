using Fusion;
using UnityEngine;

namespace KIM.Dev
{
    [CreateAssetMenu(fileName = "ObstacleSpawnData", menuName = "Scriptable Objects/Obstacle Spawn Data")]
    public sealed class ObstacleSpawnData : ScriptableObject
    {
        [SerializeField] private NetworkObject _prefab;
        [SerializeField, Min(1)] private int _spawnCount = 4;
        [SerializeField] private float _heightOffset;

        public NetworkObject Prefab => _prefab;
        public int SpawnCount => Mathf.Max(1, _spawnCount);
        public float HeightOffset => _heightOffset;
        public bool IsAvailable => _prefab != null;
    }
}
