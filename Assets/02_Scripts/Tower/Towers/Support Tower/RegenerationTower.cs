using Fusion;
using Grid;
using UnityEngine;

public class RegenerationTower : SupportTower
{
    [SerializeField] private float _armor = 100f;
    [SerializeField] private float _maxHeal = 100f;
    [SerializeField] private float _healPerSec = 5f;

    protected override bool EmitBuffCells => true;

    private bool _receiveArmor = false;
    private IHeal _detectedRunner;
    private IHeal _enteringRunner;

    private float _healTimer = 1f;
    private float _currentTimer = 0f;

    protected override void TowerFixedUpdateNetwork()
    {
        SyncBuffCellSource();

        if (!HasStateAuthority)
        {
            return;
        }

        if (!TryGetGridManager(out GridManager gm))
        {
            _detectedRunner = null;
            _enteringRunner = null;
            _receiveArmor = false;
            _currentTimer = 0f;
            return;
        }

        _detectedRunner = null;
        var entries = BuffReceiverRegistry.Snapshot;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if ((entry.TargetType & BuffTargetType.Runner) == 0) continue;
            if (entry.Transform == null) continue;
            if (!gm.IsWorldPositionInBuffSource(BuffSourceId, entry.Transform.position)) continue;

            if (entry.Receiver is IHeal healReceiver)
            {
                _detectedRunner = healReceiver;
                break;
            }
        }

        if (_detectedRunner == null)
        {
            _enteringRunner = null;
            _receiveArmor = false;
            _currentTimer = 0f;
            return;
        }

        if (_detectedRunner != _enteringRunner)
        {
            _enteringRunner = _detectedRunner;
            _currentTimer = 0f;

            if (!_receiveArmor)
            {
                _enteringRunner.ReceiveArmor(_armor);
                _receiveArmor = true;
            }
        }

        if (_enteringRunner == null || _maxHeal <= 0f)
        {
            return;
        }

        _currentTimer += Runner.DeltaTime;
        if (_currentTimer < _healTimer)
        {
            return;
        }

        _currentTimer = 0f;

        float request = Mathf.Min(_healPerSec, _maxHeal);
        float healed = _enteringRunner.Heal(request);
        _maxHeal -= healed;
        if (_maxHeal < 0f)
        {
            _maxHeal = 0f;
        }
    }

    protected override void TowerDespawned()
    {
        base.TowerDespawned();
        ReleaseBuffCellSource();
    }
}

