using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectIO.Territory
{
    public sealed class TerritoryChunkExpansionMaterialization
    {
        private readonly ReadOnlyCollection<TerritoryChunkBoundaryEdit> _boundaryEdits;
        private readonly ReadOnlyCollection<TerritoryChunkFillRun> _fullRuns;

        internal TerritoryChunkExpansionMaterialization(
            ulong sourceRevision,
            ulong sessionId,
            decimal addedAbsoluteTwiceArea,
            List<TerritoryChunkBoundaryEdit> boundaryEdits,
            List<TerritoryChunkFillRun> fullRuns,
            TerritoryBoundarySplice boundarySplice,
            TerritoryChunkExpansionMaterializationMetrics metrics)
        {
            if (sourceRevision == 0)
                throw new ArgumentOutOfRangeException(nameof(sourceRevision));
            if (sessionId == 0)
                throw new ArgumentOutOfRangeException(nameof(sessionId));
            if (addedAbsoluteTwiceArea <= 0m)
                throw new ArgumentOutOfRangeException(nameof(addedAbsoluteTwiceArea));

            SourceRevision = sourceRevision;
            SessionId = sessionId;
            AddedAbsoluteTwiceArea = addedAbsoluteTwiceArea;
            _boundaryEdits = (boundaryEdits ?? throw new ArgumentNullException(nameof(boundaryEdits))).AsReadOnly();
            _fullRuns = (fullRuns ?? throw new ArgumentNullException(nameof(fullRuns))).AsReadOnly();
            BoundarySplice = boundarySplice;
            Metrics = metrics;
        }

        public ulong SourceRevision { get; }
        public ulong SessionId { get; }
        public decimal AddedAbsoluteTwiceArea { get; }
        public IReadOnlyList<TerritoryChunkBoundaryEdit> BoundaryEdits => _boundaryEdits;
        public IReadOnlyList<TerritoryChunkFillRun> FullRuns => _fullRuns;
        public TerritoryBoundarySplice BoundarySplice { get; }
        public TerritoryChunkExpansionMaterializationMetrics Metrics { get; }
    }
}
