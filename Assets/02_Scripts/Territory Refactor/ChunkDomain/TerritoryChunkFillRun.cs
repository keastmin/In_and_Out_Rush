using System;

namespace ProjectIO.Territory
{
    public readonly struct TerritoryChunkFillRun : IEquatable<TerritoryChunkFillRun>
    {
        public TerritoryChunkFillRun(int y, int minimumX, int maximumX)
        {
            if (minimumX > maximumX)
                throw new ArgumentException("A Full run minimum X cannot exceed its maximum X.");

            Y = y;
            MinimumX = minimumX;
            MaximumX = maximumX;
        }

        public int Y { get; }
        public int MinimumX { get; }
        public int MaximumX { get; }
        public long ChunkCount => (long)MaximumX - MinimumX + 1L;

        public bool Contains(TerritoryChunkCoordinate chunk)
            => chunk.Y == Y && chunk.X >= MinimumX && chunk.X <= MaximumX;

        public bool Equals(TerritoryChunkFillRun other)
            => Y == other.Y && MinimumX == other.MinimumX && MaximumX == other.MaximumX;

        public override bool Equals(object obj)
            => obj is TerritoryChunkFillRun other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Y;
                hash = (hash * 397) ^ MinimumX;
                return (hash * 397) ^ MaximumX;
            }
        }

        public static bool operator ==(TerritoryChunkFillRun left, TerritoryChunkFillRun right)
            => left.Equals(right);

        public static bool operator !=(TerritoryChunkFillRun left, TerritoryChunkFillRun right)
            => !left.Equals(right);
    }
}
