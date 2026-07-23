using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using KIM.Dev;

namespace Dev.Network
{
    public class ResourceSpawnSystem : System, IWorldObstacleConsumer
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

        [Serializable]
        private class MineralDistributionSettings
        {
            [SerializeField] private MineralChunkSettings _small = new(null, 100, 3f);
            [SerializeField] private MineralChunkSettings _medium = new(null, 200, 4f);
            [SerializeField] private MineralChunkSettings _large = new(null, 300, 5f);
            [SerializeField, Min(1)] private int _sectorCount = 8;
            [SerializeField, Min(1)] private int _ringCount = 3;
            [SerializeField, Min(0)] private int _mineralBudgetPerZone = 400;

            public MineralChunkSettings Small => _small;
            public MineralChunkSettings Medium => _medium;
            public MineralChunkSettings Large => _large;
            public int SectorCount => Mathf.Max(1, _sectorCount);
            public int RingCount => Mathf.Max(1, _ringCount);
            public int MineralBudgetPerZone => Mathf.Max(0, _mineralBudgetPerZone);
        }

        [Serializable]
        private class MineralChunkSettings
        {
            [SerializeField] private GameObject _prefab;
            [SerializeField, Min(1)] private int _amount;
            [SerializeField, Min(0f)] private float _minDistance;

            public MineralChunkSettings(GameObject prefab, int amount, float minDistance)
            {
                _prefab = prefab;
                _amount = amount;
                _minDistance = minDistance;
            }

            public GameObject Prefab => _prefab;
            public int Amount => Mathf.Max(1, _amount);
            public float MinDistance => Mathf.Max(0f, _minDistance);
            public bool IsAvailable => _prefab != null;
        }

