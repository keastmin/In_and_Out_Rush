using Dev.Network;
using Fusion;
using System;
using Unity.Profiling;
using UnityEngine;
using KIM.Dev;

// TODO: 상호작용 여러번 적용되는 현상 수정, 클라이언트 UI에서도 체력이 갱신되도록 수정, 증폭 타워 BuffExit 구현

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerRunnerMovement))]
[RequireComponent(typeof(PlayerRunnerOutOfBodyController))]
[RequireComponent(typeof(PlayerRunnerTeleporter))]
public class PlayerRunner : Player, IDamageable, IBuffReceiver, IHeal, IRunnerLaboratoryUpgradeReceiver
{
    private static readonly ProfilerMarker TerritoryPositionChangedMarker =
        new("PlayerRunner.TerritoryPositionChanged");

    private const float DefaultMaxHealth = 100f;
    private const float DefaultMaxStamina = 100f;
    private const float DefaultSlideStaminaCost = 10f;
    private const float LifelineReturnRadius = 2f;
    private const float WeaponSupplyDamageScalerAmount = 0.1f;
    private const RunnerSkillType DefaultSelectedSkill = RunnerSkillType.Tumble;

    [Header("Statistics")]
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public float Health { get; set; } = 100f;
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public float MaxHealth { get; set; } = DefaultMaxHealth;
    [Networked, OnChangedRender(nameof(OnStaminaChanged))]
    public float Stamina { get; set; } = 100f;
    [Networked, OnChangedRender(nameof(OnStaminaChanged))]
    public float MaxStamina { get; set; } = DefaultMaxStamina;
    [Networked] public float StaminaRecoveryRate { get; set; } = 10f;
    [Networked] public float MovementSpeed { get; set; } = 6f;
    [Networked] public float WeaponDamage { get; set; } = 1f;
    [Networked] public float RunningPower { get; set; } = 30f;
    [Networked] public float DamageReduction { get; set; } = 0f;
    [Networked] public float WeaponDamageScaler { get; set; } = 1f;
    [Networked] public float WeaponAttackSpeedScaler { get; set; } = 1f;
    [Networked] public float WeaponReloadSpeedScaler { get; set; } = 1f;
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] public NetworkBool IsBiodecompositionDeviceActive { get; set; }
    [Networked, OnChangedRender(nameof(OnSelectedSkillChanged))]
    public int SelectedSkill { get; set; } = (int)DefaultSelectedSkill;

    [Header("Slide")]
    [SerializeField] private PlayerRunnerSlideSettings _slideSettings;

    [Header("Weapon")]
    [SerializeField] private MonoBehaviour _weaponBehaviour;

    [Header("Items")]
    [SerializeField] private BarrierWave _barrierPrefab;
    [SerializeField] private Transform _barrierMuzzle;
    [SerializeField] private IncineratorDrone _incineratorDronePrefab;
    [SerializeField] private ElectricGrenadeProjectile _electricGrenadePrefab;
    [SerializeField] private Transform _electricGrenadeMuzzle;
    [SerializeField] private RunnerItemDefinition[] _itemDefinitions =
        RunnerItemInventory.CreateDefaultDefinitions();

    [Header("Biodecomposition Device")]
    [SerializeField, Min(0.01f)] private float _biodecompositionDuration = 30f;
    [SerializeField, Min(0.01f)] private float _biodecompositionDamageInterval = 0.5f;
    [SerializeField, Min(0f)] private float _biodecompositionMaxHealthDamageRate = 0.035f;
    [SerializeField, Min(0f)] private float _slashMonsterContactDamage = 1f;
    [SerializeField, Min(0.01f)] private float _biodecompositionFallbackTrackRadius = 0.4f;
    [SerializeField] private LayerMask _biodecompositionMonsterLayerMask = 1 << 6;
    [SerializeField] private GameObject _biodecompositionSlashVfxPrefab;
    [SerializeField, Min(0.01f)] private float _biodecompositionSlashVfxSampleSpacing = 0.8f;
    [SerializeField, Min(1)] private int _biodecompositionSlashVfxMaxPoints = 64;

    [Header("Ping Guide")]
    [SerializeField] private PlayerRunnerPingGuide _pingGuide; // 핑 가이드 컴포넌트 참조

    [SerializeField] private ParticleSystem _swiftnessParticleEffect;
    public Sprite[] skillIcons;

    private Rigidbody _rigidbody;
    private PlayerRunnerMovement _movement;
    private RunnerItemConsumer _itemConsumer;
    private RunnerItemInventory _itemInventory;
    private RunnerSkillCaster _skillCaster;
    private PlayerRunnerOutOfBodyController _outOfBodyController;
    private PlayerRunnerTeleporter _teleporter;
    private IRunnerWeapon _weapon;

    private PlayerRunnerCombatHandler _combatHandler;
    private PlayerRunnerSlideHandler _slideHandler;
    private PlayerRunnerTumbleHandler _tumbleHandler;
    private PlayerRunnerSwiftnessHandler _swiftnessHandler;
    private PlayerRunnerBuffHandler _buffHandler;
    private PlayerRunnerUpgradeHandler _upgradeHandler;
    private SlashContactDetector _slashContactDetector;
    private PlayerRunnerSlashContactDamageHandler _slashContactDamageHandler;
    private PlayerRunnerBiodecompositionDeviceHandler _biodecompositionDeviceHandler;
    private PlayerRunnerBiodecompositionSlashVfxHandler _biodecompositionSlashVfxHandler;

    private float _elapsedTime = 0f;

    #region 프로퍼티

    // 러너의 핑 가이드
    public PlayerRunnerPingGuide PingGuide => _pingGuide;

    #endregion 

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool _testModeInvincibleEnabled;
#endif
    // 체력 변화시 호출되는 액션
    public event Action<float, float> OnHPValueChanged; // Max HP, Current HP

    public event Action<Vector3, PlayerRunner, object> OnPositionChanged;
    public event Action<PlayerRunner, object> OnDied;
    public float EffectiveMovementSpeed => _buffHandler.GetMovementSpeed(this);
    public float SlideStaminaCost => _slideSettings != null
        ? _slideSettings.StaminaCost
        : DefaultSlideStaminaCost;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public bool IsTestModeInvincible => _testModeInvincibleEnabled;
