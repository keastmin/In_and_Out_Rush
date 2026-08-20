namespace ProjectIO.Construction
{
    public interface ITowerConstructionOperation
    {
        bool TrySpawn();
        bool ValidatePlacement();
        bool TryInitialize();
        bool TryPay();
        void Commit();
        void Rollback();
    }
}
