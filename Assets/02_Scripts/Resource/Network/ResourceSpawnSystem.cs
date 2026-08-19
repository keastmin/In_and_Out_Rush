using System.Collections.Generic;
using Fusion;
using ProjectIO.ResourceSpawn;
using Unity.Profiling;
using UnityEngine;
using KIM.Dev;
using PlacedResource = ProjectIO.ResourceSpawn.ResourceCandidatePlacementPolicy.PlacedResource;

namespace Dev.Network
{
    public class ResourceSpawnSystem : System, IWorldObstacleConsumer
    {
        private static readonly ProfilerMarker FixedUpdateMarker = new("ResourceSpawnSystem.FixedUpdateNetwork");

        private const float DefaultResourceInterestRadius = 128f;
        private const float DefaultResourceInterestRefreshInterval = 0.25f;

        [SerializeField] private ResourceSystem _resourceSystem;
        [SerializeField] private TerritorySystem _territorySystem;
        [SerializeField] private Local.ResourceView _resourceView;

        [Header("Resource Settings")]
        [SerializeField] private ResourcePlacementSettings _mineralPlacementSettings;
        [SerializeField] private ResourcePlacementSettings _gasPlacementSettings;
        // Kept for compatibility with the existing Core prefab and editor builder.
        // Resource generation now spawns the individual resource prefabs below.
        [SerializeField] private NetworkObject _resourceZonePrefab;

        [Header("Spawn Area")]
        [SerializeField, Min(0f)] private float _worldSpawnRadius = 250f;
        [SerializeField, Min(1)] private int _maxRetryCount = 10;

        [Header("Client Resource Visibility")]
        [SerializeField, Min(0.1f)] private float _resourceInterestRadius = 128f;
        [SerializeField, Min(0.05f)] private float _resourceInterestRefreshInterval = 0.25f;

        [Header("Obstacle Avoidance")]
        [SerializeField] private InfiniteGridObstacleSpawner _obstacleSpawner;
        [SerializeField, Min(0f)] private float _mineralObstaclePadding = 0f;

        private readonly List<PlacedResource> _placedResources = new();
        private readonly List<float> _obstacleDistancesSquared = new();
        private readonly List<ResourceVisible> _spawnedResources = new();
        private readonly Dictionary<ResourceVisible, HashSet<PlayerRef>> _forcedInterestPlayers = new();
        private IReadOnlyList<WorldObstacle> _worldObstacles;
        private Vector3 _startPosition;
        private bool _hasStartPosition;
        private TickTimer _resourceInterestRefreshTimer;

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
            RefreshClientResourceInterest();
            _resourceInterestRefreshTimer = TickTimer.CreateFromSeconds(
                Runner,
                GetResourceInterestRefreshInterval());
        }

        protected override void OnTearDown()
        {
            if (_territorySystem != null)
                _territorySystem.OnTerritoryExpandedEvent -= HandleTerritoryExpanded;

            DespawnGeneratedResources();
            _forcedInterestPlayers.Clear();
            _resourceInterestRefreshTimer = default;

            base.OnTearDown();
        }

        public override void FixedUpdateNetwork()
        {
            using (FixedUpdateMarker.Auto())
            {
                if (Object == null || !Object.HasStateAuthority || !_resourceInterestRefreshTimer.ExpiredOrNotRunning(Runner))
                    return;

                RefreshClientResourceInterest();
                _resourceInterestRefreshTimer = TickTimer.CreateFromSeconds(
                    Runner,
                    GetResourceInterestRefreshInterval());
            }
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
                _territorySystem = UnityEngine.Object.FindFirstObjectByType<TerritorySystem>(FindObjectsInactive.Include);

            if (_resourceView == null)
                _resourceView = UnityEngine.Object.FindFirstObjectByType<Local.ResourceView>(FindObjectsInactive.Include);

            ResolveWorldObstacles();

            if (_resourceSystem == null)
                Debug.LogWarning($"{nameof(ResourceSpawnSystem)} has no {nameof(ResourceSystem)} reference.");
        }