#endif

    public void OnHealthChanged()
    {
        if (MaxHealth <= 0f) return;

        StageBootstrapper.Instance.UIController.RunnerUI.Display.Player
            .SetHealthBarRatio(Health / MaxHealth);

        // 체력 변화시 호출되는 액션 트리거
        OnHPValueChanged?.Invoke(MaxHealth, Health);
    }

    public void OnStaminaChanged()
    {
        if (MaxStamina <= 0f) return;

        StageBootstrapper.Instance.UIController.RunnerUI.Display.Player
            .SetStaminaBarRatio(Stamina / MaxStamina);
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _movement = GetComponent<PlayerRunnerMovement>();
        _outOfBodyController = GetComponent<PlayerRunnerOutOfBodyController>();
        _teleporter = GetComponent<PlayerRunnerTeleporter>();
        _weapon = _weaponBehaviour as IRunnerWeapon;
        if (_weaponBehaviour != null && _weapon == null)
            Debug.LogWarning($"{nameof(_weaponBehaviour)} must implement {nameof(IRunnerWeapon)}.", this);

        _itemConsumer = new RunnerItemConsumer(new IItemConsumptionStrategy[]
        {
            new SpawnBarrierStrategy(_barrierPrefab, _barrierMuzzle),
            new SpawnIncineratorStrategy(_incineratorDronePrefab),
            new SpawnElectricGrenadeStrategy(_electricGrenadePrefab, _electricGrenadeMuzzle),
            new UseBiodecompositionDeviceStrategy(),
        });
        _itemInventory = new RunnerItemInventory(_itemConsumer, _itemDefinitions);
        _skillCaster = new RunnerSkillCaster();
        _combatHandler = new PlayerRunnerCombatHandler();
        _slideHandler = new PlayerRunnerSlideHandler();
        _tumbleHandler = new PlayerRunnerTumbleHandler();
        _swiftnessHandler = new PlayerRunnerSwiftnessHandler();
        _buffHandler = new PlayerRunnerBuffHandler();
        _upgradeHandler = new PlayerRunnerUpgradeHandler();
        _slashContactDetector = new SlashContactDetector();
        _slashContactDamageHandler = new PlayerRunnerSlashContactDamageHandler();
        _biodecompositionDeviceHandler = new PlayerRunnerBiodecompositionDeviceHandler();
        _biodecompositionSlashVfxHandler = new PlayerRunnerBiodecompositionSlashVfxHandler();
    }

    public override void Spawned()
    {
        base.Spawned();
        _movement.SetTarget(transform);
        if (HasStateAuthority)
        {
            MaxHealth = DefaultMaxHealth;
            Health = MaxHealth;
            MaxStamina = DefaultMaxStamina;
            Stamina = MaxStamina;
            if (Globals.Store != null)
            {
                MovementSpeed = Globals.Store.PlayerRunnerMovementSpeed;
            }

            IsDead = false;
            IsBiodecompositionDeviceActive = false;
            SelectedSkill = (int)DefaultSelectedSkill;
        }
        BuffReceiverRegistry.Register(this, transform, BuffTargetType.Runner);
        RefreshItemSlots();
        RefreshSelectedSkillIcon();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        AttachTestModeGUI();
#endif

        // 스폰 완료 후 각 클라이언트에서 호출되는 스폰 완료 트리거
        if(StageBootstrapper.Instance != null)
            StageBootstrapper.Instance.LocalPlayerRunnerSpawned(this);
    }

    public override void FixedUpdateNetwork()
    {
        if (IsDead)
        {
            if (HasStateAuthority)
            {
                StopMovement();
                _biodecompositionDeviceHandler?.Stop(this);
            }
            return;
        }

        if (HasStateAuthority)
        {
            _buffHandler.Tick(Runner.DeltaTime);
            RecoverStamina(Runner.DeltaTime);
            TerritorySystem territorySystem = StageBootstrapper.Instance != null
                ? StageBootstrapper.Instance.TerritorySystem
                : null;

            _biodecompositionDeviceHandler.Tick(
                this,
                _slashContactDetector,
                territorySystem,
                _biodecompositionDamageInterval,
                _biodecompositionMaxHealthDamageRate,
                _biodecompositionFallbackTrackRadius,
                _biodecompositionMonsterLayerMask);
            _slashContactDamageHandler.Tick(
                this,
                _slashContactDetector,
                territorySystem,
                _biodecompositionDamageInterval,
                _slashMonsterContactDamage,
                _biodecompositionFallbackTrackRadius,
                _biodecompositionMonsterLayerMask,
                !_biodecompositionDeviceHandler.IsActive,
                StageBootstrapper.Instance != null ? StageBootstrapper.Instance.IsRunnerProtectedBySanctuary : null);
        }

        if (!GetInput(out NetworkInputData data)) return;

        HandleMovementInput(data);
        HandleSlideInput(data);
        HandleItemInput(data);
        HandleSkillInput(data);
        HandleInteractInput(data);
        HandleWeaponInput(data);
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

        using (TerritoryPositionChangedMarker.Auto())
        {
            OnPositionChanged?.Invoke(sharedPosition, this, this);
        }

        TerritorySystem territorySystem = StageBootstrapper.Instance != null
            ? StageBootstrapper.Instance.TerritorySystem
            : null;
        _biodecompositionSlashVfxHandler?.Render(
            IsBiodecompositionDeviceActive && !IsDead,
            _biodecompositionSlashVfxPrefab,
            _slashContactDetector,
            territorySystem,
            _biodecompositionSlashVfxSampleSpacing,
            _biodecompositionSlashVfxMaxPoints);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _biodecompositionDeviceHandler?.Stop(this);
        _biodecompositionSlashVfxHandler?.Dispose();
        base.Despawned(runner, hasState);
        BuffReceiverRegistry.Unregister(this);
    }

    private void Update()
    {
        _elapsedTime += Time.deltaTime;
        int minutes = Mathf.FloorToInt(_elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(_elapsedTime % 60f);
        StageBootstrapper.Instance.UIController.RunnerUI.Display.ElapsedTime
            .SetElapsedTimeText($"{minutes:D2}:{seconds:D2}");
    }

    private void HandleMovementInput(NetworkInputData data)
    {
        if (_slideHandler.IsSliding || _tumbleHandler.IsTumbling) return;
        bool isDashing = data.DashInput.IsSet(NetworkInputData.DASH_INPUT);
        Vector3 direction = data.PlayerRunnerDirection.normalized;
        isDashing = TryConsumeDashStamina(isDashing, direction, Runner.DeltaTime);
        _movement.UpdateMovement(EffectiveMovementSpeed, isDashing, direction);
    }

    private bool TryConsumeDashStamina(bool isDashing, Vector3 direction, float deltaTime)
    {
        if (!isDashing) return false;
        if (direction == Vector3.zero) return false;
        if (Stamina <= 0f) return false;

        if (!HasStateAuthority) return true;

        Stamina = Mathf.Max(0f, Stamina - RunningPower * deltaTime);
        OnStaminaChanged();
        return Stamina > 0f;
    }

    private void RecoverStamina(float deltaTime)
    {
        if (Stamina >= MaxStamina) return;

        Stamina = Mathf.Min(MaxStamina, Stamina + StaminaRecoveryRate * deltaTime);
        OnStaminaChanged();
    }

    private void HandleSlideInput(NetworkInputData data)
    {
        if (!data.SlideInput.IsSet(NetworkInputData.SLIDE_INPUT)) return;
        _ = _slideHandler.StartSlide(this, _rigidbody, _swiftnessHandler);
        // RPC_DecreaseHealthTest(50f);
    }

    private void HandleItemInput(NetworkInputData data)
    {
        UpdateSelectedItemSlot(data.SelectedItem);
        if (!data.ItemInput.IsSet(NetworkInputData.ITEM_INPUT)) return;
        if (!_itemInventory.TryUse(data.SelectedItem, new RunnerItemUseContext(this, data.MousePosition))) return;

        RefreshItemSlots();
    }

    private void HandleSkillInput(NetworkInputData data)
    {
        if (!data.SkillInput.IsSet(NetworkInputData.SKILL_INPUT)) return;
        if (_outOfBodyController.IsOutOfBodyActive)
            _outOfBodyController.EndOutOfBody();
        else
            _skillCaster.Cast(GetCurrentSelectedSkill(), this);
    }

    private void HandleInteractInput(NetworkInputData data)
    {
        if (!data.InteractInput.IsSet(NetworkInputData.INTERACT_INPUT)) return;
        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out RaycastHit hit, 3f))
            if (hit.collider.TryGetComponent<IRunnerInteractableTower>(out var interactableTower))
                interactableTower.Interact(this);
        Debug.Log("상호작용 사용");
    }

    private void HandleWeaponInput(NetworkInputData data)
    {
        if (!HasStateAuthority) return;
        if (!data.WeaponInput.IsSet(NetworkInputData.WEAPON_INPUT)) return;

        _weapon?.TryFire(this, data.MousePosition);
    }

    private void UpdateSkillIcon(int skillIndex)
    {
        if (!HasInputAuthority) return;
        if (skillIndex < 1 || skillIndex > skillIcons.Length) return;
        StageBootstrapper.Instance.UIController.RunnerUI.Display.Player
            .SetSkillIcon(skillIcons[skillIndex - 1]);
    }

    private void OnSelectedSkillChanged()
    {
        RefreshSelectedSkillIcon();
    }

    private void RefreshSelectedSkillIcon()
    {
        UpdateSkillIcon(SelectedSkill);
    }

    private void UpdateSelectedItemSlot(int slotIndex)
    {
        if (!HasInputAuthority) return;

        StageBootstrapper.Instance.UIController.RunnerUI.SelectItemSlot(slotIndex);
    }

    private void RefreshItemSlots()
    {
        for (int i = 0; i < RunnerItemInventory.SlotCount; i++)
        {
            RunnerItemSlot slot = _itemInventory.GetSlot(i);
            if (HasInputAuthority)
                SetItemSlotUI(i, slot.ItemType, slot.Count);
            else if (HasStateAuthority)
                RPC_SetItemSlot(i, (int)slot.ItemType, slot.Count);
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void AttachTestModeGUI()
    {
        if (!HasInputAuthority) return;
        if (TryGetComponent<PlayerRunnerTestModeGUI>(out _)) return;

        gameObject.AddComponent<PlayerRunnerTestModeGUI>().Initialize(this);
    }
#endif

    public bool TryActivateLifeline(out Vector3 returnPosition)
    {
        returnPosition = Vector3.zero;

        if (!HasStateAuthority)
            return false;

        if (!_itemInventory.TryConsume(RunnerItemType.Lifeline))
            return false;

        returnPosition = GetLifelineReturnPosition();
        TeleportTo(returnPosition);
        RefreshItemSlots();
        return true;
    }

    private Vector3 GetLifelineReturnPosition()
    {
        Vector3 targetPosition = Vector3.zero;
        var laboratory = StageBootstrapper.Instance != null
            ? StageBootstrapper.Instance.NetworkLaboratory
            : null;

        if (laboratory != null)
            targetPosition = laboratory.transform.position;

        float angle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * LifelineReturnRadius,
            0f,
            Mathf.Sin(angle) * LifelineReturnRadius);

        return targetPosition + offset;
    }

    private void SetItemSlotUI(int slotIndex, RunnerItemType itemType, int count)
    {
        StageBootstrapper.Instance.UIController.RunnerUI.SetItemSlot(slotIndex, itemType, count);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_SetItemSlot(int slotIndex, int itemType, int count)
    {
        SetItemSlotUI(slotIndex, (RunnerItemType)itemType, count);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_DecreaseHealthTest(float amount)
    {
        if (HasStateAuthority) TakeDamage(amount);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetTestModeInvincible(bool enabled)
    {
        if (!HasStateAuthority) return;

        _testModeInvincibleEnabled = enabled;
        _combatHandler.SetTestModeInvincible(this, enabled);
    }
#endif

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestLaboratoryUpgrade(
        int upgradeType,
        int nextLevel,
        float amount,
        Cost cost)
    {
        TryApplyLaboratoryUpgrade(
            (RunnerLaboratoryUpgradeType)upgradeType,
            nextLevel,
            amount,
            cost);
    }

    private bool TryApplyLaboratoryUpgrade(
        RunnerLaboratoryUpgradeType upgradeType,
        int nextLevel,
        float amount,
        Cost cost)
    {
        if (!HasStateAuthority ||
            upgradeType == RunnerLaboratoryUpgradeType.Weapon ||
            !Enum.IsDefined(typeof(RunnerLaboratoryUpgradeType), upgradeType) ||
            nextLevel <= 0 ||
            amount <= 0f)
        {
            return false;
        }

        ResourceSystem resourceSystem = ResourceSystem.Instance;
        if (resourceSystem == null || !resourceSystem.TryDeductCost(cost))
            return false;

        _upgradeHandler.ApplyLaboratoryUpgrade(this, upgradeType, amount);
        return true;
    }

    // IDamageable
    public void TakeDamage(float damage) => _combatHandler.TakeDamage(this, damage);
    public void TakeTrackCompletionDamage(float damage) => _combatHandler.TakeTrackCompletionDamage(this, damage);
    public void TakeSlashDamage(float damage) => _combatHandler.TakeSlashDamage(this, damage);
    public void Kill() => _combatHandler.Kill(this);
    public void StopMovement() => _movement.Stop();
    public void InvokeDiedEvent(PlayerRunner runner, object sender) => OnDied?.Invoke(runner, sender);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void SetTestModeInvincible(bool enabled)
    {
        _testModeInvincibleEnabled = enabled;

        if (HasStateAuthority)
            _combatHandler.SetTestModeInvincible(this, enabled);
        else
            RPC_SetTestModeInvincible(enabled);
    }
#endif

    // IHeal
    public float Heal(float amount) => _combatHandler.Heal(this, amount);
    public void ReceiveArmor(float amount) => _combatHandler.ReceiveArmor(this, amount);

    // IRunnerLaboratoryUpgradeReceiver
    public bool TryRequestLaboratoryUpgrade(RunnerLaboratoryUpgradeRequest request)
    {
        if (!Enum.IsDefined(typeof(RunnerLaboratoryUpgradeType), request.Type) ||
            request.Type == RunnerLaboratoryUpgradeType.Weapon)
            return false;
        if (request.NextLevel <= 0 || request.Amount <= 0f)
            return false;

        if (HasStateAuthority)
        {
            return TryApplyLaboratoryUpgrade(
                request.Type,
                request.NextLevel,
                request.Amount,
                request.Cost);
        }

        RPC_RequestLaboratoryUpgrade(
            (int)request.Type,
            request.NextLevel,
            request.Amount,
            request.Cost);
        return true;
    }

    // IBuffReceiver
    public void BuffEnter(IBuffParam buffParam) => _buffHandler.BuffEnter(this, buffParam);
    public void BuffStay(IBuffParam buffParam) => _buffHandler.BuffStay(this, buffParam);
    public void BuffExit(IBuffParam buffParam) => _buffHandler.BuffExit(this, buffParam);

    // RunnerSkillCaster 호출용 공개 메서드
    public void StartTumble() => _tumbleHandler.StartTumble(this, _rigidbody);
    public void StartInvincibility(float duration) => _combatHandler.StartInvincibility(this, duration);
    public bool TryStartBiodecompositionDevice() =>
        _biodecompositionDeviceHandler.TryStart(this, _biodecompositionDuration);
    public void SetSlashDamageInvincible(bool enabled) =>
        _combatHandler.SetSlashDamageInvincible(this, enabled);
    public void SetBiodecompositionDeviceActive(bool enabled)
    {
        if (!HasStateAuthority || Object == null || !Object.IsValid || !Object.IsInSimulation)
            return;

        IsBiodecompositionDeviceActive = enabled;
    }
    public void StartOutOfBody() => _outOfBodyController.StartOutOfBody();
    public void StartSwiftness() => _swiftnessHandler.StartSwiftness(this, _swiftnessParticleEffect);
    public void TeleportTo(Vector3 position) => _teleporter.TeleportTo(position);
    public void AttackUp(float amount) => _upgradeHandler.AttackUp(this, amount);
    public void SpeedUp(float amount) => _upgradeHandler.SpeedUp(this, amount);
    public bool Supply(IObtainable obtainable) => _upgradeHandler.Supply(this, obtainable);

    public bool TryReceiveRandomItemSupply()
    {
        if (!HasStateAuthority)
            return false;

        int[] availableSlotIndices = new int[RunnerItemInventory.SlotCount];
        int availableSlotCount = 0;
        for (int i = 0; i < RunnerItemInventory.SlotCount; i++)
        {
            RunnerItemSlot slot = _itemInventory.GetSlot(i);
            if (slot.HasUnlimitedCapacity || slot.Count < slot.MaxCount)
            {
                availableSlotIndices[availableSlotCount] = i;
                availableSlotCount++;
            }
        }

        if (availableSlotCount == 0)
            return false;

        int randomIndex = UnityEngine.Random.Range(0, availableSlotCount);
        bool added = _itemInventory.TryAdd(availableSlotIndices[randomIndex], 1);
        if (added)
            RefreshItemSlots();

        return added;
    }

    public bool TryReceiveWeaponSupply()
    {
        if (!HasStateAuthority)
            return false;

        AttackUp(WeaponSupplyDamageScalerAmount);
        return true;
    }

    public bool TryReceiveRandomSkillSupply()
    {
        if (!HasStateAuthority)
            return false;

        RunnerSkillType currentSkill = GetCurrentSelectedSkill();
        RunnerSkillType[] availableSkills =
        {
            RunnerSkillType.Tumble,
            RunnerSkillType.OutOfBody,
            RunnerSkillType.Swiftness,
        };

        int selectableCount = 0;
        for (int i = 0; i < availableSkills.Length; i++)
        {
            if (availableSkills[i] != currentSkill)
                selectableCount++;
        }

        if (selectableCount == 0)
            return true;

        int selectedIndex = UnityEngine.Random.Range(0, selectableCount);
        for (int i = 0; i < availableSkills.Length; i++)
        {
            if (availableSkills[i] == currentSkill)
                continue;

            if (selectedIndex == 0)
            {
                SelectedSkill = (int)availableSkills[i];
                RefreshSelectedSkillIcon();
                return true;
            }

            selectedIndex--;
        }

        return true;
    }

    private RunnerSkillType GetCurrentSelectedSkill()
    {
        RunnerSkillType selectedSkill = (RunnerSkillType)SelectedSkill;
        if (!Enum.IsDefined(typeof(RunnerSkillType), selectedSkill) || selectedSkill == RunnerSkillType.None)
            return DefaultSelectedSkill;

        return selectedSkill;
    }
}
