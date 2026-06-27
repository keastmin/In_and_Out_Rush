using System.Collections.Generic;
using Fusion;
using Dev.Network;
using UnityEngine;
using UnityEngine.UI;

namespace KIM.Dev
{
    public class AttackTower : Tower, ICanClickObject, ICanDragObject, IBuffReceiver
    {
        #region 필드

        [Header("공격")]
        [SerializeField] protected float _attackSpeed = 1f; // 공격 주기
        [SerializeField] protected Transform _attackPosition; // 공격 시작 지점

        [Space(10)]

        [Header("타겟 지정")]
        [SerializeField] protected LayerMask _enemyLayer; // 감지할 의 레이어 마스크
        [SerializeField] protected float _targettingRange = 5f; // 감지 가능한 거리

        [Space(10)]

        [Header("타겟에 대한 동작")]
        [SerializeField] protected float _rotateSpeed = 10f; // 타겟을 바라보는 회전 속도

        [Space(10)]

        [Header("속성 부여")]
        [SerializeField] private GameObject _flameEffect;
        [SerializeField] private GameObject _blitzEffect;
        [SerializeField] private GameObject _bioEffect;

        [Networked] protected TickTimer _attackTick { get; set; } // 공격 주기를 계산하는 타이머
        [Networked] protected NetworkObject _targetObject { get; set; } // 타겟 오브젝트

        [Space(10)]

        // 디버그용 타겟 확인
        [SerializeField] protected Collider _currTarget; // 현재 타겟

        private readonly HashSet<AmplificationTowerBuffParam> _amplificationBuffSources = new();
        private float _maxAttackSpeedBonusRate;

        protected float EffectiveAttackInterval => _attackSpeed / (1f + _maxAttackSpeedBonusRate);
        protected virtual TowerUpgradeType UpgradeType => TowerUpgradeType.SentryGun;

        #endregion

        protected override void TowerAwake()
        {
            base.TowerAwake();
            _flameEffect.SetActive(false);
            _blitzEffect.SetActive(false);
            _bioEffect.SetActive(false);
        }

        public override void Spawned()
        {
            base.Spawned();

            if (HasStateAuthority)
            {
                BuffReceiverRegistry.Register(this, transform, BuffTargetType.Tower);
            }
        }

        protected override void TowerDespawned()
        {
            if (HasStateAuthority)
            {
                BuffReceiverRegistry.Unregister(this);
            }

            _amplificationBuffSources.Clear();
            _maxAttackSpeedBonusRate = 0f;
            base.TowerDespawned();
        }

        // 타겟 설정 메서드
        protected virtual Collider SetTarget()
        {
            Collider target = null;

            // 몬스터 감지
            Collider[] monsterCollider = Physics.OverlapSphere(
                transform.position,
                _targettingRange,
                _enemyLayer);

            // 트랙몬스터이고 우선순위가 더 낮으면 우선 타겟팅
            int minPriority = int.MaxValue;
            foreach (var mc in monsterCollider)
            {
                if (mc.TryGetComponent(out TrackMonster tm))
                {
                    if (tm.Priority < minPriority)
                    {
                        minPriority = tm.Priority;
                        target = mc;
                    }
                }
            }

            // 타겟 동기화
            if (target != null)
            {
                _targetObject = target.GetComponent<NetworkObject>();
            }

            return target;
        }

        // 타겟을 향해 회전하는 메서드
        protected virtual void LookAtTarget(Collider target)
        {
            // Y축만 회전
            if (target != null)
            {
                Vector3 dir = target.transform.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    var look = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Lerp(
                        transform.rotation, look, _rotateSpeed * Runner.DeltaTime);
                }
            }
        }

        protected virtual void Fire() { }

        protected float CalculateDamage(float baseDamage)
        {
            return TowerUpgradeManager != null
                ? TowerUpgradeManager.CalculateDamage(UpgradeType, PropertyType, baseDamage)
                : baseDamage;
        }

        protected void ApplyDamageAndPropertyEffect(Collider target, float baseDamage)
        {
            if (target == null)
                return;

            float finalDamage = CalculateDamage(baseDamage);
            TowerPropertyEffectApplier.ApplyDamageAndEffect(
                target,
                PropertyType,
                this,
                baseDamage,
                finalDamage);
        }

        #region 속성 부여

