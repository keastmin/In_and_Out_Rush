using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteGrid : NetworkBehaviour
{
    public static InfiniteGrid Instance;

    [SerializeField] private InfiniteGridLayoutSettings _layout = new();
    [SerializeField] private InfiniteGridGuideSettings _guide = new();
    [SerializeField] private InfiniteGridRenderingSettings _rendering = new();

    [Networked, Capacity(512), OnChangedRender(nameof(RefreshVisuals))]
    public NetworkDictionary<Vector2Int, CellData> NetworkGrid => default;

    public bool ShowCellStateOverlay => _layout.ShowCellStateOverlay;
    public Vector3 GridOrigin => _layout.ResolveOrigin(transform);

    private GridCalculator _gridCalculator;
    private InfiniteGridVisualController _visualController;

    private void OnValidate()
    {
        Initialize();
        RefreshVisuals();
    }

    private void Awake()
    {
        Instance = this;

        Initialize();
        RefreshVisuals();

        // 시작시 그리드 가이드 끄기
        SetCellStateOverlayEnabled(false);
    }

    private void OnEnable()
    {
        Initialize();
        RefreshVisuals();

        // 시작시 그리드 가이드 끄기
        SetCellStateOverlayEnabled(false);
    }

    public override void Spawned()
    {
        base.Spawned();
        RefreshVisuals();
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

    /// <summary>
    /// 사용중인 셀 등록
    /// </summary>
    /// <param name="index">등록할 셀의 인덱스</param>
    /// <param name="range">사용 등록 범위</param>
    /// <returns>등록 성공 여부</returns>
    public bool AddActiveCell(Vector2Int index, int range)
    {
        if (!HasStateAuthority || NetworkGrid.ContainsKey(index))
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

        _visualController.Apply(gameObject, transform, _layout, _guide, _rendering, _gridCalculator, networkGrid);
    }

    private bool CanUseNetworkGrid()
    {
        return Application.isPlaying && Object != null && Object.IsValid;
    }
}
