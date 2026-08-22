using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectIO.Territory
{
    public sealed class TerritoryChunkExpansionPlan
    {
        internal TerritoryChunkExpansionPlan(
            ulong sourceRevision,
            ulong sessionId,
            FixedTerritoryPoint exitPoint,
            FixedTerritoryPoint entryPoint,
            int exitBoundarySequence,
            int entryBoundarySequence,
            TerritoryBoundarySegmentId exitBoundaryId,
            TerritoryBoundarySegmentId entryBoundaryId,
            bool boundaryForwardFromEntryToExit,
            decimal resultTwiceArea,
            ReadOnlyCollection<FixedTerritoryPoint> trailPoints,
            ReadOnlyCollection<TerritoryChunkCoordinate> affectedChunks,
            TerritoryChunkExpansionMetrics metrics)
        {
            SourceRevision = sourceRevision;
            SessionId = sessionId;
            ExitPoint = exitPoint;
            EntryPoint = entryPoint;
            ExitBoundarySequence = exitBoundarySequence;
            EntryBoundarySequence = entryBoundarySequence;
            ExitBoundaryId = exitBoundaryId;
            EntryBoundaryId = entryBoundaryId;
            BoundaryForwardFromEntryToExit = boundaryForwardFromEntryToExit;
            ResultTwiceArea = resultTwiceArea;
            TrailPoints = trailPoints ?? throw new ArgumentNullException(nameof(trailPoints));
            AffectedChunks = affectedChunks ?? throw new ArgumentNullException(nameof(affectedChunks));
            Metrics = metrics;
        }

        public ulong SourceRevision { get; }
        public ulong SessionId { get; }
        public FixedTerritoryPoint ExitPoint { get; }
        public FixedTerritoryPoint EntryPoint { get; }
        public int ExitBoundarySequence { get; }
        public int EntryBoundarySequence { get; }
        public TerritoryBoundarySegmentId ExitBoundaryId { get; }
        public TerritoryBoundarySegmentId EntryBoundaryId { get; }
        public bool BoundaryForwardFromEntryToExit { get; }
        public decimal ResultTwiceArea { get; }
        public decimal ResultAbsoluteTwiceArea => Math.Abs(ResultTwiceArea);
        public IReadOnlyList<FixedTerritoryPoint> TrailPoints { get; }
        public IReadOnlyList<TerritoryChunkCoordinate> AffectedChunks { get; }
        public TerritoryChunkExpansionMetrics Metrics { get; }
    }
}
