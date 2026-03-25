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
    public bool IsGridOccupied => _hasOccupiedCenter;

    public override void Spawned()
    {
        //if (ShouldSyncGridState())
        //{
        //    TryOccupyAtWorldPosition(transform.position, _requireTerritory, requireEmpty: true);
        //}
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        //if (ShouldSyncGridState())
        //{
        //    ReleaseGridOccupation();
        //}
    }

    public bool CanPlaceAtWorldPosition(Vector3 worldPosition, bool requireTerritory = true, bool requireEmpty = true)
    {
        var gm = GridManager.Instance;
        if (gm == null) return false;

        Vector2Int center = gm.GetNearestCellIndex(worldPosition);
        return CanPlaceAtIndex(center, requireTerritory, requireEmpty);
    }

    public bool CanPlaceAtIndex(Vector2Int centerIndex, bool requireTerritory = true, bool requireEmpty = true)
    {
        var gm = GridManager.Instance;
        if (gm == null) return false;

        return gm.CanPlaceInRange(
            centerIndex,
            _buildRange,
            requireEmpty: requireEmpty,
            requireTerritory: requireTerritory,
            ignoreOccupiedIndices: _occupiedIndexSet);
    }

    public bool TryOccupyAtWorldPosition(Vector3 worldPosition, bool requireTerritory = true, bool requireEmpty = true)
    {
        var gm = GridManager.Instance;
        if (gm == null) return false;

        Vector2Int center = gm.GetNearestCellIndex(worldPosition);
        return TryOccupyAtIndex(center, requireTerritory, requireEmpty);
    }

    public bool TryOccupyAtIndex(Vector2Int centerIndex, bool requireTerritory = true, bool requireEmpty = true)
    {
        var gm = GridManager.Instance;
        if (gm == null) return false;

        if (!CanPlaceAtIndex(centerIndex, requireTerritory, requireEmpty))
            return false;

        ReleaseGridOccupation();

        if (!gm.SetCellStateInRange(centerIndex, _buildRange, true))
            return false;

        CacheOccupiedState(centerIndex, gm.GetCellIndicesInRange(centerIndex, _buildRange, includeCenter: true));
        return true;
    }

    public bool TryMoveOccupancyToWorldPosition(Vector3 worldPosition, bool requireTerritory = true)
    {
        var gm = GridManager.Instance;
        if (gm == null) return false;

        Vector2Int center = gm.GetNearestCellIndex(worldPosition);
        return TryOccupyAtIndex(center, requireTerritory, requireEmpty: true);
    }

    public void ReleaseGridOccupation()
    {
        if (!_hasOccupiedCenter)
        {
            ClearOccupiedState();
            return;
        }

        var gm = GridManager.Instance;
        if (gm != null)
        {
            gm.SetCellStateInRange(_occupiedCenterIndex, _buildRange, false);
        }

        ClearOccupiedState();
    }

    private void CacheOccupiedState(Vector2Int centerIndex, List<Vector2Int> indices)
    {
        _occupiedCenterIndex = centerIndex;
        _hasOccupiedCenter = true;

        _occupiedIndices.Clear();
        _occupiedIndexSet.Clear();

        if (indices == null) return;

        for (int i = 0; i < indices.Count; i++)
        {
            Vector2Int idx = indices[i];
            _occupiedIndices.Add(idx);
            _occupiedIndexSet.Add(idx);
        }
    }

    private void ClearOccupiedState()
    {
        _hasOccupiedCenter = false;
        _occupiedIndices.Clear();
        _occupiedIndexSet.Clear();
    }

    private bool ShouldSyncGridState()
    {
        if (GridManager.Instance == null) return false;
        if (NetworkManager.Instance == null) return false;
        if (NetworkManager.Instance.Registry == null) return false;
        if (NetworkManager.Instance.Registry.Runner == null) return false;
        if (!NetworkManager.Instance.Registry.RefToPosition.ContainsKey(Runner.LocalPlayer)) return false;

        return NetworkManager.Instance.Registry.IsPlayerBuilder(Runner.LocalPlayer);
    }
}