        /// <summary>
        /// 타워에 속성을 부여함
        /// </summary>
        /// <param name="increaseType">확률이 증가한 타입</param>
        /// <param name="increaseAmount">확률 증가량(10%와 같은 0 ~ 100% 사이 값)</param>
        /// <returns>부여된 속성</returns>
        public TowerPropertiesType AddProperties(TowerPropertiesType increaseType, float increaseAmount = 0f)
        {
            // 이미 속성이 부여되어 있으면 즉시 반환
            if (HasProperty)
            {
                return PropertyType;
            }

            float flameProb = 1f / 3f; // 화염 속성 확률
            float blitzProb = 1f / 3f; // 전격 속성 확률
            float bioProb = 1f / 3f; // 생화학 속성 확률

            // 증가 확률이 있을 경우
            if (increaseType != TowerPropertiesType.None)
            {
                // 증가량 대입
                switch (increaseType)
                {
                    case TowerPropertiesType.Flame: flameProb += increaseAmount / 100f; break;
                    case TowerPropertiesType.Blitz: blitzProb += increaseAmount / 100f; break;
                    case TowerPropertiesType.Biochemical: bioProb += increaseAmount / 100f; break;
                }

                // 남은 확률 분배
                float remain = 1f - (increaseType == TowerPropertiesType.Flame ? flameProb :
                                    (increaseType == TowerPropertiesType.Blitz) ? blitzProb : bioProb);

                float other = remain / 2f;

                if (increaseType != TowerPropertiesType.Flame) flameProb = other;
                if (increaseType != TowerPropertiesType.Blitz) blitzProb = other;
                if (increaseType != TowerPropertiesType.Biochemical) bioProb = other;
            }

            // 속성 부여
            var type = RandomPickProperties(flameProb, blitzProb, bioProb);
            if (!TryAssignProperty(type))
                return PropertyType;

            Debug.Log("속성 부여 완료");

            return type;
        }

        #endregion

        #region ICanDragObject 구현

        public void OnDragSelectedThisObject()
        {
            OnLeftMouseDownThisObject();
        }

        public void OnDragOverThisObject()
        {
            OnCancelClickThisObject();
        }

        public void OnDragCompleteThisObject()
        {
            var manager = StageBootstrapper.Instance;
            if (manager != null)
            {
                // 빌더의 타워 선택을 함수를 호출하여 자신을 선택된 타워로 넘겨줌
                manager.PlayerBuilder.TowerSelected(this);
            }
        }

        #endregion

        #region 버프

        public void BuffEnter(IBuffParam buffParam)
        {
            if (buffParam is not AmplificationTowerBuffParam amplificationParam)
            {
                return;
            }

            _amplificationBuffSources.Add(amplificationParam);
            RefreshAttackSpeedBonus();
        }

        public void BuffStay(IBuffParam buffParam)
        {
        }

        public void BuffExit(IBuffParam buffParam)
        {
            if (buffParam is not AmplificationTowerBuffParam amplificationParam ||
                !_amplificationBuffSources.Remove(amplificationParam))
            {
                return;
            }

            RefreshAttackSpeedBonus();
        }

        private void RefreshAttackSpeedBonus()
        {
            _maxAttackSpeedBonusRate = 0f;
            foreach (AmplificationTowerBuffParam source in _amplificationBuffSources)
            {
                _maxAttackSpeedBonusRate = Mathf.Max(
                    _maxAttackSpeedBonusRate,
                    Mathf.Max(0f, source.TowerAttackSpeedBonus) * 0.01f);
            }
        }

        #endregion

        #region 헬퍼

        // 확률에 따라 부여할 속성 랜덤 뽑기
        private TowerPropertiesType RandomPickProperties(float flameProb, float blitzProb, float bioProb)
        {
            // 모든 확률의 합: 1이거나 그의 근삿값
            float sum = flameProb + blitzProb + bioProb;

            // 정규화
            flameProb /= sum; blitzProb /= sum; bioProb /= sum;

            // 확률에 따라 속성 리턴
            float r = Random.value; // [0, 1)
            if (r < flameProb) return TowerPropertiesType.Flame;
            if (r < flameProb + blitzProb) return TowerPropertiesType.Blitz;
            return TowerPropertiesType.Biochemical;
        }

        protected override void OnTowerPropertyChanged(TowerPropertiesType type)
        {
            base.OnTowerPropertyChanged(type);

            if (type == TowerPropertiesType.None)
            {
                _flameEffect.SetActive(false);
                _blitzEffect.SetActive(false);
                _bioEffect.SetActive(false);
            }
            else if (type == TowerPropertiesType.Flame)
            {
                _flameEffect.SetActive(true);
            }
            else if (type == TowerPropertiesType.Blitz)
            {
                _blitzEffect.SetActive(true);
            }
            else if (type == TowerPropertiesType.Biochemical)
            {
                _bioEffect.SetActive(true);
            }
        }

        #endregion
    }
}
