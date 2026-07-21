using System.Collections.Generic;
using Dev.Network;
using Fusion;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class InfiniteGridRockSpawner : MonoBehaviour
    {
        [Header("Network Rock Prefabs")]
        [SerializeField] private NetworkObject[] _rockPrefabs;

        [Header("Distribution")]
        [SerializeField, Min(1)] private int _spawnCountPerPrefab = 4;
        [SerializeField, Min(0f)] private float _maximumHalfExtent = 220f;
        [SerializeField, Range(0f, 0.45f)] private float _edgePaddingRatio = 0.08f;
        [SerializeField, Range(0f, 0.45f)] private float _cellJitterRatio = 0.2f;
        [SerializeField, Min(1)] private int _placementRetryCount = 24;
        [SerializeField, Min(0f)] private float _minimumSpacing = 24f;
        [SerializeField] private float _heightOffset;
        [SerializeField] private int _randomSeed = 20260720;

        private readonly List<NetworkObject> _validRockPrefabs = new();
        private readonly List<Vector3> _spawnedPositions = new();
        private readonly List<WorldObstacle> _spawnedRocks = new();
        private InfiniteGrid _grid;
        private MeshRenderer _groundRenderer;
        private bool _spawnStarted;

        public IReadOnlyList<WorldObstacle> SpawnedRocks => _spawnedRocks;

        public void SpawnRocks()
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

            if (!IsPlacementContextReady())
            {
                Debug.LogWarning("바위 배치에 필요한 트랙 및 영역 정보가 준비되기 전이므로 바위를 스폰할 수 없습니다.");
                return;
            }

            SpawnRocksInternal();
        }

        private bool IsPlacementContextReady()
        {
            StageBootstrapper bootstrapper = StageBootstrapper.Instance;
            if (bootstrapper == null || !bootstrapper.IsInitialized)
                return false;

            TrackSystem trackSystem = bootstrapper.RoundTrackSystem;
            TerritorySystem territorySystem = bootstrapper.TerritorySystem;
            return trackSystem != null &&
                   trackSystem.Track?.Vertices != null &&
                   trackSystem.Track.Vertices.Length >= 2 &&
                   territorySystem != null &&
                   territorySystem.Territory != null;
        }

        private void SpawnRocksInternal()
        {
            if (_spawnStarted || _grid.Runner == null)
                return;

            _spawnStarted = true;
            CollectValidPrefabs();
            if (_validRockPrefabs.Count == 0)
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
            _spawnedRocks.Clear();

            int columnCount = _validRockPrefabs.Count;
            int rowCount = Mathf.Max(1, _spawnCountPerPrefab);
            float cellWidth = usableHalfExtentX * 2f / columnCount;
            float cellDepth = usableHalfExtentZ * 2f / rowCount;

            for (int row = 0; row < rowCount; row++)
            {
                for (int column = 0; column < columnCount; column++)
                {
                    int prefabIndex = (column + row) % columnCount;
                    NetworkObject prefab = _validRockPrefabs[prefabIndex];
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
                            out Vector3 spawnPosition))
                    {
                        Debug.LogWarning($"{prefab.name}의 바위 배치 위치를 찾지 못했습니다.");
                        continue;
                    }

                    float yaw = NextRange(random, 0f, 360f);
                    Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
                    int instanceNumber = row + 1;
                    NetworkObject spawnedRock = _grid.Runner.Spawn(
                        prefab,
                        spawnPosition,
                        rotation,
                        PlayerRef.None,
                        (_, spawnedObject) => spawnedObject.name = $"{prefab.name}_{instanceNumber:00}");

                    if (spawnedRock == null)
                        continue;

                    _spawnedPositions.Add(spawnPosition);
                    if (spawnedRock.TryGetComponent(out WorldObstacle worldObstacle))
                    {
                        _spawnedRocks.Add(worldObstacle);
                    }
                    else
                    {
                        Debug.LogError($"{spawnedRock.name}에 WorldObstacle 컴포넌트가 없어 바위 목록에 등록할 수 없습니다.", spawnedRock);
                    }
                }
            }
        }

        private void CollectValidPrefabs()
        {
            _validRockPrefabs.Clear();
            if (_rockPrefabs == null)
                return;

            for (int i = 0; i < _rockPrefabs.Length; i++)
            {
                NetworkObject prefab = _rockPrefabs[i];
                if (prefab != null && !_validRockPrefabs.Contains(prefab))
                    _validRockPrefabs.Add(prefab);
            }
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
            out Vector3 position)
        {
            float minX = groundBounds.center.x - usableHalfExtentX;
            float minZ = groundBounds.center.z - usableHalfExtentZ;
            float centerX = minX + cellWidth * (column + 0.5f);
            float centerZ = minZ + cellDepth * (row + 0.5f);
            float jitterX = cellWidth * _cellJitterRatio;
            float jitterZ = cellDepth * _cellJitterRatio;
            float groundHeight = groundBounds.max.y + _heightOffset;

            for (int attempt = 0; attempt < _placementRetryCount; attempt++)
            {
                position = new Vector3(
                    centerX + NextRange(random, -jitterX, jitterX),
                    groundHeight,
                    centerZ + NextRange(random, -jitterZ, jitterZ));

                if (IsValidPosition(position))
                    return true;
            }

            int fallbackRetryCount = _placementRetryCount * Mathf.Max(rowCount, columnCount);
            for (int attempt = 0; attempt < fallbackRetryCount; attempt++)
            {
                position = new Vector3(
                    groundBounds.center.x + NextRange(random, -usableHalfExtentX, usableHalfExtentX),
                    groundHeight,
                    groundBounds.center.z + NextRange(random, -usableHalfExtentZ, usableHalfExtentZ));

                if (IsValidPosition(position))
                    return true;
            }

            position = default;
            return false;
        }

        private bool IsValidPosition(Vector3 position)
        {
            Vector2Int cellIndex = _grid.GetCellIndexFromWorldPosition(position);
            if (_grid.IsCellBlockedByTrack(cellIndex) || _grid.IsCellInTerritory(cellIndex))
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
    }
}
