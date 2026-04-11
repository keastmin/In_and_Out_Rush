using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class GridPlaceable : NetworkBehaviour
{
    [Header("그리드")]
    [SerializeField][Min(0)] protected int _buildRange = 0;
    [SerializeField] protected bool _requireTerritory = true;

    private readonly List<Vector2Int> _occupiedIndices = new();
    private Vector2Int _occupiedCenterIndex;
    private bool _hasOccupiedCenter;

    public int BuildRange => _buildRange;
    public IReadOnlyList<Vector2Int> OccupiedIndices => _occupiedIndices;
    public bool HasGridOccupation => _hasOccupiedCenter;
    protected virtual bool RequireTerritoryOnSpawn => _requireTerritory;

    [Networked]
    public Vector2Int BuiltIndex { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Vector2Int index = InfiniteGrid.Instance.GetCellIndexFromWorldPosition(transform.position);
            TryOccupyAtIndex(index, RequireTerritoryOnSpawn, true);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
    }

    public bool TryOccupyAtIndex(Vector2Int centerIndex, bool requireTerritory = true, bool requireEmpty = true)
    {
        var grid = InfiniteGrid.Instance;
        if (grid == null) return false;

        if (requireEmpty && !grid.CanPlaceAt(centerIndex, _buildRange, null, requireTerritory))
            return false;

        ReleaseGridOccupation();

        if (!grid.AddActiveCell(centerIndex, _buildRange, requireTerritory))
            return false;

        BuiltIndex = centerIndex;
        _occupiedCenterIndex = centerIndex;
        _hasOccupiedCenter = true;

        _occupiedIndices.Clear();
        _occupiedIndices.AddRange(grid.GetCellIndicesInRange(centerIndex, _buildRange));

        return true;
    }

    public void ReleaseGridOccupation()
    {
        if (!_hasOccupiedCenter)
        {
            return;
        }

        var grid = InfiniteGrid.Instance;
        if (grid != null)
        {
            grid.RemoveActiveCell(_occupiedCenterIndex);
        }

        _occupiedIndices.Clear();
        _hasOccupiedCenter = false;
    }
}
