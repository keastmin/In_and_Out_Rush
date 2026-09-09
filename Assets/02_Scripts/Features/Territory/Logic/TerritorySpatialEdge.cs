using UnityEngine;

namespace ProjectIO.Territory
{
    public readonly struct TerritorySpatialEdge
    {
        public TerritorySpatialEdge(int index, Vector2 start, Vector2 end)
        {
            Index = index;
            Start = start;
            End = end;
            Minimum = Vector2.Min(start, end);
            Maximum = Vector2.Max(start, end);
        }

        public int Index { get; }
        public Vector2 Start { get; }
        public Vector2 End { get; }
        public Vector2 Minimum { get; }
        public Vector2 Maximum { get; }

        public bool Overlaps(Vector2 minimum, Vector2 maximum, float padding)
            => Maximum.x >= minimum.x - padding && Minimum.x <= maximum.x + padding &&
               Maximum.y >= minimum.y - padding && Minimum.y <= maximum.y + padding;
    }
}