        private void ResolveWorldObstacles()
        {
            if (_obstacleSpawner == null && StageBootstrapper.Instance != null && StageBootstrapper.Instance.Grid != null)
                StageBootstrapper.Instance.Grid.TryGetComponent(out _obstacleSpawner);

            if (_obstacleSpawner == null)
                _obstacleSpawner = UnityEngine.Object.FindFirstObjectByType<InfiniteGridObstacleSpawner>(FindObjectsInactive.Include);

            if (_worldObstacles == null && _obstacleSpawner != null)
                InitializeWorldObstacles(_obstacleSpawner.SpawnedObstacles);
        }

        public void GenerateResources()
        {
            if (!HasStateAuthority)
                return;

            DespawnGeneratedResources();
            _placedResources.Clear();
            _spawnedResources.Clear();
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
                    var zoneResources = new List<ResourceVisible>(zoneChunks.Count);

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

                        if (!TrySpawnResource(
                                settings.ResourceType,
                                chunk,
                                position,
                                out ResourceVisible resource))
                        {
                            continue;
                        }

                        zonePlacedResources.Add(new PlacedResource(position.x, position.z, chunk.MinDistance));
                        zoneResources.Add(resource);
                    }

                    if (zoneResources.Count == 0)
                    {
                        skippedZoneCount++;
                        continue;
                    }

