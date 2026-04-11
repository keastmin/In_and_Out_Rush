using UnityEngine;

[System.Serializable]
public class InfiniteGridLayoutSettings
{
    [Header("Grid")]
    [SerializeField] private bool _useTransformAsOrigin = true;
    [SerializeField] private Vector2 _gridOrigin = Vector2.zero;
    [SerializeField][Min(0.001f)] private float _cellSize = 1.6f;
    [SerializeField][Range(0f, 1f)] private float _cellFill = 0.2f;
    [SerializeField] private bool _showCellStateOverlay = true;

    public float CellSize => _cellSize;
    public float CellFill => _cellFill;
    public bool ShowCellStateOverlay => _showCellStateOverlay;

    public void SetCellStateOverlay(bool enabled)
    {
        _showCellStateOverlay = enabled;
    }

    public Vector3 ResolveOrigin(Transform ownerTransform)
    {
        if (_useTransformAsOrigin)
        {
            return ownerTransform.position + new Vector3(_gridOrigin.x, 0f, _gridOrigin.y);
        }

        return Vector3.zero;
    }
}