        private readonly struct PlacedMineral
        {
            public readonly Vector3 Position;
            public readonly float MinDistance;

            public PlacedMineral(Vector3 position, float minDistance)
            {
                Position = position;
                MinDistance = minDistance;
            }
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
        [SerializeField] private MineralDistributionSettings _mineralDistributionSettings = new();
        [SerializeField] private ResourceSpawnSettings _gasSettings = new();

        [Header("Spawn Area")]
        [SerializeField, Min(0f)] private float _worldSpawnRadius = 250f;
        [SerializeField, Min(0f)] private float _nearStartSpawnRadius = 30f;
        [SerializeField, Min(1)] private int _maxRetryCount = 10;

        [Header("Obstacle Avoidance")]
        [SerializeField] private InfiniteGridRockSpawner _rockSpawner;
        [SerializeField, Min(0f)] private float _mineralObstaclePadding = 0f;

        private readonly CircleSpawnPolicy<NetworkObject> _circleSpawnPolicy = new();
        private readonly List<PlacedMineral> _placedMinerals = new();
        private IReadOnlyList<WorldObstacle> _worldObstacles;
        private Vector3 _startPosition;
        private bool _hasStartPosition;

        public void SetStartPosition(Vector3 startPosition)
        {
            _startPosition = startPosition;
            _hasStartPosition = true;
        }

        public void InitializeWorldObstacles(IReadOnlyList<WorldObstacle> worldObstacles)
        {
            _worldObstacles = worldObstacles;
        }

        protected override void OnSetUp()
        {
            if (!Object.HasStateAuthority)
                return;

            ResolveReferences();

            if (!_hasStartPosition)
            {
                Debug.LogWarning("Resource spawn skipped because the start position was not provided.");
                return;
            }

            GenerateResources();
        }

        private void ResolveReferences()
        {
            if (_resourceSystem == null && StageBootstrapper.Instance != null)
                _resourceSystem = StageBootstrapper.Instance.ResourceSystem;

            if (_resourceSystem == null)
                _resourceSystem = ResourceSystem.Instance;

            if (_territorySystem == null && StageBootstrapper.Instance != null)
                _territorySystem = StageBootstrapper.Instance.TerritorySystem;

            if (_territorySystem == null)
                _territorySystem = UnityEngine.Object.FindFirstObjectByType<TerritorySystem>();

            if (_resourceView == null)
                _resourceView = UnityEngine.Object.FindFirstObjectByType<Local.ResourceView>();

            ResolveWorldObstacles();

            if (_resourceSystem == null)
                Debug.LogWarning($"{nameof(ResourceSpawnSystem)} has no {nameof(ResourceSystem)} reference.");
        }

        private void ResolveWorldObstacles()
        {
            if (_rockSpawner == null && StageBootstrapper.Instance != null && StageBootstrapper.Instance.Grid != null)
                StageBootstrapper.Instance.Grid.TryGetComponent(out _rockSpawner);

            if (_rockSpawner == null)
                _rockSpawner = UnityEngine.Object.FindFirstObjectByType<InfiniteGridRockSpawner>();

            if (_worldObstacles == null && _rockSpawner != null)
                InitializeWorldObstacles(_rockSpawner.SpawnedRocks);
        }

        public void GenerateResources()
        {
            SpawnMineralsByZone();
            SpawnResources(
                ResourceType.Gas,
                _gasSettings,
                SpawnArea.World,
                Vector3.zero,
                _worldSpawnRadius,
                _gasSettings.WorldCount);
            SpawnResources(
                ResourceType.Gas,
                _gasSettings,
                SpawnArea.NearStart,
                _startPosition,
                _nearStartSpawnRadius,
                _gasSettings.NearStartCount);
        }

        private void SpawnMineralsByZone()
        {
            _placedMinerals.Clear();

            if (_mineralDistributionSettings == null ||
                _mineralDistributionSettings.MineralBudgetPerZone <= 0 ||
                _worldSpawnRadius <= 0f)
            {
                return;
            }

            var availableChunks = GetAvailableMineralChunks();
            if (availableChunks.Count == 0)
            {
                Debug.LogWarning("Mineral zone spawn skipped: no mineral prefab is assigned.");
                return;
            }

            int sectorCount = _mineralDistributionSettings.SectorCount;
            int ringCount = _mineralDistributionSettings.RingCount;
            int spawnedCount = 0;
            int spawnedAmount = 0;
            int skippedZoneCount = 0;

            for (int ringIndex = 0; ringIndex < ringCount; ringIndex++)
            {
                float innerRadius = _worldSpawnRadius * Mathf.Sqrt((float)ringIndex / ringCount);
                float outerRadius = _worldSpawnRadius * Mathf.Sqrt((float)(ringIndex + 1) / ringCount);

                for (int sectorIndex = 0; sectorIndex < sectorCount; sectorIndex++)
                {
                    float startAngle = Mathf.PI * 2f * sectorIndex / sectorCount;
                    float endAngle = Mathf.PI * 2f * (sectorIndex + 1) / sectorCount;

                    if (!TryCreateMineralBudgetPlan(
                            _mineralDistributionSettings.MineralBudgetPerZone,
                            availableChunks,
                            out List<MineralChunkSettings> zoneChunks))
                    {
                        skippedZoneCount++;
                        Debug.LogWarning(
                            $"Mineral zone skipped. Budget cannot be filled exactly. " +
                            $"Ring: {ringIndex}, Sector: {sectorIndex}, Budget: {_mineralDistributionSettings.MineralBudgetPerZone}.");
                        continue;
                    }

                    var zonePlacedMinerals = new List<PlacedMineral>(zoneChunks.Count);
                    var zoneResources = new List<ResourceVisible>(zoneChunks.Count);
                    var zoneObjects = new List<NetworkObject>(zoneChunks.Count);
                    bool isZoneCompleted = true;

                    for (int i = 0; i < zoneChunks.Count; i++)
                    {
                        MineralChunkSettings chunk = zoneChunks[i];
                        if (!TryFindMineralPosition(
                                innerRadius,
                                outerRadius,
                                startAngle,
                                endAngle,
                                chunk,
                                zonePlacedMinerals,
                                out Vector3 position))
                        {
                            Debug.LogWarning(
                                $"Failed to place mineral in zone after {_maxRetryCount} attempts. " +
                                $"Ring: {ringIndex}, Sector: {sectorIndex}, Amount: {chunk.Amount}.");
                            isZoneCompleted = false;
                            continue;
                        }

                        if (!TrySpawnMineral(chunk, position, out ResourceVisible resource))
                        {
                            isZoneCompleted = false;
                            continue;
                        }

                        zonePlacedMinerals.Add(new PlacedMineral(position, chunk.MinDistance));
                        zoneResources.Add(resource);
                        zoneObjects.Add(resource.Object);
                    }

                    if (!isZoneCompleted || zoneResources.Count != zoneChunks.Count)
                    {
                        skippedZoneCount++;
                        DespawnZoneObjects(zoneObjects);
                        continue;
                    }

                    for (int i = 0; i < zoneResources.Count; i++)
                    {
                        BindResource(zoneResources[i]);
                        _placedMinerals.Add(zonePlacedMinerals[i]);
                        spawnedCount++;
                        spawnedAmount += zoneResources[i].Amount;
                    }
                }
            }

            Debug.Log(
                $"Mineral zone spawn completed: {spawnedCount} chunks, {spawnedAmount} minerals, " +
                $"{skippedZoneCount}/{ringCount * sectorCount} zones skipped.");
        }

        private List<MineralChunkSettings> GetAvailableMineralChunks()
        {
            var chunks = new List<MineralChunkSettings>(3);
            AddAvailableMineralChunk(chunks, _mineralDistributionSettings.Small);
            AddAvailableMineralChunk(chunks, _mineralDistributionSettings.Medium);
            AddAvailableMineralChunk(chunks, _mineralDistributionSettings.Large);
            return chunks;
        }

        private static void AddAvailableMineralChunk(List<MineralChunkSettings> chunks, MineralChunkSettings chunk)
        {
            if (chunk == null || !chunk.IsAvailable)
                return;

            if (!chunk.Prefab.TryGetComponent<ResourceVisible>(out _))
            {
                Debug.LogWarning($"Mineral prefab {chunk.Prefab.name} does not contain {nameof(ResourceVisible)}.");
                return;
            }

            chunks.Add(chunk);
        }

        private static bool TryCreateMineralBudgetPlan(
            int budget,
            IReadOnlyList<MineralChunkSettings> availableChunks,
            out List<MineralChunkSettings> chunks)
        {
            chunks = new List<MineralChunkSettings>();
            if (budget <= 0)
                return true;

            if (!CanFillBudget(budget, availableChunks))
                return false;

            int remainingBudget = budget;
            while (remainingBudget > 0)
            {
                var candidates = new List<MineralChunkSettings>();
                for (int i = 0; i < availableChunks.Count; i++)
                {
                    MineralChunkSettings chunk = availableChunks[i];
                    int nextBudget = remainingBudget - chunk.Amount;
                    if (nextBudget >= 0 && CanFillBudget(nextBudget, availableChunks))
                        candidates.Add(chunk);
                }

                if (candidates.Count == 0)
                    return false;

                MineralChunkSettings selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                chunks.Add(selected);
                remainingBudget -= selected.Amount;
            }

            return true;
        }

        private static bool CanFillBudget(int budget, IReadOnlyList<MineralChunkSettings> availableChunks)
        {
            if (budget == 0)
                return true;

            if (budget < 0 || availableChunks == null || availableChunks.Count == 0)
                return false;

            var fillable = new bool[budget + 1];
            fillable[0] = true;

            for (int value = 1; value <= budget; value++)
            {
                for (int i = 0; i < availableChunks.Count; i++)
                {
                    int amount = availableChunks[i].Amount;
                    if (value >= amount && fillable[value - amount])
                    {
                        fillable[value] = true;
                        break;
                    }
                }
            }

            return fillable[budget];
        }

        private bool TryFindMineralPosition(
            float innerRadius,
            float outerRadius,
            float startAngle,
            float endAngle,
            MineralChunkSettings chunk,
            IReadOnlyList<PlacedMineral> zonePlacedMinerals,
            out Vector3 position)
        {
            for (int i = 0; i < _maxRetryCount; i++)
            {
                position = SampleAnnularSectorPosition(innerRadius, outerRadius, startAngle, endAngle);
                if (!IsValidMineralPosition(position, chunk))
                    continue;

                if (!IsFarEnoughFromPlacedMinerals(position, chunk.MinDistance, zonePlacedMinerals))
                    continue;

                return true;
            }

            position = default;
            return false;
        }

        private static Vector3 SampleAnnularSectorPosition(
            float innerRadius,
            float outerRadius,
            float startAngle,
            float endAngle)
        {
            float innerRadiusSqr = innerRadius * innerRadius;
            float outerRadiusSqr = outerRadius * outerRadius;
            float radius = Mathf.Sqrt(UnityEngine.Random.Range(innerRadiusSqr, outerRadiusSqr));
            float angle = UnityEngine.Random.Range(startAngle, endAngle);

            return new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);
        }

