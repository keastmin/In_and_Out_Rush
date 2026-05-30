using UnityEngine;

namespace KIM.Dev
{
    [System.Serializable]
    public class InfiniteGridRenderingSettings
    {
        [Header("Rendering")]
        [SerializeField] private bool _applyInfiniteGridMaterial = true;
        [SerializeField] private Material _infiniteGridMaterial;

        public bool ApplyInfiniteGridMaterial => _applyInfiniteGridMaterial;
        public Material InfiniteGridMaterial => _infiniteGridMaterial;
    } 
}
