public sealed class UseBiodecompositionDeviceStrategy : IItemConsumptionStrategy
{
    public RunnerItemType ItemType => RunnerItemType.BiodecompositionDevice;

    public bool TryUse(RunnerItemUseContext context)
    {
        PlayerRunner playerRunner = context.User;
        return playerRunner != null
            && playerRunner.HasStateAuthority
            && playerRunner.TryStartBiodecompositionDevice();
    }
}
