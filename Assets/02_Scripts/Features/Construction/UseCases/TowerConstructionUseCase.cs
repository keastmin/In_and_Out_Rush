namespace ProjectIO.Construction
{
    public sealed class TowerConstructionUseCase
    {
        public TowerConstructionResult Execute(ITowerConstructionOperation operation)
        {
            if (operation == null)
                return TowerConstructionResult.InvalidOperation;

            if (!operation.TrySpawn())
                return Rollback(operation, TowerConstructionResult.SpawnFailed);

            if (!operation.ValidatePlacement())
                return Rollback(operation, TowerConstructionResult.PlacementRejected);

            if (!operation.TryInitialize())
                return Rollback(operation, TowerConstructionResult.InitializationFailed);

            if (!operation.TryPay())
                return Rollback(operation, TowerConstructionResult.PaymentFailed);

            operation.Commit();
            return TowerConstructionResult.Success;
        }

        private static TowerConstructionResult Rollback(
            ITowerConstructionOperation operation,
            TowerConstructionResult result)
        {
            operation.Rollback();
            return result;
        }
    }
}
