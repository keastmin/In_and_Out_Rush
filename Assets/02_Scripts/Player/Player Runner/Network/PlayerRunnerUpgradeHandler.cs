using UnityEngine;

public class PlayerRunnerUpgradeHandler
{
    public void AttackUp(PlayerRunner runner, float amount)
    {
        runner.WeaponDamageScaler += amount;
    }

    public void SpeedUp(PlayerRunner runner, float amount)
    {
        runner.MovementSpeed += amount;
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
