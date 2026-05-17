using Fusion;
using UnityEngine;

public class RegenerationTower : SupportTower
{
    [SerializeField] private float _armor = 100f;
    [SerializeField] private float _maxHeal = 100f;
    [SerializeField] private float _healPerSec = 5f;

    private bool _receiveArmor = false;
    private IHeal _detectedRunner;
    private IHeal _enteringRunner;

    private float _healTimer = 1f;
    private float _currentTimer = 0f;

    protected override void TowerFixedUpdateNetwork()
    {
        if (!HasStateAuthority)
        {
            return;
        }

        if (!TryGetGrid(out InfiniteGrid gm))
        {
            _detectedRunner = null;
            _enteringRunner = null;
            _receiveArmor = false;
            _currentTimer = 0f;
            return;
        }

    }

    protected override void TowerDespawned()
    {
        base.TowerDespawned();
    }
}

