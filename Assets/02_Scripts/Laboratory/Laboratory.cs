using Fusion;
using UnityEngine;
using System;

public class Laboratory : GridPlaceable, ICanClickObject
{
    [Header("Laboratory Grid")]
    [SerializeField][Min(0)] private int _defaultRange = 1;
    [SerializeField] private bool _ignoreTerritoryOnSpawn = true;

    private PlayerBuilder _pb;
    private PlayerRunner _pr;

    public event Action<bool> OnClickLaboratoryObjectAction;

    private void Awake()
    {
        if (_buildRange == 0)
        {
            _buildRange = _defaultRange;
        }
    }

    public override void Spawned()
    {
        base.Spawned();

        if (_ignoreTerritoryOnSpawn && !IsGridOccupied)
        {
            TryOccupyAtWorldPosition(transform.position, requireTerritory: false, requireEmpty: true);
        }
    }

    public void OnLeftMouseDownThisObject()
    {
    }

    // 빌더가 연구소를 통해 강화 UI를 띄우기
    public void OnLeftMouseUpThisObject()
    {
        OnClickLaboratoryObjectAction?.Invoke(true);
    }

    public void OnCancelClickThisObject()
    {
    }

    /// <summary>
    /// OnEnable에서 구독, OnDisable, Despawnd에서 구독 취소
    /// </summary>

    // 플레이어 러너의 연구소 바라보는 액션 구독
    private void CinemachinePriorityUp()
    {
        // 연구소를 바라보는 시네머신의 Priority를 올림
    }

    // 플레이어 러너의 연구소 바라보는 것을 해제하는 액션 구독
    private void CinemachinePriorityDown()
    {
        // 연구소를 바라보는 시네머신의 Priority를 내림
    }
}
