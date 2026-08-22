using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectIO.Territory
{
    public sealed class TerritoryCompactCommitResult
    {
        private readonly ReadOnlyCollection<TerritoryChunkCoordinate> _changedBoundaryChunks;
        private readonly ReadOnlyCollection<int> _changedFullRows;

        internal TerritoryCompactCommitResult(
            ulong previousRevision,
            ulong revision,
            List<TerritoryChunkCoordinate> changedBoundaryChunks,
            List<int> changedFullRows,
            TerritoryCompactApplyMetrics metrics)
        {
            PreviousRevision = previousRevision;
            Revision = revision;
            _changedBoundaryChunks = (changedBoundaryChunks ?? throw new ArgumentNullException(nameof(changedBoundaryChunks))).AsReadOnly();
            _changedFullRows = (changedFullRows ?? throw new ArgumentNullException(nameof(changedFullRows))).AsReadOnly();
            Metrics = metrics;
        }

        public ulong PreviousRevision { get; }
        public ulong Revision { get; }
        public IReadOnlyList<TerritoryChunkCoordinate> ChangedBoundaryChunks => _changedBoundaryChunks;
        public IReadOnlyList<int> ChangedFullRows => _changedFullRows;
        public TerritoryCompactApplyMetrics Metrics { get; }
    }
}
