using Fusion;
using UnityEngine;

public sealed class PlayerRunnerBiodecompositionDeviceHandler
{
    private TickTimer _durationTimer;
    private TickTimer _damageTimer;
    private bool _isActive;

    public bool IsActive => _isActive;

    public bool TryStart(PlayerRunner runner, float duration)
    {
        if (runner == null || !runner.HasStateAuthority || duration <= 0f || _isActive)
            return false;

        _durationTimer = TickTimer.CreateFromSeconds(runner.Runner, duration);
        _damageTimer = TickTimer.None;
        _isActive = true;
        runner.SetSlashDamageInvincible(true);
        runner.SetBiodecompositionDeviceActive(true);
        return true;
    }

    public void Tick(
        PlayerRunner runner,
        SlashContactDetector slashContactDetector,
        TerritorySystem territorySystem,
        float damageInterval,
        float maxHealthDamageRate,
        float fallbackRadius,
        LayerMask monsterLayerMask)
    {
        if (runner == null || !runner.HasStateAuthority)
            return;

        if (_isActive && _durationTimer.Expired(runner.Runner))
        {
            Stop(runner);
            return;
        }

        if (!_isActive)
            return;

        if (!_damageTimer.ExpiredOrNotRunning(runner.Runner))
            return;

        ApplySlashContact(
            slashContactDetector,
            territorySystem,
            maxHealthDamageRate,
            fallbackRadius,
            monsterLayerMask);
        _damageTimer = TickTimer.CreateFromSeconds(runner.Runner, Mathf.Max(0.01f, damageInterval));
    }

    public void Stop(PlayerRunner runner)
    {
        if (!_isActive)
            return;

        _isActive = false;
        _durationTimer = TickTimer.None;
        _damageTimer = TickTimer.None;

        if (runner != null && runner.HasStateAuthority)
        {
            runner.SetSlashDamageInvincible(false);
            runner.SetBiodecompositionDeviceActive(false);
        }
    }

    private void ApplySlashContact(
        SlashContactDetector slashContactDetector,
        TerritorySystem territorySystem,
        float maxHealthDamageRate,
        float fallbackRadius,
        LayerMask monsterLayerMask)
    {
        if (!_isActive || slashContactDetector == null)
            return;

        foreach (WorldMonster monster in slashContactDetector.DetectWorldMonsters(
            territorySystem,
            fallbackRadius,
            monsterLayerMask))
        {
            float maximumHealth = monster.MaxHealth;
            float damage = Mathf.Max(0f, maximumHealth) * Mathf.Max(0f, maxHealthDamageRate);
            if (damage <= 0f)
                continue;

            monster.TakeDamage(damage);
            float currentHealth = 0f;
            if (monster.CanAccessNetworkState)
                monster.TryGetHealthSnapshot(out currentHealth, out maximumHealth);

            Debug.Log(
                $"{nameof(PlayerRunnerBiodecompositionDeviceHandler)} dealt {damage:F2} damage to {monster.name}. " +
                $"HP: {currentHealth:F2}/{maximumHealth:F2}",
                monster);
        }
    }
}
