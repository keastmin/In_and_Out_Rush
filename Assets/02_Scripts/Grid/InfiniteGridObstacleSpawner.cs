using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class InfiniteGridObstacleSpawner : MonoBehaviour
    {
        [Header("Obstacle Spawn Data")]
        [SerializeField] private ObstacleSpawnData[] _obstacleSpawnData;

        [Header("Distribution")]
        [SerializeField, Min(0f)] private float _maximumHalfExtent = 220f;
        [SerializeField, Range(0f, 0.45f)] private float _edgePaddingRatio = 0.08f;
        [SerializeField, Min(0f)] private float _centerExclusionRadius = 40f;
        [SerializeField, Range(0f, 0.45f)] private float _cellJitterRatio = 0.2f;
        [SerializeField, Min(1)] private int _placementRetryCount = 24;
        [SerializeField, Min(0f)] private float _minimumSpacing = 24f;
        [SerializeField] private int _randomSeed = 20260720;

        private readonly List<ObstacleSpawnData> _validObstacleSpawnData = new();
        private readonly List<Vector3> _spawnedPositions = new();
        private readonly List<WorldObstacle> _spawnedObstacles = new();
        private InfiniteGrid _grid;
        private MeshRenderer _groundRenderer;
        private bool _spawnStarted;

        public IReadOnlyList<WorldObstacle> SpawnedObstacles => _spawnedObstacles;

        public void DespawnObstaclesOverlappingTrack(NetworkRunner runner, IReadOnlyList<Vector3> trackVertices, float trackLineWidth)
        {
            if (runner == null || trackVertices == null || trackVertices.Count < 2)
                return;

            float trackRadius = Mathf.Max(0f, trackLineWidth * 0.5f);
            for (int i = _spawnedObstacles.Count - 1; i >= 0; i--)
            {
                WorldObstacle obstacle = _spawnedObstacles[i];
                if (!ShouldDespawnObstacle(obstacle, trackVertices, trackRadius))
                    continue;

                DespawnObstacle(runner, obstacle);
                _spawnedObstacles.RemoveAt(i);
            }
        }

        public void DespawnObstaclesOverlappingTerritory(NetworkRunner runner, Territory territory)
        {
            if (runner == null || territory == null || territory.Vertices == null || territory.Vertices.Count < 3)
                return;

            for (int i = _spawnedObstacles.Count - 1; i >= 0; i--)
            {
                WorldObstacle obstacle = _spawnedObstacles[i];
                if (!ShouldDespawnObstacle(obstacle, territory))
                    continue;

                DespawnObstacle(runner, obstacle);
                _spawnedObstacles.RemoveAt(i);
            }
        }

        public void SpawnObstacles()
        {
            _grid = GetComponent<InfiniteGrid>();
            _groundRenderer = GetComponent<MeshRenderer>();

            if (_grid == null || _groundRenderer == null)
            {
                Debug.LogError("Infinite Grid 바위 배치에 필요한 InfiniteGrid 또는 MeshRenderer를 찾을 수 없습니다.");
                return;
            }

            if (_grid.Object == null || !_grid.Object.IsValid)
            {
                Debug.LogWarning("Infinite Grid NetworkObject가 준비되기 전이므로 바위를 스폰할 수 없습니다.");
                return;
            }

            if (!_grid.HasStateAuthority)
                return;

            SpawnObstaclesInternal();
        }

        private void SpawnObstaclesInternal()
        {
            if (_spawnStarted || _grid.Runner == null)
                return;

            _spawnStarted = true;
            CollectValidSpawnData();
            if (_validObstacleSpawnData.Count == 0)
            {
                Debug.LogError("Infinite Grid에 배치할 네트워크 바위 프리팹이 없습니다.");
                return;
            }

            Bounds groundBounds = _groundRenderer.bounds;
            float usableHalfExtentX = GetUsableHalfExtent(groundBounds.extents.x);
            float usableHalfExtentZ = GetUsableHalfExtent(groundBounds.extents.z);
            if (usableHalfExtentX <= 0f || usableHalfExtentZ <= 0f)
            {
                Debug.LogError("Infinite Grid Ground의 배치 가능 범위가 올바르지 않습니다.");
                return;
            }

            var random = new System.Random(_randomSeed);
            _spawnedPositions.Clear();
            _spawnedObstacles.Clear();

            int spawnCount = GetTotalSpawnCount();
            int columnCount = Mathf.CeilToInt(Mathf.Sqrt(spawnCount));
            int rowCount = Mathf.CeilToInt((float)spawnCount / columnCount);
            float cellWidth = usableHalfExtentX * 2f / columnCount;
            float cellDepth = usableHalfExtentZ * 2f / rowCount;

            for (int row = 0; row < rowCount; row++)
            {
                for (int column = 0; column < columnCount; column++)
                {
                    int spawnIndex = row * columnCount + column;
                    if (spawnIndex >= spawnCount)
                        continue;

                    ObstacleSpawnData spawnData = GetSpawnDataForIndex(spawnIndex);
                    NetworkObject prefab = spawnData.Prefab;
                    if (!TryFindSpawnPosition(
                            random,
                            groundBounds,
                            usableHalfExtentX,
                            usableHalfExtentZ,
                            row,
                            column,
                            rowCount,
                            columnCount,
                            cellWidth,
                            cellDepth,
                            spawnData.HeightOffset,
                            out Vector3 spawnPosition))
                    {
                        Debug.LogWarning($"{prefab.name}의 바위 배치 위치를 찾지 못했습니다.");
                        continue;
                    }

                    float yaw = NextRange(random, 0f, 360f);
                    Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
                    int instanceNumber = row + 1;
                    NetworkObject spawnedObstacle = _grid.Runner.Spawn(
                        prefab,
                        spawnPosition,
                        rotation,
                        PlayerRef.None,
                        (_, spawnedObject) => spawnedObject.name = $"{prefab.name}_{instanceNumber:00}");

                    if (spawnedObstacle == null)
                        continue;

                    _spawnedPositions.Add(spawnPosition);
                    if (TryGetWorldObstacle(spawnedObstacle, out WorldObstacle worldObstacle))
                    {
                        _spawnedObstacles.Add(worldObstacle);
                    }
                    else
                    {
                        Debug.LogError($"{spawnedObstacle.name}에 WorldObstacle 컴포넌트가 없어 장애물 목록에 등록할 수 없습니다.", spawnedObstacle);
                    }
                }
            }
        }

        private void CollectValidSpawnData()
        {
            _validObstacleSpawnData.Clear();
            if (_obstacleSpawnData == null)
                return;

            for (int i = 0; i < _obstacleSpawnData.Length; i++)
            {
                ObstacleSpawnData spawnData = _obstacleSpawnData[i];
                if (spawnData != null && spawnData.IsAvailable)
                    _validObstacleSpawnData.Add(spawnData);
            }
        }

        private int GetTotalSpawnCount()
        {
            int totalSpawnCount = 0;
            for (int i = 0; i < _validObstacleSpawnData.Count; i++)
                totalSpawnCount += _validObstacleSpawnData[i].SpawnCount;

            return Mathf.Max(1, totalSpawnCount);
        }

        private ObstacleSpawnData GetSpawnDataForIndex(int spawnIndex)
        {
            for (int i = 0; i < _validObstacleSpawnData.Count; i++)
            {
                ObstacleSpawnData spawnData = _validObstacleSpawnData[i];
                if (spawnIndex < spawnData.SpawnCount)
                    return spawnData;

                spawnIndex -= spawnData.SpawnCount;
            }

            return _validObstacleSpawnData[_validObstacleSpawnData.Count - 1];
        }

        private static bool TryGetWorldObstacle(NetworkObject spawnedObstacle, out WorldObstacle worldObstacle)
        {
            worldObstacle = null;
            return spawnedObstacle != null &&
                   (spawnedObstacle.TryGetComponent(out worldObstacle) ||
                    spawnedObstacle.GetComponentInChildren<WorldObstacle>() is WorldObstacle childWorldObstacle &&
                    (worldObstacle = childWorldObstacle) != null);
        }

        private static bool ShouldDespawnObstacle(
            WorldObstacle obstacle,
            IReadOnlyList<Vector3> trackVertices,
            float trackRadius)
        {
            return obstacle == null ||
                   DoesTrackOverlapObstacleBounds(trackVertices, trackRadius, obstacle.Bounds);
        }

        private static bool ShouldDespawnObstacle(WorldObstacle obstacle, Territory territory)
        {
            return obstacle == null || DoesTerritoryOverlapObstacleBounds(territory, obstacle.Bounds);
        }

        private static void DespawnObstacle(NetworkRunner runner, WorldObstacle obstacle)
        {
            if (obstacle == null)
                return;

            NetworkObject networkObject = obstacle.GetComponentInParent<NetworkObject>();
            if (networkObject == null || !networkObject.IsValid || !networkObject.HasStateAuthority)
                return;

            runner.Despawn(networkObject);
        }

        private bool TryFindSpawnPosition(
            System.Random random,
            Bounds groundBounds,
            float usableHalfExtentX,
            float usableHalfExtentZ,
            int row,
            int column,
            int rowCount,
            int columnCount,
            float cellWidth,
            float cellDepth,
            float heightOffset,
            out Vector3 position)
        {
            float minX = groundBounds.center.x - usableHalfExtentX;
            float minZ = groundBounds.center.z - usableHalfExtentZ;
            float centerX = minX + cellWidth * (column + 0.5f);
            float centerZ = minZ + cellDepth * (row + 0.5f);
            float jitterX = cellWidth * _cellJitterRatio;
            float jitterZ = cellDepth * _cellJitterRatio;
            float groundHeight = groundBounds.max.y + heightOffset;

            for (int attempt = 0; attempt < _placementRetryCount; attempt++)
            {
                position = new Vector3(
                    centerX + NextRange(random, -jitterX, jitterX),
                    groundHeight,
                    centerZ + NextRange(random, -jitterZ, jitterZ));

                if (IsValidPosition(position, groundBounds.center))
                    return true;
            }

            int fallbackRetryCount = _placementRetryCount * Mathf.Max(rowCount, columnCount);
            for (int attempt = 0; attempt < fallbackRetryCount; attempt++)
            {
                position = new Vector3(
                    groundBounds.center.x + NextRange(random, -usableHalfExtentX, usableHalfExtentX),
                    groundHeight,
                    groundBounds.center.z + NextRange(random, -usableHalfExtentZ, usableHalfExtentZ));

                if (IsValidPosition(position, groundBounds.center))
                    return true;
            }

            position = default;
            return false;
        }

        private bool IsValidPosition(Vector3 position, Vector3 mapCenter)
        {
            Vector3 centerOffset = position - mapCenter;
            centerOffset.y = 0f;
            float centerExclusionRadiusSqr = _centerExclusionRadius * _centerExclusionRadius;
            if (centerOffset.sqrMagnitude < centerExclusionRadiusSqr)
                return false;

            float minimumSpacingSqr = _minimumSpacing * _minimumSpacing;
            for (int i = 0; i < _spawnedPositions.Count; i++)
            {
                Vector3 offset = _spawnedPositions[i] - position;
                offset.y = 0f;
                if (offset.sqrMagnitude < minimumSpacingSqr)
                    return false;
            }

            return true;
        }

        private float GetUsableHalfExtent(float groundHalfExtent)
        {
            float paddedExtent = Mathf.Max(0f, groundHalfExtent * (1f - _edgePaddingRatio));
            return _maximumHalfExtent > 0f
                ? Mathf.Min(paddedExtent, _maximumHalfExtent)
                : paddedExtent;
        }

        private static float NextRange(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        private static bool DoesTrackOverlapObstacleBounds(IReadOnlyList<Vector3> trackVertices, float trackRadius, Bounds bounds)
        {
            Vector2 boundsMin = ToXZ(bounds.min);
            Vector2 boundsMax = ToXZ(bounds.max);
            float trackRadiusSqr = trackRadius * trackRadius;

            for (int i = 0; i < trackVertices.Count; i++)
            {
                Vector2 start = ToXZ(trackVertices[i]);
                Vector2 end = ToXZ(trackVertices[(i + 1) % trackVertices.Count]);

                if (GetSegmentBoundsDistanceSqr(start, end, boundsMin, boundsMax) <= trackRadiusSqr)
                    return true;
            }

            return false;
        }

        private static bool DoesTerritoryOverlapObstacleBounds(Territory territory, Bounds bounds)
        {
            const float geometryTolerance = 0.0001f;
            IReadOnlyList<Vector2> territoryVertices = territory.Vertices;
            Vector2 boundsMin = ToXZ(bounds.min) - Vector2.one * geometryTolerance;
            Vector2 boundsMax = ToXZ(bounds.max) + Vector2.one * geometryTolerance;

            if (territory.IsPointInPolygon(new Vector2(boundsMin.x, boundsMin.y)) ||
                territory.IsPointInPolygon(new Vector2(boundsMin.x, boundsMax.y)) ||
                territory.IsPointInPolygon(new Vector2(boundsMax.x, boundsMin.y)) ||
                territory.IsPointInPolygon(new Vector2(boundsMax.x, boundsMax.y)))
            {
                return true;
            }

            for (int i = 0; i < territoryVertices.Count; i++)
            {
                if (IsPointInsideBounds(territoryVertices[i], boundsMin, boundsMax))
                    return true;
            }

            for (int i = 0; i < territoryVertices.Count; i++)
            {
                Vector2 start = territoryVertices[i];
                Vector2 end = territoryVertices[(i + 1) % territoryVertices.Count];
                if (DoesSegmentIntersectBounds(start, end, boundsMin, boundsMax))
                    return true;
            }

            return false;
        }

        private static Vector2 ToXZ(Vector3 position)
        {
            return new Vector2(position.x, position.z);
        }

        private static float GetSegmentBoundsDistanceSqr(Vector2 start, Vector2 end, Vector2 boundsMin, Vector2 boundsMax)
        {
            if (IsPointInsideBounds(start, boundsMin, boundsMax) ||
                IsPointInsideBounds(end, boundsMin, boundsMax) ||
                DoesSegmentIntersectBounds(start, end, boundsMin, boundsMax))
            {
                return 0f;
            }

            float minDistanceSqr = Mathf.Min(
                GetPointBoundsDistanceSqr(start, boundsMin, boundsMax),
                GetPointBoundsDistanceSqr(end, boundsMin, boundsMax));

            Vector2 bottomLeft = new(boundsMin.x, boundsMin.y);
            Vector2 bottomRight = new(boundsMax.x, boundsMin.y);
            Vector2 topLeft = new(boundsMin.x, boundsMax.y);
            Vector2 topRight = new(boundsMax.x, boundsMax.y);

            minDistanceSqr = Mathf.Min(minDistanceSqr, GetDistanceToSegmentSqr(bottomLeft, start, end));
            minDistanceSqr = Mathf.Min(minDistanceSqr, GetDistanceToSegmentSqr(bottomRight, start, end));
            minDistanceSqr = Mathf.Min(minDistanceSqr, GetDistanceToSegmentSqr(topLeft, start, end));
            minDistanceSqr = Mathf.Min(minDistanceSqr, GetDistanceToSegmentSqr(topRight, start, end));
            return minDistanceSqr;
        }

        private static bool IsPointInsideBounds(Vector2 point, Vector2 boundsMin, Vector2 boundsMax)
        {
            return point.x >= boundsMin.x &&
                   point.x <= boundsMax.x &&
                   point.y >= boundsMin.y &&
                   point.y <= boundsMax.y;
        }

        private static float GetPointBoundsDistanceSqr(Vector2 point, Vector2 boundsMin, Vector2 boundsMax)
        {
            float deltaX = Mathf.Max(boundsMin.x - point.x, 0f, point.x - boundsMax.x);
            float deltaY = Mathf.Max(boundsMin.y - point.y, 0f, point.y - boundsMax.y);
            return deltaX * deltaX + deltaY * deltaY;
        }

        private static bool DoesSegmentIntersectBounds(Vector2 start, Vector2 end, Vector2 boundsMin, Vector2 boundsMax)
        {
            Vector2 direction = end - start;
            float minT = 0f;
            float maxT = 1f;

            return ClipSegmentAxis(start.x, direction.x, boundsMin.x, boundsMax.x, ref minT, ref maxT) &&
                   ClipSegmentAxis(start.y, direction.y, boundsMin.y, boundsMax.y, ref minT, ref maxT);
        }

        private static bool ClipSegmentAxis(
            float start,
            float direction,
            float min,
            float max,
            ref float minT,
            ref float maxT)
        {
            if (Mathf.Abs(direction) <= Mathf.Epsilon)
                return start >= min && start <= max;

            float inverseDirection = 1f / direction;
            float enter = (min - start) * inverseDirection;
            float exit = (max - start) * inverseDirection;
            if (enter > exit)
            {
                (enter, exit) = (exit, enter);
            }

            minT = Mathf.Max(minT, enter);
            maxT = Mathf.Min(maxT, exit);
            return minT <= maxT;
        }

        private static float GetDistanceToSegmentSqr(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float segmentLengthSqr = segment.sqrMagnitude;

            if (segmentLengthSqr <= Mathf.Epsilon)
                return (point - start).sqrMagnitude;

            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segmentLengthSqr);
            Vector2 closestPoint = start + segment * t;
            return (point - closestPoint).sqrMagnitude;
        }
    }
}
