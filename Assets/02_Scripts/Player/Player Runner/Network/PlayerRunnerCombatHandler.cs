using System;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerRunnerCombatHandler
{
    private bool _isInvincible;

    public bool IsInvincible => _isInvincible;

    public void TakeDamage(PlayerRunner runner, float damage)
    {
        if (!runner.HasStateAuthority) return;
        if (runner.IsDead) return;
        if (_isInvincible) return;

        runner.Health -= damage;
        StageManager.Instance.UIController.RunnerUI.Display.Player
            .SetHealthBarRatio(runner.Health / PlayerRunner.MaxHealth);

        if (runner.Health <= 0f)
        {
            runner.IsDead = true;
            runner.InvokeDiedEvent(runner, runner);
        }
    }

    public float Heal(PlayerRunner runner, float amount)
    {
        if (runner.IsDead) return 0f;

        float healedAmount = Mathf.Min(amount, PlayerRunner.MaxHealth - runner.Health);
        runner.Health += healedAmount;
        StageManager.Instance.UIController.RunnerUI.Display.Player
            .SetHealthBarRatio(runner.Health / PlayerRunner.MaxHealth);
        return healedAmount;
    }

    public void ReceiveArmor(PlayerRunner runner, float amount)
    {
        runner.DamageReduction = Mathf.Min(runner.DamageReduction + amount, 100f);
    }

    public void StartInvincibility(PlayerRunner runner, float duration)
    {
        if (!runner.HasStateAuthority) return;
        if (_isInvincible) return;

        Debug.Log("무적 상태 시작");
        _isInvincible = true;
        _ = EndInvincibilityAfterDelay(runner, duration);
    }

    private async Task EndInvincibilityAfterDelay(PlayerRunner runner, float duration)
    {
        await Task.Delay(TimeSpan.FromSeconds(duration));
        if (runner.HasStateAuthority)
            _isInvincible = false;
        Debug.Log("무적 상태 종료");
    }
}
