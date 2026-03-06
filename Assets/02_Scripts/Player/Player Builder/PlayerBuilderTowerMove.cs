using Fusion;
using Grid;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBuilderTowerMove : NetworkBehaviour
{
    private struct PlannedTowerMove
    {
        public Tower Tower;
        public TowerGhost Ghost;
        public Vector2Int CurrentCenter;
        public Vector2Int TargetCenter;
        public Vector3 TargetPosition;
        public List<Vector2Int> TargetIndices;
    }

    private List<TowerGhost> _ghosts;
    private Dictionary<TowerGhost, Tower> _ghostToTowerDic;
    private Dictionary<Tower, Vector3> _towerToVecDic;
    private readonly HashSet<Vector2Int> _previewIndices = new();

    private PlayerBuilderTowerSystem _towerSystem;

    #region API

    public void InitTowerMove(PlayerBuilderTowerSystem towerSystem)
    {
        _towerSystem = towerSystem;
    }

    public void TowerMoveSet(HashSet<Tower> towers)
    {
        TowerGhostInstantiate(towers);
        Vector3 pivot = GetPivot(towers);
        TowerDistanceVectorCalc(towers, pivot);
    }

    public bool TowerGhostSnapShot(Vector3 mousePos)
    {
        bool canMoveAll = true;
        _previewIndices.Clear();
        var gridManager = GridManager.Instance;
        var selectedOccupied = CollectSelectedOccupiedIndices();
        var claimedTargets = new Dictionary<Vector2Int, Tower>();
        var conflictedTowers = new HashSet<Tower>();
        var plannedMoves = new List<PlannedTowerMove>(_ghosts.Count);

        foreach (var ghost in _ghosts)
        {
            Tower targetTower = _ghostToTowerDic[ghost];
            Vector3 diff = _towerToVecDic[targetTower];
            Vector3 targetPos = mousePos + diff;

            Vector2Int snapshotIndex = gridManager.GetNearestCellIndex(targetPos);
            Vector3 snapshotPos = gridManager.GetCellCenterPositionFromIndex(snapshotIndex);
            Vector2Int currentIndex = gridManager.GetNearestCellIndex(targetTower.transform.position);
            List<Vector2Int> targetIndices = gridManager.GetCellIndicesInRange(snapshotIndex, targetTower.BuildRange, includeCenter: true);

            ghost.transform.position = snapshotPos;
            ghost.EnableTower();
            AddIndicesToSet(targetIndices, _previewIndices);

            plannedMoves.Add(new PlannedTowerMove
            {
                Tower = targetTower,
                Ghost = ghost,
                CurrentCenter = currentIndex,
                TargetCenter = snapshotIndex,
                TargetPosition = snapshotPos,
                TargetIndices = targetIndices
            });
        }

        // 이동 대상들끼리 도착 범위가 겹치면 불가.
        for (int i = 0; i < plannedMoves.Count; i++)
        {
            var move = plannedMoves[i];
            for (int j = 0; j < move.TargetIndices.Count; j++)
            {
                Vector2Int idx = move.TargetIndices[j];
                if (claimedTargets.TryGetValue(idx, out Tower owner))
                {
                    if (owner != move.Tower)
                    {
                        conflictedTowers.Add(owner);
                        conflictedTowers.Add(move.Tower);
                    }
                }
                else
                {
                    claimedTargets[idx] = move.Tower;
                }
            }
        }

        for (int i = 0; i < plannedMoves.Count; i++)
        {
            var move = plannedMoves[i];
            bool isSamePosition = move.TargetCenter == move.CurrentCenter;
            bool canPlace = gridManager.CanPlaceInRange(
                move.TargetCenter,
                move.Tower.BuildRange,
                requireEmpty: true,
                requireTerritory: true,
                ignoreOccupiedIndices: selectedOccupied);
            bool conflicted = conflictedTowers.Contains(move.Tower);
            bool canMoveThisTower = !isSamePosition && canPlace && !conflicted;

            if (canMoveThisTower)
            {
                move.Ghost.EnableTower();
            }
            else
            {
                move.Ghost.DisableTower();
                canMoveAll = false;
            }
        }

        gridManager.SetBuildRangePreview(_previewIndices);
        return canMoveAll;
    }

    public void TowerMove()
    {
        var gridManager = GridManager.Instance;
        if (gridManager == null) return;

        var selectedOccupied = CollectSelectedOccupiedIndices();
        var claimedTargets = new Dictionary<Vector2Int, Tower>();
        var conflictedTowers = new HashSet<Tower>();
        var plannedMoves = new List<PlannedTowerMove>(_ghosts.Count);

        foreach (var ghost in _ghosts)
        {
            Tower tower = _ghostToTowerDic[ghost];
            Vector2Int currentIndex = gridManager.GetNearestCellIndex(tower.transform.position);
            Vector2Int targetIndex = gridManager.GetNearestCellIndex(ghost.transform.position);
            List<Vector2Int> targetIndices = gridManager.GetCellIndicesInRange(targetIndex, tower.BuildRange, includeCenter: true);

            plannedMoves.Add(new PlannedTowerMove
            {
                Tower = tower,
                Ghost = ghost,
                CurrentCenter = currentIndex,
                TargetCenter = targetIndex,
                TargetPosition = ghost.transform.position,
                TargetIndices = targetIndices
            });
        }

        for (int i = 0; i < plannedMoves.Count; i++)
        {
            var move = plannedMoves[i];
            for (int j = 0; j < move.TargetIndices.Count; j++)
            {
                Vector2Int idx = move.TargetIndices[j];
                if (claimedTargets.TryGetValue(idx, out Tower owner))
                {
                    if (owner != move.Tower)
                    {
                        conflictedTowers.Add(owner);
                        conflictedTowers.Add(move.Tower);
                    }
                }
                else
                {
                    claimedTargets[idx] = move.Tower;
                }
            }
        }

        for (int i = 0; i < plannedMoves.Count; i++)
        {
            var move = plannedMoves[i];
            bool isSamePosition = move.TargetCenter == move.CurrentCenter;
            bool canPlace = gridManager.CanPlaceInRange(
                move.TargetCenter,
                move.Tower.BuildRange,
                requireEmpty: true,
                requireTerritory: true,
                ignoreOccupiedIndices: selectedOccupied);
            bool conflicted = conflictedTowers.Contains(move.Tower);
            if (isSamePosition || !canPlace || conflicted)
            {
                return;
            }
        }

        // 순차 적용 시 서로를 막지 않도록 먼저 전부 점유 해제한 뒤 새 위치 점유.
        for (int i = 0; i < plannedMoves.Count; i++)
        {
            plannedMoves[i].Tower.ReleaseGridOccupation();
        }

        for (int i = 0; i < plannedMoves.Count; i++)
        {
            var move = plannedMoves[i];
            bool occupied = move.Tower.TryOccupyAtIndex(move.TargetCenter, requireTerritory: true, requireEmpty: true);
            if (!occupied)
            {
                // 실패 시 기존 위치로 복구.
                for (int j = 0; j < plannedMoves.Count; j++)
                {
                    plannedMoves[j].Tower.ReleaseGridOccupation();
                }
                for (int j = 0; j < plannedMoves.Count; j++)
                {
                    var rollback = plannedMoves[j];
                    rollback.Tower.TryOccupyAtIndex(rollback.CurrentCenter, requireTerritory: true, requireEmpty: true);
                }
                return;
            }
        }

        int arrayCount = _ghostToTowerDic.Count;
        int currentCount = 0;
        NetworkId[] netId = new NetworkId[arrayCount];
        Vector3[] vec = new Vector3[arrayCount];

        for (int i = 0; i < plannedMoves.Count; i++)
        {
            var move = plannedMoves[i];
            Tower tower = move.Tower;

            netId[currentCount] = tower.Object.Id;
            vec[currentCount++] = move.TargetPosition;

            TowerMoveProcess(tower);
        }

        RPC_TowerMove(netId, vec);
    }

    public void TowerMoveClear()
    {
        GridManager.Instance.ClearBuildRangePreview();

        for (int i = 0; i < _ghosts.Count; i++)
        {
            Destroy(_ghosts[i].gameObject);
        }

        _ghosts.Clear();
        _ghostToTowerDic.Clear();
        _towerToVecDic.Clear();
    }

    #endregion

    #region Core

    private void TowerGhostInstantiate(HashSet<Tower> towers)
    {
        _ghosts = new List<TowerGhost>();
        _ghostToTowerDic = new Dictionary<TowerGhost, Tower>();

        foreach (var tower in towers)
        {
            var ghost = Instantiate(tower.Ghost);
            if (tower.HasBuffRange)
            {
                ghost.SetGhostBuffRange(tower.BuffRange);
            }

            _ghosts.Add(ghost);
            _ghostToTowerDic.Add(ghost, tower);
        }
    }

    private Vector3 GetPivot(HashSet<Tower> towers)
    {
        if (towers == null || towers.Count == 0)
            return Vector3.zero;

        bool inited = false;
        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;

        foreach (var t in towers)
        {
            if (!t) continue;

            Vector3 p = t.transform.position;
            if (!inited)
            {
                min = p;
                max = p;
                inited = true;
            }
            else
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
        }

        return inited ? (min + max) * 0.5f : Vector3.zero;
    }

    private void TowerDistanceVectorCalc(HashSet<Tower> towers, Vector3 pivot)
    {
        _towerToVecDic = new Dictionary<Tower, Vector3>();

        foreach (var tower in towers)
        {
            Vector3 p = tower.transform.position;
            Vector3 diff = p - pivot;
            _towerToVecDic.Add(tower, diff);
        }
    }

    private HashSet<Vector2Int> CollectSelectedOccupiedIndices()
    {
        var occupied = new HashSet<Vector2Int>();
        foreach (var pair in _ghostToTowerDic)
        {
            Tower tower = pair.Value;
            if (tower == null) continue;

            var indices = tower.OccupiedIndices;
            for (int i = 0; i < indices.Count; i++)
            {
                occupied.Add(indices[i]);
            }
        }

        return occupied;
    }

    private static void AddIndicesToSet(List<Vector2Int> source, HashSet<Vector2Int> target)
    {
        if (source == null || target == null) return;
        for (int i = 0; i < source.Count; i++)
        {
            target.Add(source[i]);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_TowerMove(NetworkId[] netId, Vector3[] pos)
    {
        for (int i = 0; i < netId.Length && i < pos.Length; i++)
        {
            if (!Runner.TryFindObject(netId[i], out NetworkObject obj))
                continue;

            obj.TryGetComponent(out NetworkTransform nt);
            nt.Teleport(pos[i]);
        }
    }

    private void TowerMoveProcess(Tower tower)
    {
        string towerId = tower.TowerID;
        if (towerId == TowerIDContainer.TELEPORT_TOWER_ID)
        {
            TeleportTowerMoveProcess(tower);
        }
    }

    private void TeleportTowerMoveProcess(Tower tower)
    {
        if (tower.TryGetComponent(out TeleportTower teleportTower))
        {
            teleportTower.SetCoolDown();
        }
    }

    #endregion
}
