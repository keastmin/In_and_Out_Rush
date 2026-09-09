using System;

namespace ProjectIO.Territory
{
    public readonly struct TerritoryChunkCoordinate : IEquatable<TerritoryChunkCoordinate>
    {
        public const int SizeInWorldUnits = 8;
        public const int SizeInFixedUnits = SizeInWorldUnits * FixedTerritoryPoint.UnitsPerWorldUnit;

        public TerritoryChunkCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        public long MinimumX => (long)X * SizeInFixedUnits;
        public long MinimumY => (long)Y * SizeInFixedUnits;
        public long MaximumX => MinimumX + SizeInFixedUnits;
        public long MaximumY => MinimumY + SizeInFixedUnits;

        public static TerritoryChunkCoordinate FromPoint(FixedTerritoryPoint point)
            => new(FloorDivide(point.X, SizeInFixedUnits), FloorDivide(point.Y, SizeInFixedUnits));

        public bool Contains(FixedTerritoryPoint point)
            => point.X >= MinimumX && point.X < MaximumX &&
               point.Y >= MinimumY && point.Y < MaximumY;

        public bool ContainsClosed(FixedTerritoryPoint point)
            => point.X >= MinimumX && point.X <= MaximumX &&
               point.Y >= MinimumY && point.Y <= MaximumY;

        public bool Equals(TerritoryChunkCoordinate other)
            => X == other.X && Y == other.Y;

        public override bool Equals(object obj)
            => obj is TerritoryChunkCoordinate other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
            => $"({X}, {Y})";

        public static bool operator ==(TerritoryChunkCoordinate left, TerritoryChunkCoordinate right)
            => left.Equals(right);

        public static bool operator !=(TerritoryChunkCoordinate left, TerritoryChunkCoordinate right)
            => !left.Equals(right);

        private static int FloorDivide(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;
            return remainder < 0 ? quotient - 1 : quotient;
        }
    }
}
