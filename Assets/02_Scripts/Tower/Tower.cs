using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class Tower : GridPlaceable, ICanClickObject
{
    [SerializeField] private Cost _cost;
    [SerializeField] private TowerGhost _ghost;
    [SerializeField] private TowerType _type;
    [SerializeField] private string _towerId;

    [Header("선택 시 표시")]
    [SerializeField] protected GameObject _selectedChecker; // 타워 선택 시 표시 오브젝트
    [SerializeField] protected Image _selectedImage; // 타워 선택 시 UI에 표시될 이미지

    [Header("버프")]
    [SerializeField] protected float _buffRange = 25f; // 버프 범위
    [SerializeField] protected Transform _buffRangeTransform; // 버프 범위 표시 오브젝트의 Transform

    public Cost Cost => _cost;
    public TowerGhost Ghost => _ghost;
    public bool IsCenter => (_type == TowerType.Center);
    public TowerType Type => _type;
    public bool HasBuffRange => (_buffRangeTransform != null); // 버프 범위가 있으면 true 아니면 false
    public float BuffRange => _buffRange;
    public string TowerID => _towerId;

    [Networked, OnChangedRender(nameof(OnChangeBuffScale))] protected float NetBuffRange { get; set; }

    private void Awake()
    {
        OnCancelClickThisObject();
        TowerAwake();
    }

    public override void Spawned()
    {
        base.Spawned();

        if (HasStateAuthority)
            NetBuffRange = _buffRange;

        OnChangeBuffScale();
        TowerSpawned();
    }

    public override void FixedUpdateNetwork()
    {
        TowerFixedUpdateNetwork();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        NotifyBuilderTowerDespawned();
        base.Despawned(runner, hasState);
        TowerDespawned();
    }

    /// <summary>
    /// Tower의 Awake 내부 동작 함수
    /// </summary>
    protected virtual void TowerAwake()
    {
        Debug.Log("Tower Awake");
    }

    protected virtual void TowerSpawned()
    {
    }

    protected virtual void TowerFixedUpdateNetwork()
    {
    }

    protected virtual void TowerDespawned()
    {
    }

    protected void SetId(string id)
    {
        _towerId = id;
    }

    /// <summary>
    /// 버프 범위를 설정하는 함수
    /// </summary>
    /// <param name="range">버프 범위</param>
    public void SetBuffRange(float range)
    {
        if (_buffRangeTransform == null) return;

        if (HasStateAuthority)
        {
            NetBuffRange = range;
        }
    }

    public void OnLeftMouseDownThisObject()
    {
        _selectedChecker.SetActive(true);
    }

    public void OnLeftMouseUpThisObject()
    {
        var builder = StageManager.Instance.PlayerBuilder;
        if (builder != null)
        {
            builder.TowerSelected(this);
        }
    }

    public void OnCancelClickThisObject()
    {
        _selectedChecker.SetActive(false);
    }

    private void OnChangeBuffScale()
    {
        if (_buffRangeTransform != null)
        {
            Vector3 scale = new Vector3(NetBuffRange, NetBuffRange, NetBuffRange);
            _buffRangeTransform.localScale = scale;
        }
    }

    private void NotifyBuilderTowerDespawned()
    {
        if (StageManager.Instance == null)
            return;

        var builder = StageManager.Instance.PlayerBuilder;
        if (builder == null)
            return;

        builder.OnTowerDespawned(this);
    }
}
