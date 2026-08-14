using System;

namespace ProjectIO.Monsters
{
    public readonly struct MonsterChunkCoordinate : IEquatable<MonsterChunkCoordinate>
    {
        public MonsterChunkCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public bool Equals(MonsterChunkCoordinate other)
            => X == other.X && Y == other.Y;

        public override bool Equals(object obj)
            => obj is MonsterChunkCoordinate other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }
    }
}
