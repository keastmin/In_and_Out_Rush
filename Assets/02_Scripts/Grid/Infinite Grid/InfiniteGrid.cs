using System.Collections.Generic;
using UnityEngine;

public class InfiniteGrid : MonoBehaviour
{
    public static InfiniteGrid Instance;

    [SerializeField] private InfiniteGridLayoutSettings _layout = new();
    [SerializeField] private InfiniteGridGuideSettings _guide = new();
    [SerializeField] private InfiniteGridRenderingSettings _rendering = new();

    public Dictionary<Vector2Int, CellData> Grid { get; private set; }
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
        Debug.Log("무한 그리드 Awake 실행");
        Instance = this;
        Debug.Log("인스턴스 이름: "+ Instance.name);

        Initialize();
        RefreshVisuals();
    }

    private void OnEnable()
    {
        Initialize();
        RefreshVisuals();
    }

    private void LateUpdate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

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
    /// <param name="worldPos">등록할 셀을 찾을 위치</param>
    /// <param name="range">사용 등록 범위</param>
    /// <returns>등록 성공 여부</returns>
    public bool AddActiveCell(Vector3 worldPos, int range)
    {
        return AddActiveCell(worldPos, range, BuffData.Empty);
    }

    /// <summary>
    /// 사용중인 셀 등록
    /// </summary>
    /// <param name="worldPos">등록할 셀을 찾을 위치</param>
    /// <param name="range">사용 등록 범위</param>
    /// <param name="buffData">버프 데이터</param>
    /// <returns>등록 성공 여부</returns>
    public bool AddActiveCell(Vector3 worldPos, int range, BuffData buffData)
    {
        Vector2Int index = GetCellIndexFromWorldPosition(worldPos);
        if (Grid.ContainsKey(index))
            return false;
        Grid.Add(index, new CellData(range, buffData));
        return true;
    }

    /// <summary>
    /// 사용중인 셀 삭제
    /// </summary>
    /// <param name="worldPos">삭제할 위치</param>
    /// <returns>삭제 성공 여부</returns>
    public bool RemoveActiveCell(Vector3 worldPos)
    {
        return true;
    }

    private void Initialize()
    {
        _gridCalculator ??= new GridCalculator(32, 32);
        _visualController ??= new InfiniteGridVisualController();
        Grid ??= new Dictionary<Vector2Int, CellData>();
    }

    private void RefreshVisuals()
    {
        _visualController.Apply(gameObject, transform, _layout, _guide, _rendering);
    }
}
