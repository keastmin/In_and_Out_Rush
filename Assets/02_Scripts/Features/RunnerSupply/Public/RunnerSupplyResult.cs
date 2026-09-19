namespace ProjectIO.RunnerSupply
{
    public enum RunnerSupplyResult
    {
        Success,
        Pending,
        NotReady,
        NotBuilder,
        InvalidProduct,
        Locked,
        QueueFull,
        InsufficientResources,
        StateChanged
    }
}
