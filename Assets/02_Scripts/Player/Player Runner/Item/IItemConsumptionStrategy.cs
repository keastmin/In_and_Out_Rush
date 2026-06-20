public readonly struct RunnerItemUseContext
{
    public RunnerItemUseContext(PlayerRunner user)
    {
        User = user;
    }

    public PlayerRunner User { get; }
}

public interface IItemConsumptionStrategy
{
    RunnerItemType ItemType { get; }
    bool TryUse(RunnerItemUseContext context);
}

public interface IRunnerItemConsumer
{
    bool TryUse(RunnerItemType itemType, RunnerItemUseContext context);
}
