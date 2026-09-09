using System;

namespace ProjectIO.Territory
{
    public readonly struct FixedTerritoryPoint : IEquatable<FixedTerritoryPoint>
    {
        public const int UnitsPerWorldUnit = 256;

        public FixedTerritoryPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public static FixedTerritoryPoint FromWorld(double x, double y)
            => new(ToFixed(x), ToFixed(y));

        public double WorldX => (double)X / UnitsPerWorldUnit;
        public double WorldY => (double)Y / UnitsPerWorldUnit;

        public bool Equals(FixedTerritoryPoint other)
            => X == other.X && Y == other.Y;

        public override bool Equals(object obj)
            => obj is FixedTerritoryPoint other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
            => $"({X}, {Y})";

        public static bool operator ==(FixedTerritoryPoint left, FixedTerritoryPoint right)
            => left.Equals(right);

        public static bool operator !=(FixedTerritoryPoint left, FixedTerritoryPoint right)
            => !left.Equals(right);

        private static int ToFixed(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Territory coordinates must be finite.");

            double scaled = value * UnitsPerWorldUnit;
            double rounded = Math.Round(scaled, MidpointRounding.AwayFromZero);
            if (rounded < int.MinValue || rounded > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), "Territory coordinate exceeds fixed-point range.");

            return (int)rounded;
        }
    }
}
