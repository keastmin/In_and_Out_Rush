using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    internal struct BuffSourceState
    {
        public Vector2Int CenterIndex;
        public int Range;
        public Color Color;
        public HashSet<Vector2Int> Cells;
    }
}
