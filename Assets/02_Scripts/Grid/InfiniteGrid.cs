using Dev;
using Dev.Network;
using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public class InfiniteGrid : NetworkBehaviour
    {
        public static InfiniteGrid Instance;

        [SerializeField] private TerritorySystem _territorySystem;
        [SerializeField] private TrackSystem _trackSystem;

        [SerializeField] private InfiniteGridLayoutSettings _layout = new();
        [SerializeField] private InfiniteGridGuideSettings _guide = new();
        [SerializeField] private InfiniteGridRenderingSettings _rendering = new();

        [Networked, Capacity(512), OnChangedRender(nameof(RefreshVisuals))]
        public NetworkDictionary<Vector2Int, CellData> NetworkGrid => default;
        public HashSet<Tower> HostOnlyReadTowers = new();

        public bool ShowCellStateOverlay => _layout.ShowCellStateOverlay;
        public Vector3 GridOrigin => _layout.ResolveOrigin(transform);
        public float GridHeight => GridOrigin.y;
        public float CellSize => _layout.CellSize;

        private GridCalculator _gridCalculator;
        private InfiniteGridVisualController _visualController;
        private bool _isTerritoryEventBound;
        private bool _isTrackEventBound;
        private readonly HashSet<Vector2Int> _previewValidCellIndices = new();
        private readonly HashSet<Vector2Int> _previewBlockedCellIndices = new();
        private readonly HashSet<Vector2Int> _trackBlockedCellIndices = new();
        private readonly Dictionary<int, BuffSourceState> _buffSources = new();
        private readonly Dictionary<int, BuffSourceState> _buffPreviewSources = new();
        private readonly Dictionary<Vector2Int, int> _buffCellRefCount = new();
        private readonly Dictionary<Vector2Int, Color> _buffCellColorSum = new();
        private readonly TowerTrackDestructionSchedule _trackDestructionSchedule = new();
        private bool _hasSpawned;

        private void OnValidate()
        {
            Initialize();
            RefreshVisuals();
        }

        private void Awake()
        {
            Instance = this;

            Initialize();
            BindTerritoryEventsIfNeeded();
            BindTrackEventsIfNeeded();
            RefreshVisuals();

            // 시작시 그리드 가이드 끄기
            SetCellStateOverlayEnabled(false);
        }

        private void OnEnable()
        {
            Initialize();
            BindTerritoryEventsIfNeeded();
            BindTrackEventsIfNeeded();
            RefreshVisuals();

            // 시작시 그리드 가이드 끄기
            SetCellStateOverlayEnabled(false);
        }

        public override void Spawned()
        {
            base.Spawned();
            _hasSpawned = true;
            BindTerritoryEventsIfNeeded();
            BindTrackEventsIfNeeded();
            RefreshTrackBlockedCells();
            RefreshVisuals();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            _hasSpawned = false;
        }

        private void OnDestroy()
        {
            if (_isTerritoryEventBound && _territorySystem != null)
            {
                _territorySystem.OnTerritoryExpandedEvent -= OnTerritoryExpanded;
                _isTerritoryEventBound = false;
            }

            if (_isTrackEventBound && _trackSystem != null)
            {
                _trackSystem.OnTrackChanged -= OnTrackChanged;
                _isTrackEventBound = false;
            }

            _visualController?.Release();
            _visualController = null;
        }

        public void SetCellStateOverlayEnabled(bool enabled)
        {
            if (_layout.ShowCellStateOverlay == enabled)
            {
                return;
            }

            _layout.SetCellStateOverlay(enabled);
            RefreshVisuals();
        }

        /// <summary>
        /// 입력받은 위치로부터 가장 가까운 셀의 중심 위치를 계산
        /// </summary>
        /// <param name="worldPos">찾을 위치</param>
        /// <returns>찾을 위치로부터 가장 가까운 셀의 중심 위치</returns>
        public Vector3 GetCellCenterPosition(Vector3 worldPos)
        {
            Vector3 centerPos = Vector3.zero;
            if (_gridCalculator == null)
            {
                Debug.LogError("그리드 초기화 안됨");
                return centerPos;
            }
            centerPos = _gridCalculator.GetCellCenterPositionFromWorldPosition(GridOrigin, worldPos, _layout.CellSize);
            return centerPos;
        }

        public Vector2Int GetCellIndexFromWorldPosition(Vector3 worldPos)
        {
            Vector2Int index = Vector2Int.zero;
            if (_gridCalculator == null)
            {
                Debug.LogError("그리드 초기화 안됨");
                return index;
            }
            index = _gridCalculator.GetNearestCellIndexFromWorldPosition(GridOrigin, worldPos, _layout.CellSize);
            return index;
        }

        public Vector3 GetCellCenterPositionFromCellIndex(Vector2Int index)
        {
            if (_gridCalculator == null)
            {
                Debug.LogError("그리드 초기화가 되지 않았습니다.");
                return GridOrigin;
            }

            return _gridCalculator.GetCellCenterPositionFromCellIndex(GridOrigin, index.x, index.y, _layout.CellSize);
        }

        public List<Vector2Int> GetCellIndicesInRange(Vector2Int index, int range)
        {
            if (_gridCalculator == null)
            {
                Debug.LogError("그리드 초기화가 되지 않았습니다.");
                return new List<Vector2Int>();
            }

            return _gridCalculator.GetInRangeIndices(index, range);
        }

        public List<Vector2Int> GetCellIndicesInRange(Vector2Int index, int range, bool includeCenter)
        {
            List<Vector2Int> indices = GetCellIndicesInRange(index, range);
            if (includeCenter)
            {
                return indices;
            }

            indices.RemoveAll(cellIndex => cellIndex == index);
            return indices;
        }

        public bool IsCellOccupied(Vector2Int index, ISet<Vector2Int> ignoreIndices = null)
        {
            if (ignoreIndices != null && ignoreIndices.Contains(index))
            {
                return false;
            }

            if (!CanUseNetworkGrid())
            {
                return false;
            }

            foreach (var pair in NetworkGrid)
            {
                List<Vector2Int> occupiedIndices = _gridCalculator.GetInRangeIndices(pair.Key, pair.Value.ActiveRange);
                for (int i = 0; i < occupiedIndices.Count; i++)
                {
                    Vector2Int occupiedIndex = occupiedIndices[i];
                    if (ignoreIndices != null && ignoreIndices.Contains(occupiedIndex))
                    {
                        continue;
                    }

                    if (occupiedIndex == index)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsCellInTerritory(Vector2Int index)
        {
            if (_gridCalculator == null)
            {
                return false;
            }

            if (_territorySystem == null || _territorySystem.Territory == null)
            {
                return false;
            }

            Vector3 center = GetCellCenterPositionFromCellIndex(index);
            Vector2 centerXZ = new Vector2(center.x, center.z);
            return _territorySystem.Territory.IsPointInPolygon(centerXZ);
        }

        public bool IsCellBlockedByTrack(Vector2Int index)
        {
            return _trackBlockedCellIndices.Contains(index);
        }

        public bool IsCellBuildBlocked(Vector2Int index, ISet<Vector2Int> ignoreIndices = null, bool requireTerritory = true)
        {
            if (requireTerritory && !IsCellInTerritory(index))
            {
                return true;
            }

            if (IsCellBlockedByTrack(index))
            {
                return true;
            }

            return IsCellOccupied(index, ignoreIndices);
        }

        public bool CanPlaceAt(Vector2Int index, int range, ISet<Vector2Int> ignoreIndices = null, bool requireTerritory = true)
        {
            if (_gridCalculator == null)
            {
                return false;
            }

            List<Vector2Int> targetIndices = _gridCalculator.GetInRangeIndices(index, range);
            for (int i = 0; i < targetIndices.Count; i++)
            {
                if (IsCellBuildBlocked(targetIndices[i], ignoreIndices, requireTerritory))
                {
                    return false;
                }
            }

            return true;
        }

        public bool CanPlaceInRange(
            Vector2Int centerIndex,
            int range,
            bool requireEmpty = true,
            bool requireTerritory = true,
            HashSet<Vector2Int> ignoreOccupiedIndices = null)
        {
            List<Vector2Int> targetIndices = GetCellIndicesInRange(centerIndex, range, includeCenter: true);
            if (targetIndices.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < targetIndices.Count; i++)
            {
                Vector2Int targetIndex = targetIndices[i];
                if (requireEmpty && IsCellBuildBlocked(targetIndex, ignoreOccupiedIndices, requireTerritory))
                {
                    return false;
                }

                if (!requireEmpty && requireTerritory && !IsCellInTerritory(targetIndex))
                {
                    return false;
                }
            }

            return true;
        }

        public void SetBuildRangePreview(Vector2Int centerIndex, int range)
        {
            _previewValidCellIndices.Clear();
            _previewBlockedCellIndices.Clear();
            List<Vector2Int> indices = GetCellIndicesInRange(centerIndex, range, includeCenter: true);
            for (int i = 0; i < indices.Count; i++)
            {
                _previewValidCellIndices.Add(indices[i]);
            }

            RefreshVisuals();
        }

        public void SetBuildRangePreview(IEnumerable<Vector2Int> indices)
        {
            _previewValidCellIndices.Clear();
            _previewBlockedCellIndices.Clear();
            if (indices != null)
            {
                foreach (Vector2Int index in indices)
                {
                    _previewValidCellIndices.Add(index);
                }
            }

            RefreshVisuals();
        }

        public void SetBuildRangePreview(IEnumerable<Vector2Int> validIndices, IEnumerable<Vector2Int> blockedIndices)
        {
            _previewValidCellIndices.Clear();
            _previewBlockedCellIndices.Clear();

            if (validIndices != null)
            {
                foreach (Vector2Int index in validIndices)
                {
                    _previewValidCellIndices.Add(index);
                }
            }

            if (blockedIndices != null)
            {
                foreach (Vector2Int index in blockedIndices)
                {
                    _previewBlockedCellIndices.Add(index);
                    _previewValidCellIndices.Remove(index);
                }
            }

            RefreshVisuals();
        }

        public void ClearBuildRangePreview()
        {
            if (_previewValidCellIndices.Count == 0 && _previewBlockedCellIndices.Count == 0)
            {
                return;
            }

            _previewValidCellIndices.Clear();
            _previewBlockedCellIndices.Clear();
            RefreshVisuals();
        }

        public void RegisterOrUpdateBuffSource(int sourceId, Vector2Int centerIndex, int range, Color color)
        {
            if (sourceId == 0)
                return;

            int normalizedRange = Mathf.Max(0, range);
            if (_buffSources.TryGetValue(sourceId, out BuffSourceState oldState))
            {
                if (oldState.CenterIndex == centerIndex &&
                    oldState.Range == normalizedRange &&
                    oldState.Color == color)
                {
                    return;
                }

                RemoveBuffCells(oldState.Cells, oldState.Color);
            }

            var cells = new HashSet<Vector2Int>(GetCellIndicesInRange(centerIndex, normalizedRange, includeCenter: true));
            AddBuffCells(cells, color);

            _buffSources[sourceId] = new BuffSourceState
            {
                CenterIndex = centerIndex,
                Range = normalizedRange,
                Color = color,
                Cells = cells
            };

            RefreshVisuals();
        }

        public void RemoveBuffSource(int sourceId)
        {
            if (sourceId == 0 || !_buffSources.TryGetValue(sourceId, out BuffSourceState state))
                return;

            RemoveBuffCells(state.Cells, state.Color);
            _buffSources.Remove(sourceId);
            RefreshVisuals();
        }

        public void RegisterOrUpdateBuffPreviewSource(int sourceId, Vector2Int centerIndex, int range, Color color)
        {
            if (sourceId == 0)
                return;

            int normalizedRange = Mathf.Max(0, range);
            if (_buffPreviewSources.TryGetValue(sourceId, out BuffSourceState oldState))
            {
                if (oldState.CenterIndex == centerIndex &&
                    oldState.Range == normalizedRange &&
                    oldState.Color == color)
                {
                    return;
                }

                RemoveBuffCells(oldState.Cells, oldState.Color);
            }

            var cells = new HashSet<Vector2Int>(GetCellIndicesInRange(centerIndex, normalizedRange, includeCenter: true));
            AddBuffCells(cells, color);

            _buffPreviewSources[sourceId] = new BuffSourceState
            {
                CenterIndex = centerIndex,
                Range = normalizedRange,
                Color = color,
                Cells = cells
            };

            RefreshVisuals();
        }

        public void ClearBuffPreviewSources()
        {
            if (_buffPreviewSources.Count == 0)
                return;

            foreach (BuffSourceState state in _buffPreviewSources.Values)
            {
                RemoveBuffCells(state.Cells, state.Color);
            }

            _buffPreviewSources.Clear();
            RefreshVisuals();
        }

        public void RemoveBuffPreviewSource(int sourceId)
        {
            if (sourceId == 0 || !_buffPreviewSources.TryGetValue(sourceId, out BuffSourceState state))
                return;

            RemoveBuffCells(state.Cells, state.Color);
            _buffPreviewSources.Remove(sourceId);
            RefreshVisuals();
        }

        public bool IsCellInBuffSource(int sourceId, Vector2Int index)
        {
            return sourceId != 0 &&
                   _buffSources.TryGetValue(sourceId, out BuffSourceState sourceState) &&
                   sourceState.Cells != null &&
                   sourceState.Cells.Contains(index);
        }

        public bool IsWorldPositionInBuffSource(int sourceId, Vector3 worldPosition)
        {
            Vector2Int index = GetCellIndexFromWorldPosition(worldPosition);
            return IsCellInBuffSource(sourceId, index);
        }

        /// <summary>
        /// 사용중인 셀 등록
        /// </summary>
        /// <param name="index">등록할 셀의 인덱스</param>
        /// <param name="range">사용 등록 범위</param>
        /// <returns>등록 성공 여부</returns>
        public bool AddActiveCell(Vector2Int index, int range, bool requireTerritory = true)
        {
            if (!HasStateAuthority || NetworkGrid.ContainsKey(index))
                return false;

            if (!CanPlaceAt(index, range, null, requireTerritory))
                return false;

            NetworkGrid.Add(index, new CellData(range, BuffData.Empty));
            RefreshVisuals();
            return true;
        }

        /// <summary>
        /// 사용중인 셀 삭제
        /// </summary>
        /// <param name="index">삭제할 인덱스</param>
        /// <returns>삭제 성공 여부</returns>
        public bool RemoveActiveCell(Vector2Int index)
        {
            if (!HasStateAuthority || !NetworkGrid.ContainsKey(index))
                return false;

            NetworkGrid.Remove(index);
            RefreshVisuals();
            return true;
        }

        private void Initialize()
        {
            _gridCalculator ??= new GridCalculator(32, 32);
            _visualController ??= new InfiniteGridVisualController();
        }

        private void RefreshVisuals()
        {
            IEnumerable<KeyValuePair<Vector2Int, CellData>> networkGrid = null;

            if (CanUseNetworkGrid())
            {
                networkGrid = NetworkGrid;
            }

            _visualController.Apply(
                gameObject,
                transform,
                _layout,
                _guide,
                _rendering,
                _gridCalculator,
                networkGrid,
                _trackBlockedCellIndices,
                _previewValidCellIndices,
                _previewBlockedCellIndices,
                _buffCellRefCount,
                _buffCellColorSum,
                _territorySystem != null ? _territorySystem.Territory : null);
        }

        private bool CanUseNetworkGrid()
        {
            return Application.isPlaying && _hasSpawned && Object != null && Object.IsValid && Object.IsInSimulation;
        }

        private void BindTerritoryEventsIfNeeded()
        {
            if (_isTerritoryEventBound || _territorySystem == null)
            {
                return;
            }

            _territorySystem.OnTerritoryExpandedEvent += OnTerritoryExpanded;
            _isTerritoryEventBound = true;
        }

        private void BindTrackEventsIfNeeded()
        {
            if (_isTrackEventBound || _trackSystem == null)
            {
                return;
            }

            _trackSystem.OnTrackChanged += OnTrackChanged;
            _isTrackEventBound = true;
            RefreshTrackBlockedCells();
        }

        private void OnTerritoryExpanded(Territory territory, TerritorySystem territorySystem)
        {
            RefreshVisuals();
        }

        private void OnTrackChanged(Vector3[] vertices, TrackSystem trackSystem, object sender)
        {
            RefreshTrackBlockedCells();
            RefreshTrackDestructionSchedule();
        }

        private void RefreshTrackBlockedCells()
        {
            _trackBlockedCellIndices.Clear();

            if (_gridCalculator == null || _trackSystem == null || _trackSystem.Track?.Vertices == null || _trackSystem.Track.Vertices.Length < 2)
            {
                RefreshVisuals();
                return;
            }

            Vector3[] vertices = _trackSystem.Track.Vertices;
            float overlapRadius = _layout.CellSize + (_trackSystem.TrackLineWidth * 0.5f);
            Bounds bounds = new Bounds(vertices[0], Vector3.zero);

            for (int i = 1; i < vertices.Length; i++)
            {
                bounds.Encapsulate(vertices[i]);
            }

            bounds.Expand(new Vector3(overlapRadius * 2f, 0f, overlapRadius * 2f));

            Vector2Int[] corners =
            {
            GetCellIndexFromWorldPosition(new Vector3(bounds.min.x, GridHeight, bounds.min.z)),
            GetCellIndexFromWorldPosition(new Vector3(bounds.min.x, GridHeight, bounds.max.z)),
            GetCellIndexFromWorldPosition(new Vector3(bounds.max.x, GridHeight, bounds.min.z)),
            GetCellIndexFromWorldPosition(new Vector3(bounds.max.x, GridHeight, bounds.max.z))
        };

            int padding = Mathf.CeilToInt(overlapRadius / Mathf.Max(0.001f, _layout.CellSize)) + 2;
            int minCol = corners[0].x;
            int maxCol = corners[0].x;
            int minRow = corners[0].y;
            int maxRow = corners[0].y;

            for (int i = 1; i < corners.Length; i++)
            {
                minCol = Mathf.Min(minCol, corners[i].x);
                maxCol = Mathf.Max(maxCol, corners[i].x);
                minRow = Mathf.Min(minRow, corners[i].y);
                maxRow = Mathf.Max(maxRow, corners[i].y);
            }

            minCol -= padding;
            maxCol += padding;
            minRow -= padding;
            maxRow += padding;

            for (int col = minCol; col <= maxCol; col++)
            {
                for (int row = minRow; row <= maxRow; row++)
                {
                    Vector2Int cellIndex = new Vector2Int(col, row);
                    Vector3 cellCenter = GetCellCenterPositionFromCellIndex(cellIndex);
                    if (IsTrackOverlappingCell(cellCenter, vertices, overlapRadius))
                    {
                        _trackBlockedCellIndices.Add(cellIndex);
                    }
                }
            }

            RefreshVisuals();
        }

        private bool IsTrackOverlappingCell(Vector3 cellCenter, Vector3[] trackVertices, float overlapRadius)
        {
            Vector2 center = new Vector2(cellCenter.x, cellCenter.z);
            float overlapRadiusSqr = overlapRadius * overlapRadius;

            for (int i = 0; i < trackVertices.Length; i++)
            {
                Vector3 startVertex = trackVertices[i];
                Vector3 endVertex = trackVertices[(i + 1) % trackVertices.Length];
                Vector2 start = new Vector2(startVertex.x, startVertex.z);
                Vector2 end = new Vector2(endVertex.x, endVertex.z);

                if (GetDistanceToSegmentSqr(center, start, end) <= overlapRadiusSqr)
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetDistanceToSegmentSqr(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float segmentLengthSqr = segment.sqrMagnitude;

            if (segmentLengthSqr <= Mathf.Epsilon)
            {
                return (point - start).sqrMagnitude;
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segmentLengthSqr);
            Vector2 closestPoint = start + segment * t;
            return (point - closestPoint).sqrMagnitude;
        }

        public void DestroyTowersBlockedByTrack()
        {
            RefreshTrackBlockedCells();
            DestroyBlockedTowers();
            RefreshVisuals();
        }

        public bool IsTowerPendingTrackDestruction(Tower tower)
        {
            return _trackDestructionSchedule.IsPendingDestruction(tower);
        }

        public bool HasFreeTrackRelocation(Tower tower)
        {
            return _trackDestructionSchedule.HasFreeRelocation(tower);
        }

        public bool TryConsumeFreeTrackRelocation(Tower tower)
        {
            if (!HasStateAuthority || IsTowerBlockedByTrack(tower))
                return false;

            return _trackDestructionSchedule.TryConsumeFreeRelocation(tower);
        }

        public void RemoveTowerFromTrackDestructionSchedule(Tower tower)
        {
            _trackDestructionSchedule.Remove(tower);
        }

        private void DestroyBlockedTowers()
        {
            if (!HasStateAuthority || Runner == null)
            {
                return;
            }

            if (HostOnlyReadTowers.Count == 0)
            {
                _trackDestructionSchedule.Clear();
                return;
            }

            List<Tower> towersToDestroy = new();

            foreach (Tower tower in HostOnlyReadTowers)
            {
                if (IsTowerBlockedByTrack(tower))
                    towersToDestroy.Add(tower);
            }

            for (int i = 0; i < towersToDestroy.Count; i++)
            {
                Tower tower = towersToDestroy[i];
                if (tower == null || tower.Object == null)
                {
                    continue;
                }

                if (tower.IsCenter && StageBootstrapper.Instance != null && StageBootstrapper.Instance.PlayerBuilder != null)
                {
                    int nextCenterCount = Mathf.Max(0, StageBootstrapper.Instance.PlayerBuilder.CenterTowerCount - 1);
                    StageBootstrapper.Instance.PlayerBuilder.SetCenterTowerCount(nextCenterCount);
                }

                tower.ReleaseGridOccupation();
                _trackDestructionSchedule.Remove(tower);
                Runner.Despawn(tower.Object);
            }

            _trackDestructionSchedule.Clear();
        }

        private void RefreshTrackDestructionSchedule()
        {
            if (!CanUseNetworkGrid() || !HasStateAuthority)
                return;

            _trackDestructionSchedule.Refresh(HostOnlyReadTowers, IsTowerBlockedByTrack);
        }

        private bool IsTowerBlockedByTrack(Tower tower)
        {
            if (tower == null || !tower.HasGridOccupation)
                return false;

            IReadOnlyList<Vector2Int> occupiedIndices = tower.OccupiedIndices;
            for (int i = 0; i < occupiedIndices.Count; i++)
            {
                if (IsCellBlockedByTrack(occupiedIndices[i]))
                    return true;
            }

            return false;
        }

        private void AddBuffCells(IEnumerable<Vector2Int> indices, Color color)
        {
            if (indices == null)
                return;

            foreach (Vector2Int index in indices)
            {
                if (_buffCellRefCount.TryGetValue(index, out int count))
                {
                    _buffCellRefCount[index] = count + 1;
                }
                else
                {
                    _buffCellRefCount[index] = 1;
                }

                if (_buffCellColorSum.TryGetValue(index, out Color sumColor))
                {
                    _buffCellColorSum[index] = sumColor + color;
                }
                else
                {
                    _buffCellColorSum[index] = color;
                }
            }
        }

        private void RemoveBuffCells(IEnumerable<Vector2Int> indices, Color color)
        {
            if (indices == null)
                return;

            foreach (Vector2Int index in indices)
            {
                if (_buffCellRefCount.TryGetValue(index, out int count))
                {
                    if (count <= 1)
                    {
                        _buffCellRefCount.Remove(index);
                    }
                    else
                    {
                        _buffCellRefCount[index] = count - 1;
                    }
                }

                if (_buffCellColorSum.TryGetValue(index, out Color sumColor))
                {
                    Color nextColor = sumColor - color;
                    if (_buffCellRefCount.ContainsKey(index))
                    {
                        _buffCellColorSum[index] = nextColor;
                    }
                    else
                    {
                        _buffCellColorSum.Remove(index);
                    }
                }
            }
        }
    }
}
