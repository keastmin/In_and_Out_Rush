using Dev;
using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteGrid : NetworkBehaviour
{
    private struct BuffSourceState
    {
        public Vector2Int CenterIndex;
        public int Range;
        public Color Color;
        public HashSet<Vector2Int> Cells;
    }

    public static InfiniteGrid Instance;

    [SerializeField] private TerritorySystem _territorySystem;
    [SerializeField] private TrackSystem _trackSystem;

    [SerializeField] private InfiniteGridLayoutSettings _layout = new();
    [SerializeField] private InfiniteGridGuideSettings _guide = new();
    [SerializeField] private InfiniteGridRenderingSettings _rendering = new();

    [Networked, Capacity(512), OnChangedRender(nameof(RefreshVisuals))]
    public NetworkDictionary<Vector2Int, CellData> NetworkGrid => default;

    public bool ShowCellStateOverlay => _layout.ShowCellStateOverlay;
    public Vector3 GridOrigin => _layout.ResolveOrigin(transform);
    public float GridHeight => GridOrigin.y;

    private GridCalculator _gridCalculator;
    private InfiniteGridVisualController _visualController;
    private bool _isTerritoryEventBound;
    private readonly HashSet<Vector2Int> _previewCellIndices = new();
    private readonly Dictionary<int, BuffSourceState> _buffSources = new();
    private readonly Dictionary<Vector2Int, int> _buffCellRefCount = new();
    private readonly Dictionary<Vector2Int, Color> _buffCellColorSum = new();

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
        RefreshVisuals();

        // 시작시 그리드 가이드 끄기
        SetCellStateOverlayEnabled(false);
    }

    private void OnEnable()
    {
        Initialize();
        BindTerritoryEventsIfNeeded();
        RefreshVisuals();

        // 시작시 그리드 가이드 끄기
        SetCellStateOverlayEnabled(false);
    }

    public override void Spawned()
    {
        base.Spawned();
        BindTerritoryEventsIfNeeded();
        RefreshVisuals();
    }

    private void OnDestroy()
    {
        if (_isTerritoryEventBound && _territorySystem != null)
        {
            _territorySystem.OnTerritoryExpandedEvent -= OnTerritoryExpanded;
            _isTerritoryEventBound = false;
        }
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
        if(_gridCalculator == null)
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
        if(_gridCalculator == null)
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

    public bool CanPlaceAt(Vector2Int index, int range, ISet<Vector2Int> ignoreIndices = null, bool requireTerritory = true)
    {
        if (_gridCalculator == null)
        {
            return false;
        }

        List<Vector2Int> targetIndices = _gridCalculator.GetInRangeIndices(index, range);
        for (int i = 0; i < targetIndices.Count; i++)
        {
            if (requireTerritory && !IsCellInTerritory(targetIndices[i]))
            {
                return false;
            }

            if (IsCellOccupied(targetIndices[i], ignoreIndices))
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
            if (requireTerritory && !IsCellInTerritory(targetIndex))
            {
                return false;
            }

            if (requireEmpty && IsCellOccupied(targetIndex, ignoreOccupiedIndices))
            {
                return false;
            }
        }

        return true;
    }

    public void SetBuildRangePreview(Vector2Int centerIndex, int range)
    {
        _previewCellIndices.Clear();
        List<Vector2Int> indices = GetCellIndicesInRange(centerIndex, range, includeCenter: true);
        for (int i = 0; i < indices.Count; i++)
        {
            _previewCellIndices.Add(indices[i]);
        }

        RefreshVisuals();
    }

    public void SetBuildRangePreview(IEnumerable<Vector2Int> indices)
    {
        _previewCellIndices.Clear();
        if (indices != null)
        {
            foreach (Vector2Int index in indices)
            {
                _previewCellIndices.Add(index);
            }
        }

        RefreshVisuals();
    }

    public void ClearBuildRangePreview()
    {
        if (_previewCellIndices.Count == 0)
        {
            return;
        }

        _previewCellIndices.Clear();
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
            _previewCellIndices,
            _buffCellRefCount,
            _buffCellColorSum,
            _territorySystem != null ? _territorySystem.Territory : null);
    }

    private bool CanUseNetworkGrid()
    {
        return Application.isPlaying && Object != null && Object.IsValid;
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

    private void OnTerritoryExpanded(Territory territory, TerritorySystem territorySystem)
    {
        RefreshVisuals();
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
