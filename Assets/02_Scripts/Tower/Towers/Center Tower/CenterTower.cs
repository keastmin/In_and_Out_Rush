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

        [Header("Center Tower Skill")]
        [SerializeField] private GameObject _skillProjectilePrefab;
        [SerializeField] private GameObject _skillExplosionPrefab;
        [SerializeField] private float _skillProjectileSpeed = 10f;
        [SerializeField] private float _skillExplosionRange = 5f;
        [SerializeField] private float _flameSkillCooldown = 60f;
        [SerializeField] private float _blitzSkillCooldown = 45f;
        [SerializeField] private float _bioSkillCooldown = 30f;
        [SerializeField] private float _empStunDuration = 3f;
        [SerializeField] private int _skillVisualPoolPrewarmCount = 3;
        [SerializeField] private float _skillExplosionVisualReleaseDelay = 3f;

        private TowerTargeting _towerTargeting; // 타워의 타겟 감지

        // 타겟
        private Collider _targetCollider; // 타겟의 콜라이더
        [Networked] private NetworkObject _targetNetworkObj { get; set; } // 타겟의 네트워크 오브젝트

        // 발사
        [Networked] private TickTimer _fireTickTimer { get; set; } // 발사 타이밍을 확인할 타이머
        [Networked, OnChangedRender(nameof(FireBullet))] private int _fireTrigger { get; set; } // 발사 트리거

        // 속성
        private Dictionary<TowerPropertiesType, GameObject> _effects; // 속성에 따른 이펙트
        private CenterTowerSkillVisualPool _skillVisualPool;
        private Coroutine _skillProjectileCoroutine;
        private Coroutine _skillExplosionCoroutine;
        private bool _isSkillPending;
        private bool _isSkillCooldownInitialized;

        [Networked] private TickTimer _skillCooldownTimer { get; set; }
        [Networked] private TickTimer _skillImpactTimer { get; set; }
        [Networked] private NetworkObject _skillTargetObject { get; set; }
        [Networked] private Vector3 _skillImpactPosition { get; set; }
        [Networked] private Vector3 _skillExplosionPosition { get; set; }
        [Networked, OnChangedRender(nameof(PlaySkillProjectileVisual))] private int _skillProjectileTrigger { get; set; }
        [Networked, OnChangedRender(nameof(PlaySkillExplosionVisual))] private int _skillExplosionTrigger { get; set; }

        protected override void TowerAwake()
        {
            base.TowerAwake();
            _flamePropertiesEffect.SetActive(false);
            _blitzPropertiesEffect.SetActive(false);
            _bioPropertiesEffect.SetActive(false);
            _towerTargeting = new TowerTargeting();
            _effects = new Dictionary<TowerPropertiesType, GameObject>();
            _effects.Add(TowerPropertiesType.Flame, _flamePropertiesEffect);
            _effects.Add(TowerPropertiesType.Blitz, _blitzPropertiesEffect);
            _effects.Add(TowerPropertiesType.Biochemical, _bioPropertiesEffect);
            _skillVisualPool = new CenterTowerSkillVisualPool(
                transform,
                _skillProjectilePrefab,
                _skillExplosionPrefab,
                _skillVisualPoolPrewarmCount);
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
                TowerPropertyEffectApplier.ApplyDamageAndEffect(
                    _targetCollider,
                    PropertyType,
                    this,
                    _damage,
                    _damage);
            }

            UpdateCenterTowerSkill();
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
            return TryAssignProperty(type, GetCenterPropertyCost(type));
        }

        #region RPC

        protected override void OnTowerPropertyChanged(TowerPropertiesType type)
        {
            base.OnTowerPropertyChanged(type);

            foreach (GameObject effect in _effects.Values)
                effect.SetActive(false);

            if (type != TowerPropertiesType.None && _effects.TryGetValue(type, out GameObject targetEffect))
                targetEffect.SetActive(true);
        }

        #endregion

        protected override Cost GetPropertyAssignmentCost(TowerPropertiesType propertyType, Cost requestedCost)
        {
            return GetCenterPropertyCost(propertyType);
        }

        protected override void TowerDespawned()
        {
            if (_skillProjectileCoroutine != null)
                StopCoroutine(_skillProjectileCoroutine);

            if (_skillExplosionCoroutine != null)
                StopCoroutine(_skillExplosionCoroutine);

            _skillVisualPool?.Dispose();
            _skillVisualPool = null;
            base.TowerDespawned();
        }

        private static Cost GetCenterPropertyCost(TowerPropertiesType type)
        {
            return type switch
            {
                TowerPropertiesType.Flame => new Cost(500, 250),
                TowerPropertiesType.Blitz => new Cost(375, 375),
                TowerPropertiesType.Biochemical => new Cost(250, 500),
                _ => default
            };
        }

        private void UpdateCenterTowerSkill()
        {
            if (PropertyType == TowerPropertiesType.None)
                return;

            InitializeSkillCooldownIfNeeded();
            ResolvePendingSkillIfNeeded();

            if (_isSkillPending || !_skillCooldownTimer.ExpiredOrNotRunning(Runner))
                return;

            Collider skillTarget = SelectSkillTarget(PropertyType);
            if (skillTarget == null)
                return;

            _skillTargetObject = skillTarget.GetComponentInParent<NetworkObject>();
            _skillImpactPosition = skillTarget.transform.position;

            float travelDuration = GetSkillTravelDuration(_skillImpactPosition);
            _skillImpactTimer = TickTimer.CreateFromSeconds(Runner, travelDuration);
            _skillCooldownTimer = TickTimer.CreateFromSeconds(Runner, GetSkillCooldown(PropertyType));
            _isSkillPending = true;
            _skillProjectileTrigger++;
        }

        private void InitializeSkillCooldownIfNeeded()
        {
            if (_isSkillCooldownInitialized)
                return;

            _skillCooldownTimer = TickTimer.CreateFromSeconds(Runner, GetSkillCooldown(PropertyType));
            _isSkillCooldownInitialized = true;
        }

        private void ResolvePendingSkillIfNeeded()
        {
            if (!_isSkillPending || !_skillImpactTimer.Expired(Runner))
                return;

            Vector3 explosionPosition = ResolveSkillTargetPosition();
            _skillExplosionPosition = explosionPosition;
            _skillExplosionTrigger++;
            ApplySkillExplosion(explosionPosition);

            _isSkillPending = false;
            _skillTargetObject = null;
        }

        private float GetSkillTravelDuration(Vector3 targetPosition)
        {
            float speed = Mathf.Max(0.01f, _skillProjectileSpeed);
            Vector3 startPosition = _firePosition != null ? _firePosition.position : transform.position;
            return Mathf.Max(0.05f, Vector3.Distance(startPosition, targetPosition) / speed);
        }

        private float GetSkillCooldown(TowerPropertiesType type)
        {
            return type switch
            {
                TowerPropertiesType.Flame => Mathf.Max(0.01f, _flameSkillCooldown),
                TowerPropertiesType.Blitz => Mathf.Max(0.01f, _blitzSkillCooldown),
                TowerPropertiesType.Biochemical => Mathf.Max(0.01f, _bioSkillCooldown),
                _ => 0.01f
            };
        }

        private Vector3 ResolveSkillTargetPosition()
        {
            if (_skillTargetObject != null)
                return _skillTargetObject.transform.position;

            return _skillImpactPosition;
        }

        private Collider SelectSkillTarget(TowerPropertiesType propertyType)
        {
            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                _range,
                _layer,
                QueryTriggerInteraction.Collide);

            return propertyType == TowerPropertiesType.Biochemical
                ? SelectHighestHealthTrackMonster(colliders)
                : SelectHighestPriorityTrackMonster(colliders);
        }

        private static Collider SelectHighestPriorityTrackMonster(Collider[] colliders)
        {
            Collider target = null;
            int minPriority = int.MaxValue;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (!TryGetTrackMonster(collider, out TrackMonster trackMonster))
                    continue;

                if (trackMonster.Priority < minPriority)
                {
                    minPriority = trackMonster.Priority;
                    target = collider;
                }
            }

            return target;
        }

        private static Collider SelectHighestHealthTrackMonster(Collider[] colliders)
        {
            Collider fallbackTarget = null;
            int fallbackPriority = int.MaxValue;
            Collider healthTarget = null;
            float maxHealth = float.MinValue;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (!TryGetTrackMonster(collider, out TrackMonster trackMonster))
                    continue;

                if (trackMonster.Priority < fallbackPriority)
                {
                    fallbackPriority = trackMonster.Priority;
                    fallbackTarget = collider;
                }

                ICenterTowerSkillTargetInfo targetInfo =
                    collider.GetComponent<ICenterTowerSkillTargetInfo>() ??
                    collider.GetComponentInParent<ICenterTowerSkillTargetInfo>();
                if (targetInfo == null || targetInfo.CurrentHealth <= maxHealth)
                    continue;

                maxHealth = targetInfo.CurrentHealth;
                healthTarget = collider;
            }

            return healthTarget != null ? healthTarget : fallbackTarget;
        }

        private void ApplySkillExplosion(Vector3 explosionPosition)
        {
            Collider[] colliders = Physics.OverlapSphere(
                explosionPosition,
                _skillExplosionRange,
                _layer,
                QueryTriggerInteraction.Collide);
            HashSet<TrackMonster> affectedMonsters = new();

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (!TryGetTrackMonster(collider, out TrackMonster trackMonster) ||
                    !affectedMonsters.Add(trackMonster))
                {
                    continue;
                }

                ApplySkillEffectToTrackMonster(trackMonster, collider, explosionPosition);
            }
        }

        private void ApplySkillEffectToTrackMonster(
            TrackMonster trackMonster,
            Collider hitCollider,
            Vector3 explosionPosition)
        {
            var context = new CenterTowerSkillEffectContext(
                PropertyType,
                this,
                trackMonster,
                explosionPosition,
                _skillExplosionRange,
                _empStunDuration);

            ICenterTowerSkillEffectReceiver receiver =
                hitCollider.GetComponent<ICenterTowerSkillEffectReceiver>() ??
                hitCollider.GetComponentInParent<ICenterTowerSkillEffectReceiver>();
            receiver?.ApplyCenterTowerSkillEffect(context);

            if (PropertyType == TowerPropertiesType.Flame)
            {
                trackMonster.TakeDamage(float.MaxValue);
            }
            else if (PropertyType == TowerPropertiesType.Blitz)
            {
                trackMonster.ApplyStun(_empStunDuration);
            }
        }

        private static bool TryGetTrackMonster(Collider collider, out TrackMonster trackMonster)
        {
            trackMonster = null;
            if (collider == null)
                return false;

            return collider.TryGetComponent(out trackMonster) ||
                   (trackMonster = collider.GetComponentInParent<TrackMonster>()) != null;
        }

        private void PlaySkillProjectileVisual()
        {
            if (_skillProjectileCoroutine != null)
                StopCoroutine(_skillProjectileCoroutine);

            _skillProjectileCoroutine = StartCoroutine(PlaySkillProjectileVisualRoutine());
        }

        private IEnumerator PlaySkillProjectileVisualRoutine()
        {
            GameObject projectile = _skillVisualPool?.AcquireProjectile();
            if (projectile == null)
                yield break;

            Transform projectileTransform = projectile.transform;
            projectileTransform.position = _firePosition != null ? _firePosition.position : transform.position;

            Vector3 targetPosition = ResolveSkillTargetPosition();
            while (Vector3.Distance(projectileTransform.position, targetPosition) > 0.1f)
            {
                targetPosition = ResolveSkillTargetPosition();
                Vector3 direction = targetPosition - projectileTransform.position;
                if (direction.sqrMagnitude <= 0.0001f)
                    break;

                projectileTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                projectileTransform.position = Vector3.MoveTowards(
                    projectileTransform.position,
                    targetPosition,
                    Mathf.Max(0.01f, _skillProjectileSpeed) * Time.deltaTime);
                yield return null;
            }

            _skillVisualPool.ReleaseProjectile(projectile);
            _skillProjectileCoroutine = null;
        }

        private void PlaySkillExplosionVisual()
        {
            if (_skillExplosionCoroutine != null)
                StopCoroutine(_skillExplosionCoroutine);

            _skillExplosionCoroutine = StartCoroutine(PlaySkillExplosionVisualRoutine());
        }

        private IEnumerator PlaySkillExplosionVisualRoutine()
        {
            GameObject explosion = _skillVisualPool?.AcquireExplosion();
            if (explosion == null)
                yield break;

            explosion.transform.position = _skillExplosionPosition;
            ParticleSystem[] particles = explosion.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Clear(true);
                particles[i].Play(true);
            }

            yield return new WaitForSeconds(Mathf.Max(0.01f, _skillExplosionVisualReleaseDelay));

            for (int i = 0; i < particles.Length; i++)
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            _skillVisualPool.ReleaseExplosion(explosion);
            _skillExplosionCoroutine = null;
        }
    }
}
