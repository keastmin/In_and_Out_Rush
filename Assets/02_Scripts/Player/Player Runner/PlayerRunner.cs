using DG.Tweening;
using Fusion;
using Fusion.Addons.Physics;
using System;
using System.Threading.Tasks;
using UnityEngine;

// TODO: 상호작용 여러번 적용되는 현상 수정, 클라이언트 UI에서도 체력이 갱신되도록 수정, 증폭 타워 BuffExit 구현

[RequireComponent(typeof(Rigidbody))]
public class PlayerRunner : Player, IDamageable, IBuffReceiver, IHeal
{
    private static readonly float MAX_HEALTH = 100f; // 임시

    [Header("Statistics")]
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public float Health { get; set; } = 100f;
    [Networked] public float Stamina { get; set; } = 100f; // 소모 1초 후 회복
    [Networked] public float StaminaRecoveryRate { get; set; } = 10f; // 초당 회복량
    [Networked] public float MovementSpeed { get; set; } = 6f;
    [Networked] public float WeaponDamage { get; set; } = 1f;
    [Networked] public float RunningPower { get; set; } = 30f;
    [Networked] public float DamageReduction { get; set; } = 0f;
    [Networked] public float WeaponDamageScaler { get; set; } = 1f;
    [Networked] public float WeaponAttackSpeedScaler { get; set; } = 1f;
    [Networked] public float WeaponReloadSpeedScaler { get; set; } = 1f;
    [Networked] public NetworkBool IsDead { get; set; }

    // 업그레이드: 체력, 이동 속도, 기력량, 기력 회복 속도, 무기 데미지

    [SerializeField] private ParticleSystem _swiftnessVFX;

    public Sprite[] skillIcons;

    private Rigidbody _rigidbody; // 리지드바디
    private RunnerItemConsumer _itemConsumer; // 아이템 소비자
    private RunnerSkillCaster _skillCaster; // 스킬 시전자
    private bool _isSliding = false; // 슬라이드 상태
    private bool _isTumbling = false; // 텀블 상태
    private bool _isOutOfBody = false; // 영혼 상태
    private Sequence _outOfBodySequence;
    private GameObject _outOfBodySpiritObject;
    private bool _isSwiftness = false; // 스위프트니스 상태
    private int _swiftnessSlideCount = 0; // 스위프트니스 슬라이드 횟수
    private bool _isInvincible = false; // 무적 상태
    private float _elapsedTime = 0f; // 경과 시간

    public event Action<PlayerRunner> OnPositionChanged; // 영역 관련 이벤트

    public void OnHealthChanged()
    {
        StageManager.Instance.UIController.RunnerUI.Display.Player.SetHealthBarRatio(Health / MAX_HEALTH); // UI 체력바 갱신
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _itemConsumer = new RunnerItemConsumer();
        _skillCaster = new RunnerSkillCaster();
    }

