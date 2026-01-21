using Fusion;
using UnityEngine;

public sealed class MissileTower : AttackTower
{
    // 총구 위치에서 발포 이펙트 재생
    // 타겟 몬스터에서 피격 이펙트 재생
    // 타겟 몬스터에 데미지 적용
    [SerializeField] private Missile _missliePrefab;
    [SerializeField] private float _missileDamage = 1f;
    [SerializeField] private float _explosionRange = 3f;
    [SerializeField] private float _missileSpeed = 10f;

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            _currTarget = SetTarget();
            LookAtTarget(_currTarget);
            Fire();
        }
    }

    protected override void Fire()
    {
        if (_attackTick.ExpiredOrNotRunning(Runner) && _currTarget != null)
        {
            _attackTick = TickTimer.CreateFromSeconds(Runner, _attackSpeed);
            // 데미지는 미사일 탄 자체가 넣음
            var missile = Runner.Spawn(_missliePrefab, _attackPosition.position, _attackPosition.rotation);
            missile.InitMissile(_missileDamage, _explosionRange, _missileSpeed, _currTarget);
        }
    }
}