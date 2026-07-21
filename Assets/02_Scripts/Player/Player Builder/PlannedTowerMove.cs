using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    internal struct PlannedTowerMove
    {
        public Tower Tower;
        public TowerGhost Ghost;
        public NetworkTransform NetworkTransform;
        public Vector2Int CurrentCenter;
        public Vector2Int TargetCenter;
        public Vector3 TargetPosition;
        public List<Vector2Int> TargetIndices;
    }
}
