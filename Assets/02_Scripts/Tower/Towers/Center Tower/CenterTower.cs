using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace KIM.Dev
{
    public class CenterTower : Tower
    {
        [Header("발사체")]
        [SerializeField] private Bullet _bullet; // 총알
        [SerializeField] private float _bulletSpeed; // 총알 속도
        [SerializeField] private Transform _firePosition; // 발사 위치

        [Header("스탯")]
        [SerializeField] private float _damage; // 데미지
        [SerializeField] private float _fireRate; // 연사 속도

        [Header("감지")]
        [SerializeField] private LayerMask _layer; // 감지할 레이어
        [SerializeField] private float _range; // 감지 범위

        [Header("속성 이펙트")]
        [SerializeField] private GameObject _flamePropertiesEffect; // 화염 속성 이펙트
        [SerializeField] private GameObject _blitzPropertiesEffect; // 전격 속성 이펙트
        [SerializeField] private GameObject _bioPropertiesEffect; // 생화학 속성 이펙트

        private TowerTargeting _towerTargeting; // 타워의 타겟 감지
        private TowerUpgrade _towerUpgrade; // 타워의 업그레이드

        // 타겟
        private Collider _targetCollider; // 타겟의 콜라이더
        [Networked] private NetworkObject _targetNetworkObj { get; set; } // 타겟의 네트워크 오브젝트

        // 발사
        [Networked] private TickTimer _fireTickTimer { get; set; } // 발사 타이밍을 확인할 타이머
        [Networked, OnChangedRender(nameof(FireBullet))] private int _fireTrigger { get; set; } // 발사 트리거

        // 속성
        private Dictionary<TowerPropertiesType, GameObject> _effects; // 속성에 따른 이펙트

        public TowerPropertiesType PropertiesType => _towerUpgrade.Properties; // 현재 타워의 속성

        protected override void TowerAwake()
        {
            base.TowerAwake();
            _flamePropertiesEffect.SetActive(false);
            _blitzPropertiesEffect.SetActive(false);
            _bioPropertiesEffect.SetActive(false);
            _towerTargeting = new TowerTargeting();
            _towerUpgrade = new TowerUpgrade();

            _effects = new Dictionary<TowerPropertiesType, GameObject>();
            _effects.Add(TowerPropertiesType.Flame, _flamePropertiesEffect);
            _effects.Add(TowerPropertiesType.Blitz, _blitzPropertiesEffect);
            _effects.Add(TowerPropertiesType.Biochemical, _bioPropertiesEffect);
        }

        public override void Spawned()
        {
            base.Spawned();

            // 호스트만 수행
            if (!HasStateAuthority) return;

            // 발사 타이머 초기화
            _fireTickTimer = TickTimer.CreateFromSeconds(Runner, _fireRate);
        }

        public override void FixedUpdateNetwork()
        {
            // 호스트만 수행
            if (!HasStateAuthority) return;

            // 적 감지
            NetworkObject netObj;
            _targetCollider = _towerTargeting.SetTarget(transform.position, _layer, _range, out netObj);
            _targetNetworkObj = netObj;

            // 감지된 적이 있다면 그 방향으로 로테이션
            if (_targetCollider != null)
            {
                _towerTargeting.TowerRotation(transform, _targetCollider, 10f, Runner);
            }

            // 투사체 발사
            if (_fireTickTimer.ExpiredOrNotRunning(Runner) && _targetCollider != null)
            {
                // 틱 갱신
                _fireTickTimer = TickTimer.CreateFromSeconds(Runner, _fireRate);

                // 발사 트리거
                _fireTrigger++;

                // 데미지 주기
                if (_targetCollider.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(_damage);
                }
            }
        }

        private void FireBullet()
        {
            if (_targetNetworkObj != null && _targetNetworkObj.TryGetComponent(out Collider target))
            {
                var bullet = Instantiate(_bullet, _firePosition.position, _firePosition.rotation);
                bullet.InitBullet(target, _bulletSpeed);
            }
        }

        /// <summary>
        /// 타워에 속성 부여하고 속성을 부여했다면 그에 맞는 이펙트 활성화
        /// </summary>
        /// <param name="type">부여할 속성</param>
        /// <returns>속성 부여 성공 여부</returns>
        public bool AddProperties(TowerPropertiesType type)
        {
            bool isAdd = _towerUpgrade.AddProperties(type);
            if (isAdd)
            {
                RPC_EffectsOn(type);
            }
            return isAdd;
        }

        #region RPC

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_EffectsOn(TowerPropertiesType type)
        {
            _effects[type].SetActive(true);
        }

        #endregion
    }
}