using System;
using Fusion;
using ProjectIO.RunnerWeapons;
using UnityEngine;

public abstract class RunnerWeaponNetworkBehaviour : NetworkBehaviour, IRunnerWeapon
{
    [Header("Magazine")]
    [SerializeField, Min(1)] private int _magazineCapacity = 16;
    [SerializeField, Min(0.01f)] private float _shotsPerSecond = 4f;
    [SerializeField, Min(0f)] private float _reloadDuration = 2.5f;

    [Networked] private int Ammunition { get; set; }
    [Networked] private TickTimer FireCooldownTimer { get; set; }
    [Networked] private TickTimer ReloadTimer { get; set; }
    [Networked] private NetworkBool ReloadActive { get; set; }
    [Networked] private float ActiveReloadDuration { get; set; }
    [Networked] private int ShotSequence { get; set; }
    [Networked] private Vector3 LastShotDirection { get; set; }

    private bool _presentationInitialized;
    private int _lastPresentedShotSequence;
    private int _lastPresentedAmmunition;
    private bool _lastPresentedReloadActive;

    public RunnerWeaponStatus Status => new(
        Ammunition,
        MagazineCapacity,
        ReloadActive,
        GetReloadProgress01());

    public event Action<RunnerWeaponStatus> StatusChanged;
    public event Action<RunnerWeaponHand, Vector3> ShotPresented;

    protected int MagazineCapacity => Mathf.Max(1, _magazineCapacity);

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Ammunition = MagazineCapacity;
            FireCooldownTimer = TickTimer.None;
            ReloadTimer = TickTimer.None;
            ReloadActive = false;
            ActiveReloadDuration = 0f;
            ShotSequence = 0;
            LastShotDirection = transform.forward;
        }

        InitializePresentationState();
        StatusChanged?.Invoke(Status);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        CompleteReloadIfExpired();
    }

    public override void Render()
    {
        if (!_presentationInitialized)
            InitializePresentationState();

        if (ShotSequence != _lastPresentedShotSequence)
        {
            _lastPresentedShotSequence = ShotSequence;
            ShotPresented?.Invoke(RunnerWeaponRules.GetHand(ShotSequence), LastShotDirection);
        }

        bool reloadActive = ReloadActive;
        if (Ammunition != _lastPresentedAmmunition ||
            reloadActive != _lastPresentedReloadActive)
        {
            _lastPresentedAmmunition = Ammunition;
            _lastPresentedReloadActive = reloadActive;
            StatusChanged?.Invoke(Status);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _presentationInitialized = false;
        StatusChanged = null;
        ShotPresented = null;
        base.Despawned(runner, hasState);
    }

    public void TryFire(PlayerRunner owner, Vector3 targetPosition)
    {
        if (!CanMutateFor(owner))
            return;

        CompleteReloadIfExpired();
        if (ReloadActive)
            return;

        if (RunnerWeaponRules.ShouldStartAutomaticReload(
                Ammunition,
                MagazineCapacity,
                ReloadActive))
        {
            StartReload(owner);
            return;
        }

        if (!FireCooldownTimer.ExpiredOrNotRunning(Runner))
            return;

        int ammunition = Ammunition;
        int shotSequence = ShotSequence;
        if (!RunnerWeaponRules.TryConsumeShot(
                ref ammunition,
                ref shotSequence,
                out RunnerWeaponHand hand))
            return;

        if (!TryExecuteShot(
                owner,
                targetPosition,
                hand,
                out Vector3 shotDirection))
            return;

        Ammunition = ammunition;
        LastShotDirection = shotDirection;
        ShotSequence = shotSequence;
        FireCooldownTimer = TickTimer.CreateFromSeconds(
            Runner,
            RunnerWeaponRules.GetShotInterval(_shotsPerSecond, owner.WeaponAttackSpeedScaler));
    }

    public void TryReload(PlayerRunner owner)
    {
        if (!CanMutateFor(owner))
            return;

        CompleteReloadIfExpired();
        StartReload(owner);
    }

    protected abstract bool TryExecuteShot(
        PlayerRunner owner,
        Vector3 targetPosition,
        RunnerWeaponHand hand,
        out Vector3 shotDirection);

    private bool CanMutateFor(PlayerRunner owner)
    {
        return owner != null &&
               HasStateAuthority &&
               owner.Object != null &&
               owner.Object.IsValid &&
               owner.Object == Object;
    }

    private void StartReload(PlayerRunner owner)
    {
        if (!RunnerWeaponRules.CanStartReload(
                Ammunition,
                MagazineCapacity,
                ReloadActive))
            return;

        float duration = RunnerWeaponRules.GetReloadDuration(
            _reloadDuration,
            owner.WeaponReloadSpeedScaler);

        if (duration <= 0f)
        {
            Ammunition = RunnerWeaponRules.CompleteReload(MagazineCapacity);
            ReloadTimer = TickTimer.None;
            ReloadActive = false;
            ActiveReloadDuration = 0f;
            return;
        }

        ActiveReloadDuration = duration;
        ReloadTimer = TickTimer.CreateFromSeconds(Runner, duration);
        ReloadActive = true;
    }

    private void CompleteReloadIfExpired()
    {
        if (!ReloadActive || !ReloadTimer.Expired(Runner))
            return;

        Ammunition = RunnerWeaponRules.CompleteReload(MagazineCapacity);
        ReloadTimer = TickTimer.None;
        ReloadActive = false;
        ActiveReloadDuration = 0f;
    }

    private float GetReloadProgress01()
    {
        if (!ReloadActive)
            return 0f;

        float? remainingTime = ReloadTimer.RemainingTime(Runner);
        if (!remainingTime.HasValue || ActiveReloadDuration <= 0f)
            return 1f;

        return Mathf.Clamp01(1f - (remainingTime.Value / ActiveReloadDuration));
    }

    private void InitializePresentationState()
    {
        _presentationInitialized = true;
        _lastPresentedShotSequence = ShotSequence;
        _lastPresentedAmmunition = Ammunition;
        _lastPresentedReloadActive = ReloadActive;
    }
}