    public override void Spawned()
    {
        InitializePlayerRunner();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            // 러너 이동
            if (_isSliding == false && _isTumbling == false)
            {
                float speed = data.DashInput.IsSet(NetworkInputData.DASH_INPUT) ? MovementSpeed * 2f : MovementSpeed;
                data.PlayerRunnerDirection.Normalize();
                _rigidbody.linearVelocity = speed * data.PlayerRunnerDirection;
                transform.LookAt(transform.position + data.PlayerRunnerDirection);
            }

            // 러너 슬라이드
            var slideUsing = data.SlideInput.IsSet(NetworkInputData.SLIDE_INPUT);
            if (slideUsing)
            {
                _ = StartSlide();
                RPC_DecreaseHealthTest(2f);
            }
                
            // 러너 아이템 사용
            var itemUsing = data.ItemInput.IsSet(NetworkInputData.ITEM_INPUT);
            if (itemUsing)
                UseItem(data.SelectedItem);

            // 러너 스킬 사용
            var skillUsing = data.SkillInput.IsSet(NetworkInputData.SKILL_INPUT);
            if (skillUsing)
            {
                if (_isOutOfBody)
                    EndOutOfBody(true); // 영혼 상태일 때 스킬 사용 시 영혼 상태 종료
                else
                    CastSkill(data.SelectedSkill);
            }
            UpdateSkillIcon(data.SelectedSkill);

            // 러너 상호작용
            var interactUsing = data.InteractInput.IsSet(NetworkInputData.INTERACT_INPUT);
            if (interactUsing)
            {
                Interact();
                Debug.Log("상호작용 사용");
            }

            // 러너 연구소 상호작용
            var laboratoryUsing = data.LaboratoryInput.IsSet(NetworkInputData.LABORATORY_INPUT);
            if (laboratoryUsing)
            {
                var lab = StageManager.Instance.Laboratory;
                StageManager.Instance.CinemachineSystem.SetTrackingTarget(lab.transform);
                Debug.Log("연구소 보기");
            }
            else
            {
                StageManager.Instance.CinemachineSystem.SetTrackingTarget(transform);
            }

            var weaponUsing = data.WeaponInput.IsSet(NetworkInputData.WEAPON_INPUT);
            if (weaponUsing)
            {
                Debug.Log("무기 사용"); // 빌더가 구매해서 러너에게 장착시킴
                // Debug.Log(data.SelectedWeapon);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_DecreaseHealthTest(float amount)
    {
        if (HasStateAuthority)
        {
            Health -= amount; // 체력 감소
            // StageManager.Instance.UIController.RunnerUI.Display.Player.SetHealthBarRatio(Health / MAX_HEALTH); // UI 체력바 갱신

            if (Health <= 0f) // 체력이 0 이하라면
                IsDead = true; // 죽음 처리
        }
    }

    public override void Render()
    {
        base.Render(); // vfx, 비주얼적인 요소
        OnPositionChanged?.Invoke(this);
    }

    private void Update()
    {
        _elapsedTime += Time.deltaTime;
        var minutes = Mathf.FloorToInt(_elapsedTime / 60f);
        var seconds = Mathf.FloorToInt(_elapsedTime % 60f);
        StageManager.Instance.UIController.RunnerUI.Display.ElapsedTime.SetElapsedTimeText($"{minutes:D2}:{seconds:D2}");
    }

    // 플레이어 러너 초기화
    private void InitializePlayerRunner()
    {
        if (HasStateAuthority)
        {
            Health = MAX_HEALTH; // MaxHealth는 100f로 하드 코딩
            IsDead = false;
            _isSliding = false;
            _isInvincible = false;
        }
    }

    private async Task StartSlide()
    {
        if (_isSliding) return; // 이미 슬라이드 상태면 무시
        Debug.Log("슬라이드 시작");
        _isSliding = true;
        _rigidbody.linearVelocity = Vector3.zero; // 슬라이드 시작 시 현재 속도 초기화
        _rigidbody.AddForce(3f * MovementSpeed * transform.forward, ForceMode.Impulse);
        var originalDrag = _rigidbody.linearDamping;
        _rigidbody.linearDamping = 2f; // 슬라이드 시 마찰력 증가
        if (_isSwiftness && _swiftnessSlideCount > 0)
        {
            _swiftnessSlideCount--;
            Debug.Log($"스위프트니스 슬라이드 남음: {_swiftnessSlideCount}회");
        }
        else
        {
            Stamina -= 10f;
            StageManager.Instance.UIController.RunnerUI.Display.Player.SetStaminaBarRatio(Stamina / 100f); // UI 기력바 갱신
        }
        await Task.Delay(1000); // 1초 동안 슬라이드 상태 유지
        _rigidbody.linearDamping = originalDrag;
        _isSliding = false;
        Debug.Log("슬라이드 종료");
    }

    private void UseItem(int itemIndex)
    {
        Debug.Log($"아이템 {itemIndex} 사용");

        var runnerItemType = (RunnerItemType)itemIndex;
        _itemConsumer.Use(runnerItemType, this);
    }

    private void CastSkill(int skillIndex)
    {
        Debug.Log($"스킬 {skillIndex} 사용");

        var skillType = (RunnerSkillType)skillIndex;
        _skillCaster.Cast(skillType, this);
    }

    private void UpdateSkillIcon(int skillIndex)
    {
        if (skillIndex < 1 || skillIndex > skillIcons.Length)
            return;
        StageManager.Instance.UIController.RunnerUI.Display.Player.SetSkillIcon(skillIcons[skillIndex - 1]);
    }

    private void Interact()
    {
        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out RaycastHit hit, 3f))
        {
            var hasInteractableTower = hit.collider.TryGetComponent<IRunnerInteractableTower>(out var interactableTower);
            if (hasInteractableTower)
                interactableTower.Interact(this);
        }
    }

    public void StartTumble()
    {
        if (_isTumbling) return; // 이미 텀블 상태면 무시
        _isTumbling = true;
        _rigidbody.linearVelocity = MovementSpeed * transform.forward;
        transform.Find("Root").DOLocalMoveY(2f, 0.5f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutQuad);
        transform.Find("Root").DOLocalRotate(new Vector3(360f, 0f, 0f), 1f, RotateMode.FastBeyond360).SetEase(Ease.Linear);
        DOTween.Sequence()
            .AppendInterval(1.0f)
            .AppendCallback(() => _isTumbling = false);
    }

    public void StartInvincibility(float duration)
    {
        if (!HasStateAuthority) return; // 상태 권한이 없으면 무시 - 호스트만 변수값 변경 가능
        if (_isInvincible) return; // 이미 무적 상태면 무시 ... 처음부터 다시 무적 상태 시작 | 지속시간 추가 | 무시

        Debug.Log("무적 상태 시작");
        _isInvincible = true; // 무적 상태 시작
        _ = EndInvincibilityAfterDelay(duration); // 일정 시간 후 무적 상태 종료
    }

    private async Task EndInvincibilityAfterDelay(float duration)
    {
        await Task.Delay(TimeSpan.FromSeconds(duration));
        if (HasStateAuthority)
            _isInvincible = false; // 무적 상태 종료
        Debug.Log("무적 상태 종료");
    }

