using System;
using System.Threading.Tasks;
using Dev.Network;
using UnityEngine;

public class PlayerRunnerCombatHandler
{
    private bool _timedInvincible;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool _testModeInvincible;
#endif

    public bool IsInvincible =>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _timedInvincible || _testModeInvincible;
#else
        _timedInvincible;
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public bool IsTestModeInvincible => _testModeInvincible;
#endif

    public void TakeDamage(PlayerRunner runner, float damage)
    {
        if (!runner.HasStateAuthority) return;
        if (runner.IsDead) return;
        if (IsInvincible) return;

        runner.Health = Mathf.Max(0f, runner.Health - damage);

        if (runner.Health <= 0f)
            MarkAsDead(runner);

        UpdateHealthUI(runner);
    }

    public void Kill(PlayerRunner runner)
    {
        if (!runner.HasStateAuthority) return;
        if (runner.IsDead) return;

        runner.Health = 0f;
        MarkAsDead(runner);
        UpdateHealthUI(runner);
    }

    public float Heal(PlayerRunner runner, float amount)
    {
        if (runner.IsDead) return 0f;

        float healedAmount = Mathf.Min(amount, runner.MaxHealth - runner.Health);
        runner.Health += healedAmount;
        UpdateHealthUI(runner);
        return healedAmount;
    }

    public void ReceiveArmor(PlayerRunner runner, float amount)
    {
        runner.DamageReduction = Mathf.Min(runner.DamageReduction + amount, 100f);
    }

    public void StartInvincibility(PlayerRunner runner, float duration)
    {
        if (!runner.HasStateAuthority) return;
        if (_timedInvincible) return;

        Debug.Log("무적 상태 시작");
        _timedInvincible = true;
        _ = EndInvincibilityAfterDelay(runner, duration);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void SetTestModeInvincible(PlayerRunner runner, bool enabled)
    {
        if (!runner.HasStateAuthority) return;

        _testModeInvincible = enabled;
    }
#endif

    private async Task EndInvincibilityAfterDelay(PlayerRunner runner, float duration)
    {
        await Task.Delay(TimeSpan.FromSeconds(duration));
        if (runner.HasStateAuthority)
            _timedInvincible = false;
        Debug.Log("무적 상태 종료");
    }

    private void UpdateHealthUI(PlayerRunner runner)
    {
        var playerUI = StageBootstrapper.Instance?.UIController?.RunnerUI?.Display?.Player;
        if (playerUI == null) return;

        playerUI.SetHealthBarRatio(runner.Health / runner.MaxHealth);
    }

    private static void MarkAsDead(PlayerRunner runner)
    {
        runner.IsDead = true;
        runner.StopMovement();
        runner.InvokeDiedEvent(runner, runner);
    }
}