        private bool IsFarEnoughFromPlacedMinerals(
            Vector3 position,
            float minDistance,
            IReadOnlyList<PlacedMineral> zonePlacedMinerals)
        {
            if (!IsFarEnoughFromPlacedMineralList(position, minDistance, _placedMinerals))
                return false;

            return IsFarEnoughFromPlacedMineralList(position, minDistance, zonePlacedMinerals);
        }

        private static bool IsFarEnoughFromPlacedMineralList(
            Vector3 position,
            float minDistance,
            IReadOnlyList<PlacedMineral> placedMinerals)
        {
            if (placedMinerals == null)
                return true;

            for (int i = 0; i < placedMinerals.Count; i++)
            {
                PlacedMineral placedMineral = placedMinerals[i];
                float requiredDistance = Mathf.Max(minDistance, placedMineral.MinDistance);
                if (Vector3.SqrMagnitude(position - placedMineral.Position) < requiredDistance * requiredDistance)
                    return false;
            }

            return true;
        }

        private bool TrySpawnMineral(MineralChunkSettings chunk, Vector3 position, out ResourceVisible resource)
        {
            resource = null;
            NetworkObject spawnedObject = Runner.Spawn(chunk.Prefab, position, Quaternion.identity);
            if (spawnedObject == null)
                return false;

            if (!spawnedObject.TryGetComponent(out resource))
            {
                Debug.LogWarning($"Spawned mineral prefab does not contain {nameof(ResourceVisible)}.");
                Runner.Despawn(spawnedObject);
                return false;
            }

            resource.Type = ResourceType.Mineral;
            resource.Amount = chunk.Amount;
            return true;
        }

