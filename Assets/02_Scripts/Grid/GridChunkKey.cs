using System;

namespace KIM.Dev
{
    public readonly struct GridChunkKey : IEquatable<GridChunkKey>
    {
        public readonly int X;
        public readonly int Y;

        public GridChunkKey(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public bool Equals(GridChunkKey other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridChunkKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public static bool operator ==(GridChunkKey left, GridChunkKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridChunkKey left, GridChunkKey right)
        {
            return !left.Equals(right);
        }
    }
}
