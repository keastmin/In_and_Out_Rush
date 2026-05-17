using Fusion;
using Dev.Network;
using System.Collections.Generic;
using UnityEngine;

public sealed class AmplificationTower : SupportTower, ICanDragObject
{
    [Header("버프")]
    [SerializeField] private AmplificationTowerBuffParam _buffParam = new();
    [SerializeField] private Transform _buffObject;
    [SerializeField] private LayerMask _detectLayer;
    [SerializeField] private float _buffRange = 25f;

    private Collider[] _detectedBuffReceivers;
    private HashSet<IBuffReceiver> _receivers;
    private HashSet<IBuffReceiver> _detectedReceivers;
    private List<IBuffReceiver> _exitCandidates;

    protected override void TowerAwake()
    {
        _receivers = new HashSet<IBuffReceiver>();
        _detectedReceivers = new HashSet<IBuffReceiver>();
        _exitCandidates = new List<IBuffReceiver>();
        _detectedBuffReceivers = new Collider[100];
    }

    public override void Spawned()
    {
        base.Spawned();

        if(_buffObject != null)
        {
            SetBuffObjectSize();
        }
    }

    protected override void TowerFixedUpdateNetwork()
    {
        // 호스트라면 버프 받는 사람들 감지 후 버프 주기
        if(HasStateAuthority && Runner.IsServer)
        {
            _detectedReceivers.Clear();

            int detectedCount = Physics.OverlapSphereNonAlloc(transform.position, _buffRange / 2f, _detectedBuffReceivers, _detectLayer);
            for(int i = 0; i < detectedCount; i++)
            {
                if (_detectedBuffReceivers[i].TryGetComponent(out IBuffReceiver receiver))
                {
                    if (!_detectedReceivers.Add(receiver))
                    {
                        continue;
                    }

                    if (_receivers.Contains(receiver))
                    {
                        receiver.BuffStay(_buffParam);
                    }
                    else
                    {
                        receiver.BuffEnter(_buffParam);
                        _receivers.Add(receiver);
                    }
                }
            }

            _exitCandidates.Clear();
            foreach (var receiver in _receivers)
            {
                if (!_detectedReceivers.Contains(receiver))
                {
                    _exitCandidates.Add(receiver);
                }
            }

            for (int i = 0; i < _exitCandidates.Count; i++)
            {
                IBuffReceiver receiver = _exitCandidates[i];
                receiver.BuffExit(_buffParam);
                _receivers.Remove(receiver);
            }
        }
    }

    protected override void TowerDespawned()
    {
        base.TowerDespawned();
    }

    private void SetBuffObjectSize()
    {
        _buffObject.transform.localScale = new Vector3(_buffRange, _buffRange, _buffRange);
    }

    #region ICanDragObject

    public void OnDragSelectedThisObject()
    {
        OnLeftMouseDownThisObject();
    }

    public void OnDragOverThisObject()
    {
        OnCancelClickThisObject();
    }

    public void OnDragCompleteThisObject()
    {
        var manager = StageBootstrapper.Instance;
        if (manager != null)
        {
            manager.PlayerBuilder.TowerSelected(this);
        }
    }

    #endregion
}