        private void DespawnZoneObjects(IReadOnlyList<NetworkObject> zoneObjects)
        {
            for (int i = 0; i < zoneObjects.Count; i++)
            {
                NetworkObject zoneObject = zoneObjects[i];
                if (zoneObject != null && zoneObject.IsValid)
                    Runner.Despawn(zoneObject);
            }
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
            return IsValidResourcePosition(position);
        }

        private bool IsValidMineralPosition(Vector3 position, MineralChunkSettings chunk)
        {
            return IsValidResourcePosition(position) &&
                   !IsOverlappingWorldObstacle(position, chunk);
        }

        private bool IsValidResourcePosition(Vector3 position)
        {
            var xzPosition = new Vector2(position.x, position.z);
            return _territorySystem == null ||
                   _territorySystem.Territory == null ||
                   !_territorySystem.Territory.IsPointInPolygon(xzPosition);
        }

        private bool IsOverlappingWorldObstacle(Vector3 position, MineralChunkSettings chunk)
        {
            if (_worldObstacles == null || _worldObstacles.Count == 0)
                return false;

            float clearance = GetMineralObstacleClearance(chunk);

            for (int i = 0; i < _worldObstacles.Count; i++)
            {
                WorldObstacle obstacle = _worldObstacles[i];
                if (obstacle == null)
                    continue;

                Collider obstacleCollider = obstacle.Collider;
                if (obstacleCollider != null && IsNearObstacleCollider(position, clearance, obstacleCollider))
                    return true;

                if (obstacleCollider == null && IsNearBoundsXZ(position, clearance, obstacle.Bounds))
                    return true;
            }

            return false;
        }

        private float GetMineralObstacleClearance(MineralChunkSettings chunk)
        {
            float fallbackRadius = chunk != null ? chunk.MinDistance * 0.5f : 0f;
            float prefabRadius = chunk != null ? GetPrefabFootprintRadius(chunk.Prefab) : 0f;
            return Mathf.Max(_mineralObstaclePadding, fallbackRadius, prefabRadius);
        }

