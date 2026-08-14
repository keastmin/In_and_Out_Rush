using System.Collections.Generic;
using Fusion;
using Unity.Profiling;
using UnityEngine;

namespace KIM.Dev
{
    public class BladeTower : AttackTower
    {
        private static readonly ProfilerMarker FixedUpdateMarker = new("BladeTower.FixedUpdateNetwork");

        protected override TowerUpgradeType UpgradeType => TowerUpgradeType.Blade;

        // 칼날 회전 이펙트 재생
        // 타겟 몬스터에서 피격 이펙트 재생
        // 타겟 몬스터에 데미지 적용

        [SerializeField] Blade blade;
        public float AttackRange = 1f;
        public bool CanHit;
        public Collider[] HitColliders;
        public HashSet<Collider> ProcessedSet = new();
        public HashSet<Collider> NewlyCollidedSet = new();

        void Start()
        {
            blade.OnSpinEvent += HandleBladeSpin;
        }

        public override void FixedUpdateNetwork()
        {
            using (FixedUpdateMarker.Auto())
            {
                if (HasStateAuthority)
                {
                    _currTarget = SetTarget();
                    LookAtTarget(_currTarget);
                    if (CheckTargetInAttackRange() && _attackTick.ExpiredOrNotRunning(Runner))
                    {
                        _attackTick = TickTimer.CreateFromSeconds(Runner, EffectiveAttackInterval);
                        AttackTarget();
                    }
                    foreach (var col in NewlyCollidedSet)
                    {
                        // PlayHitEffect(col);
                        ApplyDamageToTarget(col);
                        ProcessedSet.Add(col);
                    }
                    NewlyCollidedSet.Clear();
                }
            }
        }

        void ApplyDamageToTarget(Collider collider)
        {
            if (collider.TryGetComponent(out NetworkObject hitNo))
            {
                ApplyDamageAndPropertyEffect(collider, blade.Damage);
            }
        }

        void Update()
        {
            if (blade.CanHit)
            {
                var colliders = HitTestWithAttackRange();
                foreach (var col in colliders)
                {
                    if (ProcessedSet.Contains(col)) { continue; }
                    NewlyCollidedSet.Add(col);
                }
            }
        }

        Collider[] HitTestWithAttackRange()
        {
            var HitColliders = Physics.OverlapSphere(transform.position, AttackRange, _enemyLayer);
            return HitColliders;
        }

        bool CheckTargetInAttackRange()
        {
            if (_currTarget == null) { return false; }

            float distance = Vector3.SqrMagnitude(transform.position - _currTarget.transform.position);
            return distance <= AttackRange * AttackRange;
        }

        void AttackTarget()
        {
            PlayBladeAnimation();
        }

        void PlayBladeAnimation()
        {
            blade.StartSpinAnimation();
        }

        void HandleBladeSpin(bool canHit)
        {
            if (canHit == false)
            {
                ProcessedSet.Clear();
            }
        }
    }
}
