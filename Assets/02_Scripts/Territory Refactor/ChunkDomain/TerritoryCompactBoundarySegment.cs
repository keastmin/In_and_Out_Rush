using System;

namespace ProjectIO.Territory
{
    public readonly struct TerritoryCompactBoundarySegment : IEquatable<TerritoryCompactBoundarySegment>
    {
        public TerritoryCompactBoundarySegment(
            TerritoryBoundarySegmentId id,
            TerritoryChunkCoordinate chunk,
            TerritoryChunkLocalPoint start,
            TerritoryChunkLocalPoint end)
        {
            if (!id.IsValid)
                throw new ArgumentException("A compact Boundary segment requires a stable identity.", nameof(id));
            if (start == end)
                throw new ArgumentException("A compact Boundary segment cannot have zero length.");

            Id = id;
            Chunk = chunk;
            Start = start;
            End = end;
        }

        public TerritoryBoundarySegmentId Id { get; }
        public TerritoryChunkCoordinate Chunk { get; }
        public TerritoryChunkLocalPoint Start { get; }
        public TerritoryChunkLocalPoint End { get; }
        public FixedTerritoryPoint GlobalStart => Start.ToGlobal(Chunk);
        public FixedTerritoryPoint GlobalEnd => End.ToGlobal(Chunk);

        public decimal TwiceArea
            => TerritoryBoundaryLoopIndex.Cross(GlobalStart, GlobalEnd);

        public bool Equals(TerritoryCompactBoundarySegment other)
            => Id == other.Id && Chunk == other.Chunk && Start == other.Start && End == other.End;

        public override bool Equals(object obj)
            => obj is TerritoryCompactBoundarySegment other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Id.GetHashCode();
                hash = (hash * 397) ^ Chunk.GetHashCode();
                hash = (hash * 397) ^ Start.GetHashCode();
                return (hash * 397) ^ End.GetHashCode();
            }
        }
    }
}
