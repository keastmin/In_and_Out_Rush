using Fusion;
using System.Collections;
using Unity.Profiling;
using UnityEngine;

namespace KIM.Dev
{
    public class LaserTower : AttackTower
    {
        private static readonly ProfilerMarker FixedUpdateMarker = new("LaserTower.FixedUpdateNetwork");

        protected override TowerUpgradeType UpgradeType => TowerUpgradeType.LaserBeam;

        [Header("레이저")]
        [SerializeField] private LineRenderer _laser;
        [SerializeField] private float _laserWidth;
        [SerializeField] private float _laserLength;
        [SerializeField] private float _laserDamage;
        [SerializeField] private float _laserWidthDecreaseRate;

        private float _currLaserWidth = 0;
        private Coroutine _laserCo;

        [Networked, OnChangedRender(nameof(OnShotChanged))]
        private int _shotSeq { get; set; }

        public override void Spawned()
        {
            base.Spawned();

            if (HasStateAuthority)
            {
                InitHostLaserTower();
            }

            InitLocalLaserTower();
        }

        public override void FixedUpdateNetwork()
        {
            using (FixedUpdateMarker.Auto())
            {
                if (HasStateAuthority)
                {
                    _currTarget = SetTarget();
                    LookAtTarget(_currTarget);
                    Fire();
                }
            }
        }

        protected override void Fire()
        {
            if (_attackTick.ExpiredOrNotRunning(Runner) && _currTarget != null)
            {
                _attackTick = TickTimer.CreateFromSeconds(Runner, EffectiveAttackInterval);
                _shotSeq++;

                ApplyDamageAndPropertyEffect(_currTarget, _laserDamage);
            }
        }

        private void OnShotChanged()
        {
            // 중첩 방지
            if (_laserCo != null)
                StopCoroutine(_laserCo);

            _laser.enabled = true;
            _laser.positionCount = 2;
            _currLaserWidth = _laserWidth;
            _laserCo = StartCoroutine(LaserDecrease());
        }

        private IEnumerator LaserDecrease()
        {
            // 안전: 혹시 null이면 종료
            if (_laser == null)
                yield break;

            while (_currLaserWidth > 0f)
            {
                _laser.SetPosition(0, _attackPosition.position);
                _laser.SetPosition(1, _attackPosition.position + (_attackPosition.forward * _laserLength));

                _laser.widthMultiplier = _currLaserWidth;

                _currLaserWidth -= Time.deltaTime * _laserWidthDecreaseRate;

                yield return null;
            }

            // 종료 처리
            _currLaserWidth = 0f;
            _laser.widthMultiplier = 0f;
            _laser.enabled = false;

            _laserCo = null;
        }

        private void InitHostLaserTower()
        {
            // 공격 타이머 초기화
            _attackTick = TickTimer.CreateFromSeconds(Runner, EffectiveAttackInterval);
        }

        private void InitLocalLaserTower()
        {
            TryGetComponent(out _laser);
            _laser.enabled = false;
        }
    }
}