    public void StartOutOfBody()
    {
        // 사선은 앞으로 나가는 영혼한테 따라가도록 구현
        if (_isOutOfBody) return; // 이미 영혼 상태면 무시
        _isOutOfBody = true;
        _outOfBodySpiritObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _outOfBodySpiritObject.transform.position = transform.position + transform.forward * 1f;
        _outOfBodySequence = DOTween.Sequence()
            .Append(_outOfBodySpiritObject.transform.DOMove(transform.position + transform.forward * 20f, 2.0f).SetEase(Ease.Linear))
            .AppendCallback(() => EndOutOfBody());
    }

    private void EndOutOfBody(bool forceEnd = false)
    {
        if (!_isOutOfBody) return; // 영혼 상태가 아니면 무시
        _outOfBodySequence.Kill();
        _outOfBodySequence = null;
        _isOutOfBody = false;
        if (forceEnd)
            transform.position = _outOfBodySpiritObject.transform.position;
        Destroy(_outOfBodySpiritObject);
        _outOfBodySpiritObject = null;
    }

    public void StartSwiftness()
    {
        if (_isSwiftness) return; // 이미 스위프트니스 상태면 무시
        _isSwiftness = true;
        WeaponAttackSpeedScaler *= 1.5f; // 공격 속도 50% 증가
        WeaponReloadSpeedScaler *= 1.5f; // 장전 속도 50% 증가
        _swiftnessSlideCount = 3; // 슬라이드 3회 획득
        Debug.Log("스위프트니스 상태 시작");
        _swiftnessVFX.Play();
        DOTween.Sequence()
            .AppendInterval(10.0f) // 10초 지속
            .AppendCallback(() =>
            {
                WeaponAttackSpeedScaler /= 1.5f; // 공격 속도 원래대로
                WeaponReloadSpeedScaler /= 1.5f; // 장전 속도 원래대로
                _isSwiftness = false;
                _swiftnessSlideCount = 0;
                _swiftnessVFX.Stop();
                Debug.Log("스위프트니스 상태 종료");
            });
    }

    public void TakeDamage(float damage)
    {
        if (!HasStateAuthority) return; // 상태 권한이 없으면 무시 - 호스트만 변수값 변경 가능
        if (IsDead) return; // 이미 죽었으면 무시
        if (_isInvincible) return; // 무적 상태면 무시

        Health -= damage; // 체력 감소
        StageManager.Instance.UIController.RunnerUI.Display.Player.SetHealthBarRatio(Health / MAX_HEALTH); // UI 체력바 갱신

        if (Health <= 0f) // 체력이 0 이하라면
            IsDead = true; // 죽음 처리
    }

    #region Runner Upgrade Methods
    public void AttackUp(float amount)
    {
        WeaponDamageScaler += amount;
    }

    public void SpeedUp(float amount)
    {
        MovementSpeed += amount;
    }
    #endregion

    #region BuffReceiver Methods
    public void BuffEnter(IBuffParam buffParam)
    {
        switch (buffParam)
        {
            case AmplificationTowerBuffParam amplificationTowerBuffParam:
                WeaponDamageScaler += amplificationTowerBuffParam.AttackBonus;
                MovementSpeed += amplificationTowerBuffParam.SpeedBonus;
                break;
            default:
                break;
        }
    }
    public void BuffStay(IBuffParam buffParam)
    {
        // 버프 지속 로직 구현
    }

    public void BuffExit(IBuffParam buffParam)
    {
        // 버프 종료 로직 구현
        switch (buffParam)
        {
            case AmplificationTowerBuffParam amplificationTowerBuffParam:
                WeaponDamageScaler -= amplificationTowerBuffParam.AttackBonus;
                MovementSpeed -= amplificationTowerBuffParam.SpeedBonus;
                break;
            default:
                break;
        }
    }
    #endregion

    #region Supply Methods
    public void Supply(IObtainable obtainable)
    {
        switch (obtainable)
        {
            case Item item:
                Debug.Log("아이템 획득");
                break;
            case Weapon weapon:
                Debug.Log("무기 획득");
                break;
            case Skill skill:
                Debug.Log("스킬 획득");
                break;
            default:
                Debug.Log("알 수 없는 획득물");
                break;
        }
    }
    #endregion

    #region Teleport Methods
    public void TeleportTo(Vector3 position)
    {
        RPC_TeleportTo(position);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_TeleportTo(Vector3 position)
    {
        if (HasStateAuthority)
        {
            TryGetComponent(out NetworkRigidbody3D networkRigidbody);
            networkRigidbody.Teleport(position, transform.rotation);
        }
    }

    public void ReceiveArmor(float amount)
    {
        
    }

    public float Heal(float amount)
    {
        if (IsDead) return 0f; // 이미 죽었으면 무시

        float healedAmount = Mathf.Min(amount, MAX_HEALTH - Health);
        Health += healedAmount; // 체력 회복
        StageManager.Instance.UIController.RunnerUI.Display.Player.SetHealthBarRatio(Health / MAX_HEALTH); // UI 체력바 갱신
        return healedAmount;
    }
    #endregion
}