                    for (int i = 0; i < zoneResources.Count; i++)
                    {
                        RegisterSpawnedResource(zoneResources[i]);
                        _placedResources.Add(zonePlacedResources[i]);
                        spawnedCount++;
                        spawnedAmount += zoneResources[i].Amount;
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
                if (chunk == null || chunk.Prefab == null)
                {
                    Debug.LogWarning(
                        $"{resourceType} resource chunk is missing its prefab.");
                    return false;
                }

                if (!chunk.Prefab.TryGetComponent<NetworkObject>(out _) ||
                    !chunk.Prefab.TryGetComponent<ResourceVisible>(out _))
                {
                    Debug.LogWarning(
                        $"{resourceType} prefab {chunk.Prefab.name} must contain both " +
                        $"{nameof(NetworkObject)} and {nameof(ResourceVisible)}.");
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

            var amounts = new int[availableChunks.Count];
            for (int i = 0; i < availableChunks.Count; i++)
                amounts[i] = availableChunks[i].Amount;

            IReadOnlyList<int> selectedIndexes = ResourceBudgetPlanner.CreatePlan(
                budget,
                amounts,
                candidateCount => UnityEngine.Random.Range(0, candidateCount));

            for (int i = 0; i < selectedIndexes.Count; i++)
                chunks.Add(availableChunks[selectedIndexes[i]]);

            return chunks;
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
                if (!IsOutsideTerritory(position))
                    continue;

                if (!IsCandidatePlacementValid(position, chunk, zonePlacedResources))
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

        private bool TrySpawnResource(
            ResourceType resourceType,
            ResourceChunkPlacementSettings chunk,
            Vector3 position,
            out ResourceVisible resource)
        {
            resource = null;
            if (Runner == null || !HasStateAuthority || chunk == null || chunk.Prefab == null)
                return false;

            if (!chunk.Prefab.TryGetComponent(out NetworkObject resourcePrefab))
            {
                Debug.LogWarning(
                    $"{resourceType} prefab {chunk.Prefab.name} does not contain {nameof(NetworkObject)}.",
                    chunk.Prefab);
                return false;
            }

            NetworkObject spawnedObject = Runner.Spawn(
                resourcePrefab,
                position,
                Quaternion.identity,
                PlayerRef.None);
            if (spawnedObject == null)
                return false;

            if (!spawnedObject.TryGetComponent(out resource))
            {
                Debug.LogWarning(
                    $"Spawned {resourceType} prefab {chunk.Prefab.name} does not contain " +
                    $"{nameof(ResourceVisible)}.",
                    chunk.Prefab);
                Runner.Despawn(spawnedObject);
                return false;
            }

            resource.Type = resourceType;
            resource.Amount = chunk.Amount;
            return true;
        }

        private void RegisterSpawnedResource(ResourceVisible resource)
        {
            if (resource == null)
                return;

            resource.OnCollected -= HandleResourceCollected;
            resource.OnCollected += HandleResourceCollected;
            _spawnedResources.Add(resource);
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

            for (int i = _spawnedResources.Count - 1; i >= 0; i--)
            {
                ResourceVisible resource = _spawnedResources[i];
                if (!IsSpawnedResourceValid(resource))
                {
                    RemoveSpawnedResource(resource);
                    continue;
                }

                Vector2 resourcePosition = new(resource.transform.position.x, resource.transform.position.z);
                if (territory.IsPointInPolygon(resourcePosition))
                    resource.Collect();
            }
        }

        private void HandleResourceCollected(
            ResourceType type,
            int amount,
            ResourceVisible resource,
            object context)
        {
            RemoveSpawnedResource(resource);

            if (_resourceSystem == null)
            {
                Debug.LogWarning(
                    $"Resource collection skipped because {nameof(ResourceSystem)} is missing.",
                    this);
            }
            else
            {
                switch (type)
                {
                    case ResourceType.Mineral:
                        _resourceSystem.RPC_GetMineral(amount);
                        _resourceView?.SetMineral(_resourceSystem.Mineral);
                        break;
                    case ResourceType.Gas:
                        _resourceSystem.RPC_GetGas(amount);
                        _resourceView?.SetGas(_resourceSystem.Gas);
                        break;
                }
            }

            if (resource != null &&
                resource.Object != null &&
                resource.Object.IsValid &&
                Runner != null &&
                HasStateAuthority)
            {
                Runner.Despawn(resource.Object);
            }
        }

        private void DespawnGeneratedResources()
        {
            bool canDespawn = HasStateAuthority && Runner != null;
            for (int i = _spawnedResources.Count - 1; i >= 0; i--)
            {
                ResourceVisible resource = _spawnedResources[i];
                if (resource != null)
                {
                    resource.OnCollected -= HandleResourceCollected;

                    if (canDespawn && resource.Object != null && resource.Object.IsValid)
                        Runner.Despawn(resource.Object);
                }
            }

            _spawnedResources.Clear();
            _forcedInterestPlayers.Clear();
        }

        private void RemoveSpawnedResource(ResourceVisible resource)
        {
            if (resource != null)
            {
                resource.OnCollected -= HandleResourceCollected;
                _forcedInterestPlayers.Remove(resource);
            }

            _spawnedResources.Remove(resource);
        }

        private static bool IsSpawnedResourceValid(ResourceVisible resource)
        {
            return resource != null &&
                   resource.Object != null &&
                   resource.Object.IsValid;
        }

        private void RefreshClientResourceInterest()
        {
            if (Runner == null)
                return;

            float interestRadius = GetResourceInterestRadius();
            float interestRadiusSqr = interestRadius * interestRadius;
            var activePlayers = new HashSet<PlayerRef>();
            foreach (PlayerRef player in Runner.ActivePlayers)
                activePlayers.Add(player);

            for (int resourceIndex = _spawnedResources.Count - 1; resourceIndex >= 0; resourceIndex--)
            {
                ResourceVisible resource = _spawnedResources[resourceIndex];
                if (!IsSpawnedResourceValid(resource))
                {
                    RemoveSpawnedResource(resource);
                    continue;
                }

                if (!_forcedInterestPlayers.TryGetValue(resource, out HashSet<PlayerRef> forcedPlayers))
                {
                    forcedPlayers = new HashSet<PlayerRef>();
                    _forcedInterestPlayers.Add(resource, forcedPlayers);
                }

                foreach (PlayerRef player in activePlayers)
                {
                    if (!TryGetPlayerPosition(player, out Vector3 playerPosition))
                    {
                        if (forcedPlayers.Remove(player))
                            resource.Object.SetPlayerAlwaysInterested(player, false);

                        continue;
                    }

                    Vector3 offset = resource.transform.position - playerPosition;
                    offset.y = 0f;
                    bool shouldForceInterest = offset.sqrMagnitude <= interestRadiusSqr;

                    if (shouldForceInterest)
                    {
                        if (forcedPlayers.Add(player))
                            resource.Object.SetPlayerAlwaysInterested(player, true);
                    }
                    else if (forcedPlayers.Remove(player))
                    {
                        resource.Object.SetPlayerAlwaysInterested(player, false);
                    }
                }

                if (forcedPlayers.Count == 0)
                    continue;

                var inactivePlayers = new List<PlayerRef>();
                foreach (PlayerRef player in forcedPlayers)
                {
                    if (!activePlayers.Contains(player))
                        inactivePlayers.Add(player);
                }

                for (int i = 0; i < inactivePlayers.Count; i++)
                {
                    PlayerRef player = inactivePlayers[i];
                    resource.Object.SetPlayerAlwaysInterested(player, false);
                    forcedPlayers.Remove(player);
                }
            }
        }

        private bool TryGetPlayerPosition(PlayerRef player, out Vector3 playerPosition)
        {
            playerPosition = default;
            if (Runner == null ||
                !Runner.TryGetPlayerObject(player, out NetworkObject playerObject) ||
                playerObject == null)
            {
                return false;
            }

            playerPosition = playerObject.transform.position;
            return true;
        }

        private float GetResourceInterestRadius()
        {
            return _resourceInterestRadius > 0f
                ? _resourceInterestRadius
                : DefaultResourceInterestRadius;
        }

        private float GetResourceInterestRefreshInterval()
        {
            return _resourceInterestRefreshInterval > 0f
                ? _resourceInterestRefreshInterval
                : DefaultResourceInterestRefreshInterval;
        }

        private bool IsOutsideTerritory(Vector3 position)
        {
            var xzPosition = new Vector2(position.x, position.z);
            return _territorySystem == null ||
                   _territorySystem.Territory == null ||
                   !_territorySystem.Territory.IsPointInPolygon(xzPosition);
        }

        private bool IsCandidatePlacementValid(
            Vector3 position,
            ResourceChunkPlacementSettings chunk,
            IReadOnlyList<PlacedResource> zonePlacedResources)
        {
            float clearance = GetObstacleClearance(chunk);
            CollectObstacleDistancesSquared(position, clearance);

            return ResourceCandidatePlacementPolicy.IsCandidateValid(
                position.x,
                position.z,
                chunk.MinDistance,
                clearance,
                _obstacleDistancesSquared,
                _placedResources,
                zonePlacedResources);
        }

        private void CollectObstacleDistancesSquared(Vector3 position, float clearance)
        {
            _obstacleDistancesSquared.Clear();
            if (_worldObstacles == null)
                return;

            for (int i = 0; i < _worldObstacles.Count; i++)
            {
                WorldObstacle obstacle = _worldObstacles[i];
                if (obstacle == null)
                    continue;

                Collider obstacleCollider = obstacle.Collider;
                float distanceSquared = obstacleCollider != null
                    ? GetObstacleColliderDistanceSquared(position, clearance, obstacleCollider)
                    : GetBoundsDistanceSquaredXZ(position, obstacle.Bounds);
                _obstacleDistancesSquared.Add(distanceSquared);
            }
        }

        private float GetObstacleClearance(ResourceChunkPlacementSettings chunk)
        {
            float fallbackRadius = chunk != null ? chunk.MinDistance * 0.5f : 0f;
            float prefabRadius = chunk != null ? GetPrefabFootprintRadius(chunk.Prefab) : 0f;
            return Mathf.Max(_mineralObstaclePadding, fallbackRadius, prefabRadius);
        }

        private static float GetObstacleColliderDistanceSquared(
            Vector3 position,
            float clearance,
            Collider obstacleCollider)
        {
            Bounds bounds = obstacleCollider.bounds;
            if (!IsWithinExpandedBoundsXZ(position, clearance, bounds))
                return float.PositiveInfinity;

            Vector3 closestPoint = obstacleCollider.ClosestPoint(position);
            float offsetX = position.x - closestPoint.x;
            float offsetZ = position.z - closestPoint.z;
            return offsetX * offsetX + offsetZ * offsetZ;
        }

        private static bool IsWithinExpandedBoundsXZ(Vector3 position, float clearance, Bounds bounds)
        {
            return position.x >= bounds.min.x - clearance &&
                   position.x <= bounds.max.x + clearance &&
                   position.z >= bounds.min.z - clearance &&
                   position.z <= bounds.max.z + clearance;
        }

        private static float GetBoundsDistanceSquaredXZ(Vector3 position, Bounds bounds)
        {
            float offsetX = Mathf.Max(bounds.min.x - position.x, 0f, position.x - bounds.max.x);
            float offsetZ = Mathf.Max(bounds.min.z - position.z, 0f, position.z - bounds.max.z);
            float maxOffset = Mathf.Max(offsetX, offsetZ);
            return maxOffset * maxOffset;
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
