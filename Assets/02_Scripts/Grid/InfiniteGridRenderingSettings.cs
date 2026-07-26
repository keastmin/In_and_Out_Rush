using UnityEngine;

namespace KIM.Dev
{
    [System.Serializable]
    public class InfiniteGridRenderingSettings
    {
        [Header("Rendering")]
        [SerializeField] private bool _applyInfiniteGridMaterial = true;
        [SerializeField] private Material _infiniteGridMaterial;

        [Header("Visible Chunks")]
        [SerializeField][Min(4)] private int _chunkColumnCount = 16;
        [SerializeField][Min(4)] private int _chunkRowCount = 16;
        [SerializeField][Min(0)] private int _visibleChunkPadding = 1;
        [SerializeField][Min(1)] private int _maxVisibleCellCount = 262144;
        [SerializeField][Min(1)] private int _maxCachedChunkCount = 256;
        [SerializeField] private Camera _renderCamera;

        public bool ApplyInfiniteGridMaterial => _applyInfiniteGridMaterial;
        public Material InfiniteGridMaterial => _infiniteGridMaterial;
        public int ChunkColumnCount => Mathf.Max(4, _chunkColumnCount);
        public int ChunkRowCount => Mathf.Max(4, _chunkRowCount);
        public int VisibleChunkPadding => Mathf.Max(0, _visibleChunkPadding);
        public int MaxVisibleCellCount => Mathf.Max(1, _maxVisibleCellCount);
        public int MaxCachedChunkCount => Mathf.Max(1, _maxCachedChunkCount);
        public Camera RenderCamera => _renderCamera;
    } 
}
