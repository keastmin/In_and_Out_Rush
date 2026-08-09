using System.Collections.Generic;
using Fusion;
using UnityEngine;
using KIM.Dev;

namespace Dev.Network
{
    public class ResourceSpawnSystem : System, IWorldObstacleConsumer
    {
        private readonly struct PlacedResource
        {
            public readonly Vector3 Position;
            public readonly float MinDistance;

            public PlacedResource(Vector3 position, float minDistance)
            {
                Position = position;
                MinDistance = minDistance;
            }
        }

        [SerializeField] private ResourceSystem _resourceSystem;
        [SerializeField] private TerritorySystem _territorySystem;
        [SerializeField] private Local.ResourceView _resourceView;

        [Header("Resource Settings")]
        [SerializeField] private ResourcePlacementSettings _mineralPlacementSettings;
        [SerializeField] private ResourcePlacementSettings _gasPlacementSettings;
        [SerializeField] private NetworkObject _resourceZonePrefab;
        [SerializeField] private int _randomSeed = 20260808;

        [Header("Spawn Area")]
        [SerializeField, Min(0f)] private float _worldSpawnRadius = 250f;
        [SerializeField, Min(1)] private int _maxRetryCount = 10;

        [Header("Obstacle Avoidance")]
        [SerializeField] private InfiniteGridRockSpawner _rockSpawner;
        [SerializeField, Min(0f)] private float _mineralObstaclePadding = 0f;

        private readonly List<PlacedResource> _placedResources = new();
        private readonly List<ResourceZone> _resourceZones = new();
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

            BindTerritoryExpansion();

            GenerateResources();
        }

        protected override void OnTearDown()
        {
            if (_territorySystem != null)
                _territorySystem.OnTerritoryExpandedEvent -= HandleTerritoryExpanded;

            base.OnTearDown();
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
            DespawnGeneratedZones();
            _placedResources.Clear();
            _resourceZones.Clear();
            SpawnResourcesByZone(_mineralPlacementSettings);
            SpawnResourcesByZone(_gasPlacementSettings);
        }

