using UnityEngine;

namespace KIM.Dev
{
    public readonly struct GridChunkKey
    {
        public readonly int X;
        public readonly int Y;
        public GridChunkKey(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}