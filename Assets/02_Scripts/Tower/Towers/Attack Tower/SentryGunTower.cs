using Fusion;
using UnityEngine;

public sealed class SentryGunTower : AttackTower
{
    // 총구 위치에서 발포 이펙트 재생
    // 타겟 몬스터에서 피격 이펙트 재생
    // 타겟 몬스터에 데미지 적용

    [Header("발사체")]
    [SerializeField] private Bullet _bullet;
    [SerializeField] private float _bulletSpeed;
    [SerializeField] private float _bulletDamage;

    [Networked, OnChangedRender(nameof(OnShotChanged))]
    private int _shotSeq { get; set; }

    protected override void TowerSpawned()
    {
        Debug.Log("Tower 스폰됨");
        if (HasStateAuthority)
        {
            InitSentryTower();
        }
    }

    protected override void TowerDespawned()
    {
        Debug.Log("Tower 디스폰됨");
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            _currTarget = SetTarget(); // 타겟 설정
            LookAtTarget(_currTarget); // 타겟 바라보기
            Fire();
        }
    }

    protected override void Fire()
    {
        if (_attackTick.ExpiredOrNotRunning(Runner) && _currTarget != null)
        {
            _attackTick = TickTimer.CreateFromSeconds(Runner, _attackSpeed);
            _shotSeq++;

            // 데미지 넣기
            if (_currTarget.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(_bulletDamage);
            }
        }
    }

    private void OnShotChanged()
    {
        if(_targetObject != null && _targetObject.TryGetComponent(out Collider target))
        {
            var bullet = Instantiate(_bullet, _attackPosition.position, Quaternion.identity);
            bullet.InitBullet(target, _bulletSpeed);
        }
    }

    private void InitSentryTower()
    {
        // 공격 타이머 초기화
        _attackTick = TickTimer.CreateFromSeconds(Runner, _attackSpeed);
    }
}