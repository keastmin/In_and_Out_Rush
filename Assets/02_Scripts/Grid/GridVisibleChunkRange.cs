using System;
using UnityEngine;

namespace KIM.Dev
{
    public readonly struct GridVisibleChunkRange : IEquatable<GridVisibleChunkRange>
    {
        public readonly GridChunkKey Min;
        public readonly GridChunkKey Max;

        public GridVisibleChunkRange(GridChunkKey min, GridChunkKey max)
        {
            Min = min;
            Max = max;
        }

        public int ChunkWidth => Max.X - Min.X + 1;
        public int ChunkHeight => Max.Y - Min.Y + 1;

        public Vector2Int GetMinCellIndex(GridCalculator calculator)
        {
            return calculator.GetChunkMinCellIndex(Min);
        }

        public int GetCellWidth(GridCalculator calculator)
        {
            return ChunkWidth * calculator.ChunkColumnCount;
        }

        public int GetCellHeight(GridCalculator calculator)
        {
            return ChunkHeight * calculator.ChunkRowCount;
        }

        public bool Equals(GridVisibleChunkRange other)
        {
            return Min == other.Min && Max == other.Max;
        }

        public override bool Equals(object obj)
        {
            return obj is GridVisibleChunkRange other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Min.GetHashCode() * 397) ^ Max.GetHashCode();
            }
        }

        public static bool operator ==(GridVisibleChunkRange left, GridVisibleChunkRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridVisibleChunkRange left, GridVisibleChunkRange right)
        {
            return !left.Equals(right);
        }
    }
}
