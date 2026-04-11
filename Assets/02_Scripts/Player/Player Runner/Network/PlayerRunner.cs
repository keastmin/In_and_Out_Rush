using Dev.Network;
using Fusion;
using System;
using UnityEngine;

// TODO: 상호작용 여러번 적용되는 현상 수정, 클라이언트 UI에서도 체력이 갱신되도록 수정, 증폭 타워 BuffExit 구현

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerRunnerMovement))]
[RequireComponent(typeof(PlayerRunnerOutOfBodyController))]
[RequireComponent(typeof(PlayerRunnerTeleporter))]
public class PlayerRunner : Player, IDamageable, IBuffReceiver, IHeal
{
    public const float MaxHealth = 100f;

    [Header("Statistics")]
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public float Health { get; set; } = 100f;
    [Networked] public float Stamina { get; set; } = 100f;
    [Networked] public float StaminaRecoveryRate { get; set; } = 10f;
    [Networked] public float MovementSpeed { get; set; } = 6f;
    [Networked] public float WeaponDamage { get; set; } = 1f;
    [Networked] public float RunningPower { get; set; } = 30f;
    [Networked] public float DamageReduction { get; set; } = 0f;
    [Networked] public float WeaponDamageScaler { get; set; } = 1f;
    [Networked] public float WeaponAttackSpeedScaler { get; set; } = 1f;
    [Networked] public float WeaponReloadSpeedScaler { get; set; } = 1f;
    [Networked] public NetworkBool IsDead { get; set; }

    [SerializeField] private ParticleSystem _swiftnessParticleEffect;
    public Sprite[] skillIcons;

    private Rigidbody _rigidbody;
    private PlayerRunnerMovement _movement;
    private RunnerItemConsumer _itemConsumer;
    private RunnerSkillCaster _skillCaster;
    private PlayerRunnerOutOfBodyController _outOfBodyController;
    private PlayerRunnerTeleporter _teleporter;

    private PlayerRunnerCombatHandler _combatHandler;
    private PlayerRunnerSlideHandler _slideHandler;
    private PlayerRunnerTumbleHandler _tumbleHandler;
    private PlayerRunnerSwiftnessHandler _swiftnessHandler;
    private PlayerRunnerBuffHandler _buffHandler;
    private PlayerRunnerUpgradeHandler _upgradeHandler;

    private float _elapsedTime = 0f;

    public event Action<Vector3, PlayerRunner, object> OnPositionChanged;
    public event Action<PlayerRunner, object> OnDied;
    public event Action<PlayerRunner, object> OnLaboratoryLookStarted;
    public event Action<PlayerRunner, object> OnLaboratoryLookEnded;

    public void OnHealthChanged()
    {
        StageManager.Instance.UIController.RunnerUI.Display.Player
            .SetHealthBarRatio(Health / MaxHealth);
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _movement = GetComponent<PlayerRunnerMovement>();
        _outOfBodyController = GetComponent<PlayerRunnerOutOfBodyController>();
        _teleporter = GetComponent<PlayerRunnerTeleporter>();
        _itemConsumer = new RunnerItemConsumer();
        _skillCaster = new RunnerSkillCaster();
        _combatHandler = new PlayerRunnerCombatHandler();
        _slideHandler = new PlayerRunnerSlideHandler();
        _tumbleHandler = new PlayerRunnerTumbleHandler();
        _swiftnessHandler = new PlayerRunnerSwiftnessHandler();
        _buffHandler = new PlayerRunnerBuffHandler();
        _upgradeHandler = new PlayerRunnerUpgradeHandler();
    }

