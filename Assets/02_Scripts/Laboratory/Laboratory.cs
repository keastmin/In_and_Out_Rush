using Fusion;
using UnityEngine;
using System;

public class Laboratory : GridPlaceable, ICanClickObject
{
    [Header("Laboratory Grid")]
    [SerializeField][Min(0)] private int _defaultRange = 1;
    [SerializeField] private bool _ignoreTerritoryOnSpawn = true;

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
}