        private static bool IsNearObstacleCollider(Vector3 position, float clearance, Collider obstacleCollider)
        {
            Bounds bounds = obstacleCollider.bounds;
            if (!IsNearBoundsXZ(position, clearance, bounds))
                return false;

            Vector3 closestPoint = obstacleCollider.ClosestPoint(position);
            Vector2 positionXZ = new(position.x, position.z);
            Vector2 closestPointXZ = new(closestPoint.x, closestPoint.z);
            return (positionXZ - closestPointXZ).sqrMagnitude <= clearance * clearance;
        }

        private static bool IsNearBoundsXZ(Vector3 position, float clearance, Bounds bounds)
        {
            return position.x >= bounds.min.x - clearance &&
                   position.x <= bounds.max.x + clearance &&
                   position.z >= bounds.min.z - clearance &&
                   position.z <= bounds.max.z + clearance;
        }

        private static float GetPrefabFootprintRadius(GameObject prefab)
        {
            if (prefab == null)
                return 0f;

            Vector3 origin = prefab.transform.position;
            float radius = GetBoundsFootprintRadius(prefab.GetComponentsInChildren<Collider>(true), origin);
            if (radius > 0f)
                return radius;

            return GetBoundsFootprintRadius(prefab.GetComponentsInChildren<Renderer>(true), origin);
        }

        private static float GetBoundsFootprintRadius(IReadOnlyList<Collider> colliders, Vector3 origin)
        {
            float radiusSqr = 0f;
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                    continue;

                radiusSqr = Mathf.Max(radiusSqr, GetBoundsFootprintRadiusSqr(collider.bounds, origin));
            }

            return Mathf.Sqrt(radiusSqr);
        }

        private static float GetBoundsFootprintRadius(IReadOnlyList<Renderer> renderers, Vector3 origin)
        {
            float radiusSqr = 0f;
            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                radiusSqr = Mathf.Max(radiusSqr, GetBoundsFootprintRadiusSqr(renderer.bounds, origin));
            }

            return Mathf.Sqrt(radiusSqr);
        }

        private static float GetBoundsFootprintRadiusSqr(Bounds bounds, Vector3 origin)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            float radiusSqr = 0f;
            radiusSqr = Mathf.Max(radiusSqr, GetFootprintDistanceSqr(origin, min.x, min.z));
            radiusSqr = Mathf.Max(radiusSqr, GetFootprintDistanceSqr(origin, min.x, max.z));
            radiusSqr = Mathf.Max(radiusSqr, GetFootprintDistanceSqr(origin, max.x, min.z));
            radiusSqr = Mathf.Max(radiusSqr, GetFootprintDistanceSqr(origin, max.x, max.z));
            return radiusSqr;
        }

        private static float GetFootprintDistanceSqr(Vector3 origin, float x, float z)
        {
            float offsetX = x - origin.x;
            float offsetZ = z - origin.z;
            return offsetX * offsetX + offsetZ * offsetZ;
        }

        private void BindResource(ResourceVisible resource)
        {
            if (_territorySystem == null)
            {
                Debug.LogWarning($"{nameof(ResourceSpawnSystem)} cannot bind {resource.name}: territory system is missing.");
                return;
            }

            void HandleTerritoryExpanded(Territory territory, TerritorySystem territorySystem)
            {
                if (resource == null || resource.Object == null || !resource.Object.IsValid)
                {
                    _territorySystem.OnTerritoryExpandedEvent -= HandleTerritoryExpanded;
                    return;
                }

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
            if (_resourceSystem == null)
            {
                Debug.LogWarning($"Resource collection skipped because {nameof(ResourceSystem)} is missing.");
                return;
            }

            if (resource != null)
                resource.OnCollected -= HandleResourceCollected;

            switch (type)
            {
                case ResourceType.Mineral:
                    _resourceSystem.RPC_GetMineral(amount);
                    if (_resourceView != null)
                        _resourceView.SetMineral(_resourceSystem.Mineral);
                    break;
                case ResourceType.Gas:
                    _resourceSystem.RPC_GetGas(amount);
                    if (_resourceView != null)
                        _resourceView.SetGas(_resourceSystem.Gas);
                    break;
            }
            // Debug.Log($"Obtained {amount} {type} from {resource.gameObject.name}");
            if (resource != null && resource.Object != null && resource.Object.IsValid)
                Runner.Despawn(resource.Object);
        }
    }
}
