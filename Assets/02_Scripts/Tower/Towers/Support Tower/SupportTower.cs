using Grid;
using UnityEngine;

public class SupportTower : Tower
{
    [Header("Grid Buff Cells")]
    [SerializeField][Min(0)] protected int _buffCellRange = 1;
    [SerializeField] private bool _showLegacyBuffRangeCircle = false;
    [SerializeField] private Color _buffCellColor = new Color(1f, 0.85f, 0.15f, 0.35f);

    private int _buffSourceId;
    private bool _isBuffSourceRegistered;
    private Vector2Int _lastBuffCenterIndex = new Vector2Int(int.MinValue, int.MinValue);
    private int _lastBuffCellRange = -1;

    protected virtual bool EmitBuffCells => false;
    protected int BuffSourceId => _buffSourceId;
    protected virtual Color BuffCellColor => _buffCellColor;

    protected override void TowerSpawned()
    {
        base.TowerSpawned();

        if (_buffRangeTransform != null)
        {
            _buffRangeTransform.gameObject.SetActive(_showLegacyBuffRangeCircle);
        }

        SyncBuffCellSource();
    }

    protected override void TowerDespawned()
    {
        base.TowerDespawned();
        ReleaseBuffCellSource();
    }

    public override void Render()
    {
        base.Render();
        SyncBuffCellSource();
    }

    protected void SyncBuffCellSource()
    {
        if (!EmitBuffCells) return;

        if (!TryGetGridManager(out GridManager gm)) return;

        if (_buffSourceId == 0)
        {
            _buffSourceId = GetInstanceID();
        }

        Vector2Int centerIndex = gm.GetNearestCellIndex(transform.position);
        int range = Mathf.Max(0, _buffCellRange);

        if (_isBuffSourceRegistered && _lastBuffCenterIndex == centerIndex && _lastBuffCellRange == range)
        {
            return;
        }

        gm.RegisterOrUpdateBuffSource(_buffSourceId, centerIndex, range, BuffCellColor);
        _isBuffSourceRegistered = true;
        _lastBuffCenterIndex = centerIndex;
        _lastBuffCellRange = range;
    }

    protected void ReleaseBuffCellSource()
    {
        if (!_isBuffSourceRegistered) return;

        if (TryGetGridManager(out GridManager gm))
        {
            gm.RemoveBuffSource(_buffSourceId);
        }

        _isBuffSourceRegistered = false;
        _lastBuffCenterIndex = new Vector2Int(int.MinValue, int.MinValue);
        _lastBuffCellRange = -1;
    }

    private void OnDestroy()
    {
        ReleaseBuffCellSource();
    }

    protected bool TryGetGridManager(out GridManager gridManager)
    {
        gridManager = GridManager.Instance;
        if (gridManager == null)
        {
            gridManager = UnityEngine.Object.FindFirstObjectByType<GridManager>();
        }

        return gridManager != null;
    }
}

