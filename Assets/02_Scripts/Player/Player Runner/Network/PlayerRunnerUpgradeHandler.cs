using UnityEngine;
using KIM.Dev;

public class PlayerRunnerUpgradeHandler
{
    public void ApplyLaboratoryUpgrade(
        PlayerRunner runner,
        RunnerLaboratoryUpgradeType upgradeType,
        float amount)
    {
        switch (upgradeType)
        {
            case RunnerLaboratoryUpgradeType.Health:
                HealthUp(runner, amount);
                break;
            case RunnerLaboratoryUpgradeType.MoveSpeed:
                SpeedUp(runner, amount);
                break;
            case RunnerLaboratoryUpgradeType.Stamina:
                StaminaUp(runner, amount);
                break;
            case RunnerLaboratoryUpgradeType.StaminaRecovery:
                StaminaRecoveryUp(runner, amount);
                break;
            case RunnerLaboratoryUpgradeType.Weapon:
                AttackUp(runner, amount);
                break;
            default:
                Debug.LogWarning($"Unsupported runner laboratory upgrade type: {upgradeType}");
                break;
        }
    }

    public void HealthUp(PlayerRunner runner, float amount)
    {
        runner.MaxHealth += amount;
        runner.Health = Mathf.Min(runner.Health + amount, runner.MaxHealth);
        runner.OnHealthChanged();
    }

    public void AttackUp(PlayerRunner runner, float amount)
    {
        runner.WeaponDamageScaler += amount;
    }

    public void SpeedUp(PlayerRunner runner, float amount)
    {
        runner.MovementSpeed += amount;
    }

    public void StaminaUp(PlayerRunner runner, float amount)
    {
        runner.MaxStamina += amount;
        runner.Stamina = Mathf.Min(runner.Stamina + amount, runner.MaxStamina);
        runner.OnStaminaChanged();
    }

    public void StaminaRecoveryUp(PlayerRunner runner, float amount)
    {
        runner.StaminaRecoveryRate += amount;
    }

    public void Supply(PlayerRunner runner, IObtainable obtainable)
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
}
