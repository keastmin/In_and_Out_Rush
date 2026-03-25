using Fusion;
using Grid;
using System.Collections.Generic;
using UnityEngine;

public class GridPlaceable : NetworkBehaviour
{
    [Header("Grid Occupancy")]
    [SerializeField][Min(0)] protected int _buildRange = 0;
    [SerializeField] protected bool _requireTerritory = true;

    private readonly List<Vector2Int> _occupiedIndices = new();
    private readonly HashSet<Vector2Int> _occupiedIndexSet = new();
    private Vector2Int _occupiedCenterIndex;
    private bool _hasOccupiedCenter;

    public int BuildRange => _buildRange;
    public IReadOnlyList<Vector2Int> OccupiedIndices => _occupiedIndices;

    [Networked]
    public Vector2Int BuiltIndex { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Vector2Int index = InfiniteGrid.Instance.GetCellIndexFromWorldPosition(transform.position);
            BuiltIndex = index;
            InfiniteGrid.Instance.AddActiveCell(index, BuildRange);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
    }

    public bool TryOccupyAtIndex(Vector2Int centerIndex, bool requireTerritory = true, bool requireEmpty = true)
    {
        var gm = GridManager.Instance;
        if (gm == null) return false;

        ReleaseGridOccupation();

        if (!gm.SetCellStateInRange(centerIndex, _buildRange, true))
            return false;

        return true;
    }

    public void ReleaseGridOccupation()
    {
        if (!_hasOccupiedCenter)
        {
            return;
        }

        var gm = GridManager.Instance;
        if (gm != null)
        {
            gm.SetCellStateInRange(_occupiedCenterIndex, _buildRange, false);
        }
    }
}
