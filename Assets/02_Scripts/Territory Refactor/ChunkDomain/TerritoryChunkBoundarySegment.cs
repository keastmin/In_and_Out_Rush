using System;

namespace ProjectIO.Territory
{
    public readonly struct TerritoryChunkBoundarySegment : IEquatable<TerritoryChunkBoundarySegment>
    {
        public TerritoryChunkBoundarySegment(
            int sequence,
            TerritoryChunkLocalPoint start,
            TerritoryChunkLocalPoint end)
        {
            if (sequence < 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));
            if (start == end)
                throw new ArgumentException("A boundary segment cannot have zero length.");

            Sequence = sequence;
            Start = start;
            End = end;
        }

        public int Sequence { get; }
        public TerritoryChunkLocalPoint Start { get; }
        public TerritoryChunkLocalPoint End { get; }

        public bool Equals(TerritoryChunkBoundarySegment other)
            => Sequence == other.Sequence && Start == other.Start && End == other.End;

        public override bool Equals(object obj)
            => obj is TerritoryChunkBoundarySegment other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Sequence;
                hash = (hash * 397) ^ Start.GetHashCode();
                return (hash * 397) ^ End.GetHashCode();
            }
        }

        public static bool operator ==(
            TerritoryChunkBoundarySegment left,
            TerritoryChunkBoundarySegment right)
            => left.Equals(right);

        public static bool operator !=(
            TerritoryChunkBoundarySegment left,
            TerritoryChunkBoundarySegment right)
            => !left.Equals(right);
    }
}
