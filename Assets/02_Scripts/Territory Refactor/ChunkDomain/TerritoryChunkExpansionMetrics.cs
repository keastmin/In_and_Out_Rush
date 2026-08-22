using System;

namespace ProjectIO.Territory
{
    public readonly struct TerritoryChunkExpansionMetrics : IEquatable<TerritoryChunkExpansionMetrics>
    {
        public TerritoryChunkExpansionMetrics(
            int boundaryIndexSegmentCount,
            int fragmentCount,
            int trailPointCount,
            long boundaryCandidateChecks,
            long trailSelfIntersectionChecks,
            int terminalBoundarySegmentScans,
            int terminalTrailPointScans)
        {
            BoundaryIndexSegmentCount = boundaryIndexSegmentCount;
            FragmentCount = fragmentCount;
            TrailPointCount = trailPointCount;
            BoundaryCandidateChecks = boundaryCandidateChecks;
            TrailSelfIntersectionChecks = trailSelfIntersectionChecks;
            TerminalBoundarySegmentScans = terminalBoundarySegmentScans;
            TerminalTrailPointScans = terminalTrailPointScans;
        }

        public int BoundaryIndexSegmentCount { get; }
        public int FragmentCount { get; }
        public int TrailPointCount { get; }
        public long BoundaryCandidateChecks { get; }
        public long TrailSelfIntersectionChecks { get; }
        public int TerminalBoundarySegmentScans { get; }
        public int TerminalTrailPointScans { get; }

        public bool Equals(TerritoryChunkExpansionMetrics other)
            => BoundaryIndexSegmentCount == other.BoundaryIndexSegmentCount &&
               FragmentCount == other.FragmentCount &&
               TrailPointCount == other.TrailPointCount &&
               BoundaryCandidateChecks == other.BoundaryCandidateChecks &&
               TrailSelfIntersectionChecks == other.TrailSelfIntersectionChecks &&
               TerminalBoundarySegmentScans == other.TerminalBoundarySegmentScans &&
               TerminalTrailPointScans == other.TerminalTrailPointScans;

        public override bool Equals(object obj)
            => obj is TerritoryChunkExpansionMetrics other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = BoundaryIndexSegmentCount;
                hash = (hash * 397) ^ FragmentCount;
                hash = (hash * 397) ^ TrailPointCount;
                hash = (hash * 397) ^ BoundaryCandidateChecks.GetHashCode();
                hash = (hash * 397) ^ TrailSelfIntersectionChecks.GetHashCode();
                hash = (hash * 397) ^ TerminalBoundarySegmentScans;
                return (hash * 397) ^ TerminalTrailPointScans;
            }
        }
    }
}
