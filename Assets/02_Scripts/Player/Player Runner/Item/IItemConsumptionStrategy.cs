public readonly struct RunnerItemUseContext
{
    public RunnerItemUseContext(PlayerRunner user)
        : this(user, user != null ? user.transform.position + user.transform.forward : default)
    {
    }

    public RunnerItemUseContext(PlayerRunner user, UnityEngine.Vector3 targetPosition)
    {
        User = user;
        TargetPosition = targetPosition;
    }

    public PlayerRunner User { get; }
    public UnityEngine.Vector3 TargetPosition { get; }
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
