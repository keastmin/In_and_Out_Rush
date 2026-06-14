using System;
using Fusion;
using UnityEngine;
using KIM.Dev;

namespace Dev.Network
{
    public class ResourceSpawnSystem : System
    {
        [Serializable]
        private class ResourceSpawnSettings
        {
            [SerializeField] private GameObject _prefab;
            [SerializeField, Min(0)] private int _worldCount;
            [SerializeField, Min(0)] private int _nearStartCount;

            public GameObject Prefab => _prefab;
            public int WorldCount => _worldCount;
            public int NearStartCount => _nearStartCount;
        }

        private enum SpawnArea
        {
            World,
            NearStart
        }

        [SerializeField] private ResourceSystem _resourceSystem;
        [SerializeField] private TerritorySystem _territorySystem;
        [SerializeField] private Local.ResourceView _resourceView;

        [Header("Resource Settings")]
        [SerializeField] private ResourceSpawnSettings _mineralSettings = new();
        [SerializeField] private ResourceSpawnSettings _gasSettings = new();

        [Header("Spawn Area")]
        [SerializeField, Min(0f)] private float _worldSpawnRadius = 250f;
        [SerializeField, Min(0f)] private float _nearStartSpawnRadius = 30f;
        [SerializeField, Min(1)] private int _maxRetryCount = 10;

        private readonly CircleSpawnPolicy<NetworkObject> _circleSpawnPolicy = new();
        private Vector3 _startPosition;
        private bool _hasStartPosition;

        public void SetStartPosition(Vector3 startPosition)
        {
            _startPosition = startPosition;
            _hasStartPosition = true;
        }

        protected override void OnSetUp()
        {
            if (!Object.HasStateAuthority)
                return;

            if (!_hasStartPosition)
            {
                Debug.LogWarning("Resource spawn skipped because the start position was not provided.");
                return;
            }

            GenerateResources();
        }

        public void GenerateResources()
        {
            SpawnResources(
                ResourceType.Mineral,
                _mineralSettings,
                SpawnArea.World,
                Vector3.zero,
                _worldSpawnRadius,
                _mineralSettings.WorldCount);
            SpawnResources(
                ResourceType.Gas,
                _gasSettings,
                SpawnArea.World,
                Vector3.zero,
                _worldSpawnRadius,
                _gasSettings.WorldCount);
            SpawnResources(
                ResourceType.Mineral,
                _mineralSettings,
                SpawnArea.NearStart,
                _startPosition,
                _nearStartSpawnRadius,
                _mineralSettings.NearStartCount);
            SpawnResources(
                ResourceType.Gas,
                _gasSettings,
                SpawnArea.NearStart,
                _startPosition,
                _nearStartSpawnRadius,
                _gasSettings.NearStartCount);
        }

        private void SpawnResources(
            ResourceType resourceType,
            ResourceSpawnSettings settings,
            SpawnArea spawnArea,
            Vector3 spawnPosition,
            float spawnRadius,
            int spawnCount)
        {
            if (spawnCount <= 0)
                return;

            if (settings.Prefab == null)
            {
                Debug.LogWarning($"{resourceType} resource spawn skipped in {spawnArea}: prefab is missing.");
                return;
            }

            var spawnParam = CreateSpawnParam(settings.Prefab, spawnPosition, spawnRadius);
            var spawner = new Spawner(this, new ObjectSampler(), _circleSpawnPolicy);
            var spawnedCount = 0;

            for (int i = 0; i < spawnCount; i++)
            {
                var isSpawned = spawner.Spawn(spawnParam, out var spawnedObject);
                if (!isSpawned)
                {
                    Debug.LogWarning(
                        $"Failed to spawn {resourceType} in {spawnArea} after {_maxRetryCount} attempts. " +
                        $"Resource index: {i + 1}/{spawnCount}.");
                    continue;
                }

                if (!spawnedObject.TryGetComponent<ResourceVisible>(out var resource))
                {
                    Debug.LogWarning(
                        $"Spawned {resourceType} prefab in {spawnArea} does not contain {nameof(ResourceVisible)}.");
                    Runner.Despawn(spawnedObject);
                    continue;
                }

                BindResource(resource);
                spawnedCount++;
            }

            Debug.Log($"{resourceType} resource spawn completed in {spawnArea}: {spawnedCount}/{spawnCount}.");
        }

        private CircleSpawnParam CreateSpawnParam(GameObject prefab, Vector3 spawnPosition, float spawnRadius)
        {
            return new CircleSpawnParam
            {
                ObjectSampleParam = new ObjectSampleParam
                {
                    Prefabs = new[] { prefab },
                    Weights = new[] { 1f }
                },
                MaxRetryCount = _maxRetryCount,
                SpawnPosition = spawnPosition,
                SpawnRotation = Quaternion.identity,
                SpawnRadius = spawnRadius,
                SpawnValidator = IsValidSpawnPosition
            };
        }

        private bool IsValidSpawnPosition(object args)
        {
            var (_, position, _) = (ValueTuple<GameObject, Vector3, Quaternion>)args;
            var xzPosition = new Vector2(position.x, position.z);
            return !_territorySystem.Territory.IsPointInPolygon(xzPosition);
        }

        private void BindResource(ResourceVisible resource)
        {
            void HandleTerritoryExpanded(Territory territory, TerritorySystem territorySystem)
            {
                var xzPosition = new Vector2(resource.transform.position.x, resource.transform.position.z);
                if (!territory.IsPointInPolygon(xzPosition))
                    return;

                _territorySystem.OnTerritoryExpandedEvent -= HandleTerritoryExpanded;
                resource.Collect();
            }

            _territorySystem.OnTerritoryExpandedEvent += HandleTerritoryExpanded;
            resource.OnCollected += HandleResourceCollected;
        }

        private void HandleResourceCollected(ResourceType type, int amount, ResourceVisible resource, object context)
        {
            switch (type)
            {
                case ResourceType.Mineral:
                    _resourceSystem.RPC_GetMineral(amount);
                    _resourceView.SetMineral(_resourceSystem.Mineral);
                    break;
                case ResourceType.Gas:
                    _resourceSystem.RPC_GetGas(amount);
                    _resourceView.SetGas(_resourceSystem.Gas);
                    break;
            }
            // Debug.Log($"Obtained {amount} {type} from {resource.gameObject.name}");
            Runner.Despawn(resource.Object);
        }
    }
}
