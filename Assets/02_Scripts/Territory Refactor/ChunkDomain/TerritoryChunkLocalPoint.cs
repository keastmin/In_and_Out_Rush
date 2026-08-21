using System;

namespace ProjectIO.Territory
{
    public readonly struct TerritoryChunkLocalPoint : IEquatable<TerritoryChunkLocalPoint>
    {
        public TerritoryChunkLocalPoint(int x, int y)
        {
            int size = TerritoryChunkCoordinate.SizeInFixedUnits;
            if (x < 0 || x > size)
                throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y > size)
                throw new ArgumentOutOfRangeException(nameof(y));

            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public FixedTerritoryPoint ToGlobal(TerritoryChunkCoordinate chunk)
        {
            long x = chunk.MinimumX + X;
            long y = chunk.MinimumY + Y;
            if (x < int.MinValue || x > int.MaxValue ||
                y < int.MinValue || y > int.MaxValue)
            {
                throw new OverflowException("Chunk-local point exceeds the fixed coordinate range.");
            }

            return new FixedTerritoryPoint((int)x, (int)y);
        }

        public bool Equals(TerritoryChunkLocalPoint other)
            => X == other.X && Y == other.Y;

        public override bool Equals(object obj)
            => obj is TerritoryChunkLocalPoint other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
            => $"({X}, {Y})";

        public static bool operator ==(TerritoryChunkLocalPoint left, TerritoryChunkLocalPoint right)
            => left.Equals(right);

        public static bool operator !=(TerritoryChunkLocalPoint left, TerritoryChunkLocalPoint right)
            => !left.Equals(right);
    }
}
