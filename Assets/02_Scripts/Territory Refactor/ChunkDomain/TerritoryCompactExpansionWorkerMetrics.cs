using System;

namespace ProjectIO.Territory
{
    public readonly struct TerritoryCompactExpansionWorkerMetrics
    {
        public TerritoryCompactExpansionWorkerMetrics(
            int fragmentCount,
            TimeSpan elapsed,
            int workerThreadId,
            TerritoryChunkExpansionMetrics plan,
            TerritoryChunkExpansionMaterializationMetrics materialization,
            TerritoryCompactApplyMetrics apply)
        {
            FragmentCount = fragmentCount;
            Elapsed = elapsed;
            WorkerThreadId = workerThreadId;
            Plan = plan;
            Materialization = materialization;
            Apply = apply;
        }

        public int FragmentCount { get; }
        public TimeSpan Elapsed { get; }
        public int WorkerThreadId { get; }
        public TerritoryChunkExpansionMetrics Plan { get; }
        public TerritoryChunkExpansionMaterializationMetrics Materialization { get; }
        public TerritoryCompactApplyMetrics Apply { get; }
        public long TotalWorkUnits => Materialization.TotalWorkUnits + Apply.TotalWorkUnits;
    }
}