    public override void Spawned()
    {
        base.Spawned();
        _movement.SetTarget(transform);
        if (HasStateAuthority)
        {
            Health = MaxHealth;
            IsDead = false;
        }
        BuffReceiverRegistry.Register(this, transform, BuffTargetType.Runner);
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData data)) return;

        HandleMovementInput(data);
        HandleSlideInput(data);
        HandleItemInput(data);
        HandleSkillInput(data);
        HandleInteractInput(data);
        HandleLaboratoryInput(data);
    }

    public override void Render()
    {
        base.Render();
        var targetTransform = _outOfBodyController.CurrentTargetTransform;
        var sharedPosition = targetTransform.position;

        if (_outOfBodyController.IsRecovering)
        {
            var recoveryPosition = _outOfBodyController.RecoveryTargetPosition;
            if (Vector3.Distance(targetTransform.position, recoveryPosition) > Vector3.kEpsilon)
                sharedPosition = recoveryPosition;
            else
                _outOfBodyController.ClearRecovering();
        }

        OnPositionChanged?.Invoke(sharedPosition, this, this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        BuffReceiverRegistry.Unregister(this);
    }

    private void Update()
    {
        _elapsedTime += Time.deltaTime;
        int minutes = Mathf.FloorToInt(_elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(_elapsedTime % 60f);
        StageManager.Instance.UIController.RunnerUI.Display.ElapsedTime
            .SetElapsedTimeText($"{minutes:D2}:{seconds:D2}");
    }

    private void HandleMovementInput(NetworkInputData data)
    {
        if (_slideHandler.IsSliding || _tumbleHandler.IsTumbling) return;
        bool isDashing = data.DashInput.IsSet(NetworkInputData.DASH_INPUT);
        Vector3 direction = data.PlayerRunnerDirection.normalized;
        _movement.UpdateMovement(isDashing, direction);
    }

    private void HandleSlideInput(NetworkInputData data)
    {
        if (!data.SlideInput.IsSet(NetworkInputData.SLIDE_INPUT)) return;
        _ = _slideHandler.StartSlide(this, _rigidbody, _swiftnessHandler);
        RPC_DecreaseHealthTest(50f);
    }

    private void HandleItemInput(NetworkInputData data)
    {
        if (!data.ItemInput.IsSet(NetworkInputData.ITEM_INPUT)) return;
        _itemConsumer.Use((RunnerItemType)data.SelectedItem, this);
    }

    private void HandleSkillInput(NetworkInputData data)
    {
        UpdateSkillIcon(data.SelectedSkill);
        if (!data.SkillInput.IsSet(NetworkInputData.SKILL_INPUT)) return;
        if (_outOfBodyController.IsOutOfBodyActive)
            _outOfBodyController.EndOutOfBody();
        else
            _skillCaster.Cast((RunnerSkillType)data.SelectedSkill, this);
    }

    private void HandleInteractInput(NetworkInputData data)
    {
        if (!data.InteractInput.IsSet(NetworkInputData.INTERACT_INPUT)) return;
        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out RaycastHit hit, 3f))
            if (hit.collider.TryGetComponent<IRunnerInteractableTower>(out var interactableTower))
                interactableTower.Interact(this);
        Debug.Log("상호작용 사용");
    }

    private void HandleLaboratoryInput(NetworkInputData data)
    {
        if (StageManager.Instance.CinemachineSystem == null) return;
        if (!data.LaboratoryInput.IsSet(NetworkInputData.LABORATORY_INPUT))
            StageManager.Instance.CinemachineSystem.SetTrackingTarget(transform);
    }

    private void UpdateSkillIcon(int skillIndex)
    {
        if (skillIndex < 1 || skillIndex > skillIcons.Length) return;
        StageManager.Instance.UIController.RunnerUI.Display.Player
            .SetSkillIcon(skillIcons[skillIndex - 1]);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_DecreaseHealthTest(float amount)
    {
        if (HasStateAuthority) TakeDamage(amount);
    }

    // IDamageable
    public void TakeDamage(float damage) => _combatHandler.TakeDamage(this, damage);
    public void InvokeDiedEvent(PlayerRunner runner, object sender) => OnDied?.Invoke(runner, sender);

    // IHeal
    public float Heal(float amount) => _combatHandler.Heal(this, amount);
    public void ReceiveArmor(float amount) => _combatHandler.ReceiveArmor(this, amount);

    // IBuffReceiver
    public void BuffEnter(IBuffParam buffParam) => _buffHandler.BuffEnter(this, buffParam);
    public void BuffStay(IBuffParam buffParam) => _buffHandler.BuffStay(this, buffParam);
    public void BuffExit(IBuffParam buffParam) => _buffHandler.BuffExit(this, buffParam);

    // RunnerSkillCaster 호출용 공개 메서드
    public void StartTumble() => _tumbleHandler.StartTumble(this, _rigidbody);
    public void StartInvincibility(float duration) => _combatHandler.StartInvincibility(this, duration);
    public void StartOutOfBody() => _outOfBodyController.StartOutOfBody();
    public void StartSwiftness() => _swiftnessHandler.StartSwiftness(this, _swiftnessParticleEffect);
    public void TeleportTo(Vector3 position) => _teleporter.TeleportTo(position);
    public void AttackUp(float amount) => _upgradeHandler.AttackUp(this, amount);
    public void SpeedUp(float amount) => _upgradeHandler.SpeedUp(this, amount);
    public void Supply(IObtainable obtainable) => _upgradeHandler.Supply(this, obtainable);
}
