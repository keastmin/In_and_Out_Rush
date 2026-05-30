using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public class Missile : NetworkBehaviour
    {
        [Header("도착 판정(미세 오차)")]
        [SerializeField] private float _arriveDistance = 0.25f;

        [Header("폭발 판정 레이어(데미지 대상)")]
        [SerializeField] private LayerMask _damageableMask = ~0;

        private float _damage;
        private float _explosionRange;
        private float _speed;

        private Collider _targetCollider;
        private Vector3 _lastTargetPos;

        private Rigidbody _rb;

        private Vector3 _prevPos;
        private bool _inited;

        // "초당 1.2배 가속" = 1초에 speed * 1.2가 되도록 (지수 증가)
        private const float ACCEL_MULT_PER_SEC = 1.4f;

        public override void Spawned()
        {
            _rb = GetComponent<Rigidbody>();
            _prevPos = _rb.position;

            if (!HasStateAuthority)
                Runner.SetIsSimulated(Object, true);
        }

        /// <summary>
        /// 스폰 직후(호스트)에서만 초기화해도 충분함.
        /// Spawn 콜백이 모든 피어에서 호출되더라도, 아래 가드로 클라는 무시한다.
        /// </summary>
        public void InitMissile(float damage, float explosionRange, float speed, Collider targetCollider)
        {
            if (!HasStateAuthority)
                return;

            _damage = damage;
            _explosionRange = explosionRange;
            _speed = speed;

            _targetCollider = targetCollider;
            _lastTargetPos = targetCollider != null ? targetCollider.transform.position : _rb.position;

            _prevPos = _rb.position;
            _inited = true;

            // 첫 틱부터 움직이게
            UpdateVelocityAndFacing();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || !_inited)
                return;

            // 타겟이 살아있으면 마지막 위치 갱신
            if (_targetCollider != null)
                _lastTargetPos = _targetCollider.transform.position;

            Vector3 currPos = _rb.position;
            Vector3 toTarget = _lastTargetPos - currPos;

            float arriveSqr = _arriveDistance * _arriveDistance;

            // (1) 거의 도착
            if (toTarget.sqrMagnitude <= arriveSqr)
            {
                ExplodeAt(_lastTargetPos);
                return;
            }

            // (2) 이번 틱에 지나칠 예정인지(현재 위치→다음 위치 선분이 타겟을 넘는지)
            float dt = Runner.DeltaTime;
            Vector3 dir = toTarget.normalized;

            // 초당 1.2배 가속 (지수적 증가)
            _speed *= Mathf.Pow(ACCEL_MULT_PER_SEC, dt);

            Vector3 nextPos = currPos + dir * (_speed * dt);
            Vector3 toTargetNext = _lastTargetPos - nextPos;

            // 현재는 앞에 있었는데 다음 위치에선 뒤로 가면 "지나침"
            if (Vector3.Dot(toTarget, toTargetNext) <= 0f)
            {
                ExplodeAt(_lastTargetPos);
                return;
            }

            // 이동/회전 갱신
            UpdateVelocityAndFacing();

            _prevPos = currPos;
        }

        private void UpdateVelocityAndFacing()
        {
            Vector3 pos = _rb.position;
            Vector3 dir = (_lastTargetPos - pos);

            if (dir.sqrMagnitude <= 1e-8f)
            {
                SetLinearVelocity(Vector3.zero);
                return;
            }

            dir.Normalize();

            // 항상 진행 방향을 바라보게
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            _rb.MoveRotation(look);

            // 속도 적용
            SetLinearVelocity(dir * _speed);
        }

        private void ExplodeAt(Vector3 explosionPos)
        {
            // 폭발 범위 데미지(호스트만)
            var hits = Physics.OverlapSphere(explosionPos, _explosionRange, _damageableMask, QueryTriggerInteraction.Collide);

            if (hits != null && hits.Length > 0)
            {
                // 같은 오브젝트에 콜라이더가 여러 개면 중복 데미지 방지
                HashSet<IDamageable> damaged = new();

                foreach (var col in hits)
                {
                    if (col == null) continue;

                    // 필요시 GetComponentInParent로 바꿔도 됨
                    if (col.TryGetComponent(out IDamageable d))
                    {
                        if (damaged.Add(d))
                            d.TakeDamage(_damage);
                    }
                }
            }

            // 네트워크 오브젝트는 Destroy가 아니라 Despawn
            Runner.Despawn(Object);
        }

        private void SetLinearVelocity(Vector3 v)
        {
            _rb.linearVelocity = v;
        }
    } 
}