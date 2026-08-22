using System;

namespace ProjectIO.Territory
{
    public readonly struct TerritoryBoundarySegmentId :
        IEquatable<TerritoryBoundarySegmentId>,
        IComparable<TerritoryBoundarySegmentId>
    {
        public TerritoryBoundarySegmentId(ulong value)
        {
            if (value == 0UL)
                throw new ArgumentOutOfRangeException(nameof(value));

            Value = value;
        }

        public ulong Value { get; }
        public bool IsValid => Value != 0UL;

        public int CompareTo(TerritoryBoundarySegmentId other)
            => Value.CompareTo(other.Value);

        public bool Equals(TerritoryBoundarySegmentId other)
            => Value == other.Value;

        public override bool Equals(object obj)
            => obj is TerritoryBoundarySegmentId other && Equals(other);

        public override int GetHashCode()
            => Value.GetHashCode();

        public override string ToString()
            => Value.ToString();

        public static bool operator ==(
            TerritoryBoundarySegmentId left,
            TerritoryBoundarySegmentId right)
            => left.Equals(right);

        public static bool operator !=(
            TerritoryBoundarySegmentId left,
            TerritoryBoundarySegmentId right)
            => !left.Equals(right);
    }
}
