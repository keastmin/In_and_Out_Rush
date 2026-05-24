using Fusion;
using UnityEngine;

namespace KIM.Dev
{
    [CreateAssetMenu(fileName = "TowerData", menuName = "Scriptable Objects/TowerData")]
    public class TowerData : ScriptableObject
    {
        public Tower Tower;
        public TowerGhost TowerGhost;
        public NetworkPrefabRef TowerPrefabRef;
    }
}