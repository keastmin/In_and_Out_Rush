using System;

namespace ProjectIO.Territory
{
    public readonly struct TerritoryChunkExpansionMaterializationMetrics :
        IEquatable<TerritoryChunkExpansionMaterializationMetrics>
    {
        public TerritoryChunkExpansionMaterializationMetrics(
            long trailEdgesRead,
            long replacedBoundarySegmentsRead,
            long boundaryPartsProduced,
            long scanlineEdgeChecks,
            long scanlineIntersectionsConsumed,
            long boundaryChunkExclusions,
            long fullChunkCount,
            int fullRunCount,
            long totalWorkUnits,
            long sourceBoundaryFullScans,
            long sourceFullChunkScans,
            long sourceBoundaryRenumberings)
        {
            TrailEdgesRead = trailEdgesRead;
            ReplacedBoundarySegmentsRead = replacedBoundarySegmentsRead;
            BoundaryPartsProduced = boundaryPartsProduced;
            ScanlineEdgeChecks = scanlineEdgeChecks;
            ScanlineIntersectionsConsumed = scanlineIntersectionsConsumed;
            BoundaryChunkExclusions = boundaryChunkExclusions;
            FullChunkCount = fullChunkCount;
            FullRunCount = fullRunCount;
            TotalWorkUnits = totalWorkUnits;
            SourceBoundaryFullScans = sourceBoundaryFullScans;
            SourceFullChunkScans = sourceFullChunkScans;
            SourceBoundaryRenumberings = sourceBoundaryRenumberings;
        }

        public long TrailEdgesRead { get; }
        public long ReplacedBoundarySegmentsRead { get; }
        public long BoundaryPartsProduced { get; }
        public long ScanlineEdgeChecks { get; }
        public long ScanlineIntersectionsConsumed { get; }
        public long BoundaryChunkExclusions { get; }
        public long FullChunkCount { get; }
        public int FullRunCount { get; }
        public long TotalWorkUnits { get; }
        public long SourceBoundaryFullScans { get; }
        public long SourceFullChunkScans { get; }
        public long SourceBoundaryRenumberings { get; }

        public bool Equals(TerritoryChunkExpansionMaterializationMetrics other)
            => TrailEdgesRead == other.TrailEdgesRead &&
               ReplacedBoundarySegmentsRead == other.ReplacedBoundarySegmentsRead &&
               BoundaryPartsProduced == other.BoundaryPartsProduced &&
               ScanlineEdgeChecks == other.ScanlineEdgeChecks &&
               ScanlineIntersectionsConsumed == other.ScanlineIntersectionsConsumed &&
               BoundaryChunkExclusions == other.BoundaryChunkExclusions &&
               FullChunkCount == other.FullChunkCount &&
               FullRunCount == other.FullRunCount &&
               TotalWorkUnits == other.TotalWorkUnits &&
               SourceBoundaryFullScans == other.SourceBoundaryFullScans &&
               SourceFullChunkScans == other.SourceFullChunkScans &&
               SourceBoundaryRenumberings == other.SourceBoundaryRenumberings;

        public override bool Equals(object obj)
            => obj is TerritoryChunkExpansionMaterializationMetrics other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = TrailEdgesRead.GetHashCode();
                hash = (hash * 397) ^ ReplacedBoundarySegmentsRead.GetHashCode();
                hash = (hash * 397) ^ BoundaryPartsProduced.GetHashCode();
                hash = (hash * 397) ^ ScanlineEdgeChecks.GetHashCode();
                hash = (hash * 397) ^ ScanlineIntersectionsConsumed.GetHashCode();
                hash = (hash * 397) ^ BoundaryChunkExclusions.GetHashCode();
                hash = (hash * 397) ^ FullChunkCount.GetHashCode();
                hash = (hash * 397) ^ FullRunCount;
                hash = (hash * 397) ^ TotalWorkUnits.GetHashCode();
                hash = (hash * 397) ^ SourceBoundaryFullScans.GetHashCode();
                hash = (hash * 397) ^ SourceFullChunkScans.GetHashCode();
                return (hash * 397) ^ SourceBoundaryRenumberings.GetHashCode();
            }
        }
    }
}