        private void SpawnResourcesByZone(ResourcePlacementSettings settings)
        {
            if (settings == null || settings.ZoneBudget <= 0 || _worldSpawnRadius <= 0f)
                return;

            IReadOnlyList<ResourceChunkPlacementSettings> availableChunks = settings.GetAvailableChunks();
            if (availableChunks.Count == 0)
            {
                Debug.LogWarning($"{settings.ResourceType} zone spawn skipped: no resource prefab is assigned.");
                return;
            }

            if (!TryValidateResourceChunks(settings.ResourceType, availableChunks))
                return;

            int sectorCount = settings.SectorCount;
            int ringCount = settings.RingCount;
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

                    List<ResourceChunkPlacementSettings> zoneChunks = CreateResourceBudgetPlan(
                        settings.ZoneBudget,
                        availableChunks);
                    if (zoneChunks.Count == 0)
                    {
                        skippedZoneCount++;
                        Debug.LogWarning(
                            $"{settings.ResourceType} zone skipped. No resource chunks fit within the zone budget. " +
                            $"Ring: {ringIndex}, Sector: {sectorIndex}, Budget: {settings.ZoneBudget}.");
                        continue;
                    }

                    var zonePlacedResources = new List<PlacedResource>(zoneChunks.Count);
                    var zoneEntries = new List<ResourceZoneEntry>(zoneChunks.Count);

                    for (int i = 0; i < zoneChunks.Count; i++)
                    {
                        ResourceChunkPlacementSettings chunk = zoneChunks[i];
                        if (!TryFindResourcePosition(
                                innerRadius,
                                outerRadius,
                                startAngle,
                                endAngle,
                                chunk,
                                zonePlacedResources,
                                out Vector3 position))
                        {
                            Debug.LogWarning(
                                $"Failed to place {settings.ResourceType} in zone after {_maxRetryCount} attempts. " +
                                $"Ring: {ringIndex}, Sector: {sectorIndex}, Amount: {chunk.Amount}.");
                            continue;
                        }

                        zonePlacedResources.Add(new PlacedResource(position, chunk.MinDistance));
                        zoneEntries.Add(new ResourceZoneEntry(position, chunk.Amount));
                    }

                    if (zoneEntries.Count == 0)
                    {
                        skippedZoneCount++;
                        continue;
                    }

                    if (!SpawnResourceZone(
                        settings.ResourceType,
                        ringIndex,
                        sectorIndex,
                        zoneEntries))
                    {
                        skippedZoneCount++;
                        continue;
                    }

                    for (int i = 0; i < zoneEntries.Count; i++)
                    {
                        _placedResources.Add(zonePlacedResources[i]);
                        spawnedCount++;
                        spawnedAmount += zoneEntries[i].Amount;
                    }
                }
            }

            Debug.Log(
                $"{settings.ResourceType} zone spawn completed: {spawnedCount} chunks, {spawnedAmount} total amount, " +
                $"{skippedZoneCount}/{ringCount * sectorCount} zones skipped.");
        }

        private static bool TryValidateResourceChunks(
            ResourceType resourceType,
            IReadOnlyList<ResourceChunkPlacementSettings> chunks)
        {
            for (int i = 0; i < chunks.Count; i++)
            {
                ResourceChunkPlacementSettings chunk = chunks[i];
                if (!chunk.Prefab.TryGetComponent<ResourceVisible>(out _))
                {
                    Debug.LogWarning(
                        $"{resourceType} prefab {chunk.Prefab.name} does not contain {nameof(ResourceVisible)}.");
                    return false;
                }
            }

            return true;
        }

        private static List<ResourceChunkPlacementSettings> CreateResourceBudgetPlan(
            int budget,
            IReadOnlyList<ResourceChunkPlacementSettings> availableChunks)
        {
            var chunks = new List<ResourceChunkPlacementSettings>();
            if (budget <= 0 || availableChunks == null || availableChunks.Count == 0)
                return chunks;

            bool[] fillableBudgets = CreateFillableBudgetTable(budget, availableChunks);
            int remainingBudget = GetMaxFillableBudget(fillableBudgets);
            if (remainingBudget <= 0)
                return chunks;

            while (remainingBudget > 0)
            {
                var candidates = new List<ResourceChunkPlacementSettings>();
                for (int i = 0; i < availableChunks.Count; i++)
                {
                    ResourceChunkPlacementSettings chunk = availableChunks[i];
                    int nextBudget = remainingBudget - chunk.Amount;
                    if (nextBudget >= 0 && fillableBudgets[nextBudget])
                        candidates.Add(chunk);
                }

                if (candidates.Count == 0)
                    break;

                ResourceChunkPlacementSettings selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                chunks.Add(selected);
                remainingBudget -= selected.Amount;
            }

            return chunks;
        }

        private static bool[] CreateFillableBudgetTable(
            int budget,
            IReadOnlyList<ResourceChunkPlacementSettings> availableChunks)
        {
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

            return fillable;
        }

        private static int GetMaxFillableBudget(IReadOnlyList<bool> fillableBudgets)
        {
            for (int budget = fillableBudgets.Count - 1; budget >= 0; budget--)
            {
                if (fillableBudgets[budget])
                    return budget;
            }

            return 0;
        }

        private bool TryFindResourcePosition(
            float innerRadius,
            float outerRadius,
            float startAngle,
            float endAngle,
            ResourceChunkPlacementSettings chunk,
            IReadOnlyList<PlacedResource> zonePlacedResources,
            out Vector3 position)
        {
            for (int i = 0; i < _maxRetryCount; i++)
            {
                position = SampleAnnularSectorPosition(innerRadius, outerRadius, startAngle, endAngle);
                if (!IsValidResourcePosition(position, chunk))
                    continue;

                if (!IsFarEnoughFromPlacedResources(position, chunk.MinDistance, zonePlacedResources))
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

        private bool IsFarEnoughFromPlacedResources(
            Vector3 position,
            float minDistance,
            IReadOnlyList<PlacedResource> zonePlacedResources)
        {
            if (!IsFarEnoughFromPlacedResourceList(position, minDistance, _placedResources))
                return false;

            return IsFarEnoughFromPlacedResourceList(position, minDistance, zonePlacedResources);
        }

        private static bool IsFarEnoughFromPlacedResourceList(
            Vector3 position,
            float minDistance,
            IReadOnlyList<PlacedResource> placedResources)
        {
            if (placedResources == null)
                return true;

            for (int i = 0; i < placedResources.Count; i++)
            {
                PlacedResource placedResource = placedResources[i];
                float requiredDistance = Mathf.Max(minDistance, placedResource.MinDistance);
                if (Vector3.SqrMagnitude(position - placedResource.Position) < requiredDistance * requiredDistance)
                    return false;
            }

            return true;
        }

        private bool SpawnResourceZone(
            ResourceType resourceType,
            int ringIndex,
            int sectorIndex,
            IReadOnlyList<ResourceZoneEntry> entries)
        {
            if (_resourceZonePrefab == null)
            {
                Debug.LogError(
                    $"{nameof(ResourceSpawnSystem)} requires a {nameof(ResourceZone)} prefab.",
                    this);
                return false;
            }

            Vector3 zonePosition = CalculateZoneCenter(resourceType, ringIndex, sectorIndex);
            int seed = CreateZoneSeed(resourceType, ringIndex, sectorIndex);
            NetworkObject spawnedObject = Runner.Spawn(
                _resourceZonePrefab,
                zonePosition,
                Quaternion.identity,
                PlayerRef.None,
                (_, networkObject) =>
                {
                    if (!networkObject.TryGetComponent(out ResourceZone resourceZone))
                        return;

                    resourceZone.InitializeState(resourceType, seed, entries);
                });

            if (spawnedObject == null)
                return false;

            if (!spawnedObject.TryGetComponent(out ResourceZone spawnedZone))
            {
                Debug.LogError(
                    $"Resource zone prefab {_resourceZonePrefab.name} does not contain {nameof(ResourceZone)}.",
                    _resourceZonePrefab);
                Runner.Despawn(spawnedObject);
                return false;
            }

            _resourceZones.Add(spawnedZone);
            return true;
        }

        private Vector3 CalculateZoneCenter(
            ResourceType resourceType,
            int ringIndex,
            int sectorIndex)
        {
            ResourcePlacementSettings settings = resourceType == ResourceType.Mineral
                ? _mineralPlacementSettings
                : _gasPlacementSettings;

            if (settings == null)
                return Vector3.zero;

            float innerRadius = _worldSpawnRadius * Mathf.Sqrt((float)ringIndex / settings.RingCount);
            float outerRadius = _worldSpawnRadius * Mathf.Sqrt((float)(ringIndex + 1) / settings.RingCount);
            float startAngle = Mathf.PI * 2f * sectorIndex / settings.SectorCount;
            float endAngle = Mathf.PI * 2f * (sectorIndex + 1) / settings.SectorCount;
            float radius = (innerRadius + outerRadius) * 0.5f;
            float angle = (startAngle + endAngle) * 0.5f;

            return new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);
        }

        private int CreateZoneSeed(ResourceType resourceType, int ringIndex, int sectorIndex)
        {
            unchecked
            {
                int seed = _randomSeed;
                seed = seed * 31 + (int)resourceType;
                seed = seed * 31 + ringIndex;
                seed = seed * 31 + sectorIndex;
                return seed;
            }
        }

        private void BindTerritoryExpansion()
        {
            if (_territorySystem == null)
                return;

            _territorySystem.OnTerritoryExpandedEvent -= HandleTerritoryExpanded;
            _territorySystem.OnTerritoryExpandedEvent += HandleTerritoryExpanded;
        }

        private void HandleTerritoryExpanded(Territory territory, TerritorySystem sender)
        {
            if (!HasStateAuthority || territory == null)
                return;

            int mineralAmount = 0;
            int gasAmount = 0;

            for (int i = 0; i < _resourceZones.Count; i++)
            {
                ResourceZone resourceZone = _resourceZones[i];
                if (resourceZone == null)
                    continue;

                int collectedAmount = resourceZone.CollectWithin(territory);
                if (resourceZone.ResourceType == ResourceType.Mineral)
                    mineralAmount += collectedAmount;
                else
                    gasAmount += collectedAmount;
            }

            if (_resourceSystem == null)
            {
                if (mineralAmount > 0 || gasAmount > 0)
                {
                    Debug.LogWarning(
                        $"Resource collection skipped because {nameof(ResourceSystem)} is missing.",
                        this);
                }

                return;
            }

            if (mineralAmount > 0)
            {
                _resourceSystem.RPC_GetMineral(mineralAmount);
                _resourceView?.SetMineral(_resourceSystem.Mineral);
            }

            if (gasAmount > 0)
            {
                _resourceSystem.RPC_GetGas(gasAmount);
                _resourceView?.SetGas(_resourceSystem.Gas);
            }
        }

        private void DespawnGeneratedZones()
        {
            if (!HasStateAuthority || Runner == null)
                return;

            for (int i = _resourceZones.Count - 1; i >= 0; i--)
            {
                ResourceZone resourceZone = _resourceZones[i];
                if (resourceZone != null && resourceZone.Object != null && resourceZone.Object.IsValid)
                    Runner.Despawn(resourceZone.Object);
            }
        }

        private bool IsValidResourcePosition(Vector3 position, ResourceChunkPlacementSettings chunk)
        {
            return IsOutsideTerritory(position) &&
                   !IsOverlappingWorldObstacle(position, chunk);
        }

        private bool IsOutsideTerritory(Vector3 position)
        {
            var xzPosition = new Vector2(position.x, position.z);
            return _territorySystem == null ||
                   _territorySystem.Territory == null ||
                   !_territorySystem.Territory.IsPointInPolygon(xzPosition);
        }

        private bool IsOverlappingWorldObstacle(Vector3 position, ResourceChunkPlacementSettings chunk)
        {
            if (_worldObstacles == null || _worldObstacles.Count == 0)
                return false;

            float clearance = GetObstacleClearance(chunk);

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

        private float GetObstacleClearance(ResourceChunkPlacementSettings chunk)
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

    }
}
