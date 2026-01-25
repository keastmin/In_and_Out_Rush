using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class RegenerationTower : SupportTower
{
    [SerializeField] private float _armor = 100f;
    [SerializeField] private float _maxHeal = 100f;
    [SerializeField] private float _healPerSec = 5f;
    [SerializeField] private LayerMask _detectLayer;

    private bool _receiveArmor = false;

    private IHeal _detectedRunner;
    private IHeal _enteringRunner;
    private Collider[] _detectedColliders;
    private const int _arraySize = 100;

    private float _healTimer = 1f;
    private float _currentTimer = 0f;

    protected override void TowerAwake()
    {
        // 초기화
        _detectedColliders = new Collider[_arraySize];
    }

    protected override void TowerFixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            // 감지
            int detectCount = Physics.OverlapSphereNonAlloc(transform.position, _buffRange, _detectedColliders, _detectLayer);

            _detectedRunner = null;
            if (detectCount > 0)
            {
                for (int i = 0; i < detectCount; i++)
                {
                    if (_detectedColliders[i].TryGetComponent(out IHeal runner))
                    {
                        _detectedRunner = runner;
                    }
                }
            }
            else
            {
                if (_enteringRunner != null)
                    _enteringRunner = null;
            }

            // 진입 확인 -> 보호막 부여 후 부여했다는 플래그 true
            if(_detectedRunner != null && _enteringRunner == null)
            {
                _enteringRunner = _detectedRunner;

                if (!_receiveArmor)
                {
                    _enteringRunner.ReceiveArmor(_armor);
                    _receiveArmor = true;
                }
            }

            // 러너가 들어와 있으며 타이머가 될 때마다 힐
            if(_enteringRunner != null)
            {
                // 잔여 힐량에서 줄 수 있는 최대 초당 힐량을 떼어내어 힐을 주고 힐이 들어간만큼 잔여 힐량에서 떼어냄. 그리고 잔여 힐량이 0이면 이 객체는 사라져야됨.
                if(_currentTimer >= _healTimer)
                {
                    _currentTimer = 0f;

                    float request = Mathf.Min(_healPerSec, _maxHeal);
                    float healed = _enteringRunner.Heal(request);
                    _maxHeal -= healed;
                    if (_maxHeal < 0f) _maxHeal = 0f;
                }

                _currentTimer += Runner.DeltaTime;
            }
        }
    }
}
