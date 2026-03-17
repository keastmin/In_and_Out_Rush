using Fusion;
using Grid;
using System.Collections.Generic;
using UnityEngine;

public sealed class AmplificationTower : SupportTower, ICanDragObject
{
    private static readonly Color AmplificationBuffCellColor = new Color(1f, 0.85f, 0.15f, 0.35f);

    [SerializeField] private AmplificationTowerBuffParam _buffParam = new();
    [SerializeField] private BuffTargetType _targetTypes = BuffTargetType.Runner;
    [SerializeField] private bool _affectSelf = false;

    protected override bool EmitBuffCells => true;
    protected override Color BuffCellColor => AmplificationBuffCellColor;

    private HashSet<IBuffReceiver> _receivers;
    private readonly List<IBuffReceiver> _detectedReceivers = new();
    private List<IBuffReceiver> _removeCandidates;

    protected override void TowerAwake()
    {
        _receivers = new HashSet<IBuffReceiver>();
        _removeCandidates = new List<IBuffReceiver>();
    }

    protected override void TowerFixedUpdateNetwork()
    {
        SyncBuffCellSource();

        if (!HasStateAuthority)
        {
            return;
        }

        if (!TryGetGridManager(out GridManager gm))
        {
            ClearAllReceivers();
            return;
        }

        _detectedReceivers.Clear();
        var entries = BuffReceiverRegistry.Snapshot;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry.Receiver == null || entry.Transform == null) continue;
            if ((_targetTypes & entry.TargetType) == 0) continue;
            if (!_affectSelf && entry.Transform == transform) continue;

            if (gm.IsWorldPositionInBuffSource(BuffSourceId, entry.Transform.position))
            {
                _detectedReceivers.Add(entry.Receiver);
            }
        }

        for (int i = 0; i < _detectedReceivers.Count; i++)
        {
            IBuffReceiver detected = _detectedReceivers[i];
            if (_receivers.Contains(detected)) continue;

            detected.BuffEnter(_buffParam);
            _receivers.Add(detected);
        }

        _removeCandidates.Clear();
        foreach (var receiver in _receivers)
        {
            bool stillInside = false;
            for (int i = 0; i < _detectedReceivers.Count; i++)
            {
                if (receiver == _detectedReceivers[i])
                {
                    stillInside = true;
                    break;
                }
            }

            if (!stillInside)
            {
                _removeCandidates.Add(receiver);
            }
        }

        for (int i = 0; i < _removeCandidates.Count; i++)
        {
            IBuffReceiver receiver = _removeCandidates[i];
            receiver.BuffExit(_buffParam);
            _receivers.Remove(receiver);
        }
    }

    protected override void TowerDespawned()
    {
        base.TowerDespawned();
        ReleaseBuffCellSource();
        ClearAllReceivers();
    }

    private void ClearAllReceivers()
    {
        foreach (var receiver in _receivers)
        {
            receiver.BuffExit(_buffParam);
        }

        _receivers.Clear();
        _detectedReceivers.Clear();
        _removeCandidates.Clear();
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
        var manager = StageManager.Instance;
        if (manager != null)
        {
            manager.PlayerBuilder.TowerSelected(this);
        }
    }

    #endregion
}

