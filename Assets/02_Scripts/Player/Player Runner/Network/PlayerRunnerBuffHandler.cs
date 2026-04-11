public class PlayerRunnerBuffHandler
{
    public void BuffEnter(PlayerRunner runner, IBuffParam buffParam)
    {
        switch (buffParam)
        {
            case AmplificationTowerBuffParam amplificationTowerBuffParam:
                runner.WeaponDamageScaler += amplificationTowerBuffParam.AttackBonus;
                runner.MovementSpeed += amplificationTowerBuffParam.SpeedBonus;
                break;
            default:
                break;
        }
    }

    public void BuffStay(PlayerRunner runner, IBuffParam buffParam)
    {
        // 버프 지속 로직 구현
    }

    public void BuffExit(PlayerRunner runner, IBuffParam buffParam)
    {
        switch (buffParam)
        {
            case AmplificationTowerBuffParam amplificationTowerBuffParam:
                runner.WeaponDamageScaler -= amplificationTowerBuffParam.AttackBonus;
                runner.MovementSpeed -= amplificationTowerBuffParam.SpeedBonus;
                break;
            default:
                break;
        }
    }
}
