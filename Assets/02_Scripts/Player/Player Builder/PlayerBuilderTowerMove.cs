using Fusion;
using Grid;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBuilderTowerMove : NetworkBehaviour
{
    private List<TowerGhost> _ghosts;
    private Dictionary<TowerGhost, Tower> _ghostToTowerDic;
    private Dictionary<Tower, Vector3> _towerToVecDic;

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
        bool canMove = true;

        foreach (var ghost in _ghosts)
        {
            Tower targetTower = _ghostToTowerDic[ghost];
            Vector3 diff = _towerToVecDic[targetTower];
            Vector3 targetPos = mousePos + diff;

            Vector2Int snapshotIndex = GridManager.Instance.GetNearestCellIndex(targetPos);
            Vector3 snapshotPos = GridManager.Instance.GetCellCenterPositionFromIndex(snapshotIndex);
            ghost.transform.position = snapshotPos;
            ghost.EnableTower();

            bool canMoveThisTower = CanMoveThisPosition(targetTower, snapshotPos);
            if (!canMoveThisTower)
            {
                canMove = false;
                ghost.DisableTower();
            }
        }

        return canMove;
    }

    public void TowerMove()
    {
        int arrayCount = _ghostToTowerDic.Count;
        int currentCount = 0;
        NetworkId[] netId = new NetworkId[arrayCount];
        Vector3[] vec = new Vector3[arrayCount];

        foreach (var g in _ghosts)
        {
            Tower tower = _ghostToTowerDic[g];
            tower.TryMoveOccupancyToWorldPosition(g.transform.position, requireTerritory: true);

            netId[currentCount] = tower.Object.Id;
            vec[currentCount++] = g.transform.position;

            TowerMoveProcess(tower);
        }

        RPC_TowerMove(netId, vec);
    }

    public void TowerMoveClear()
    {
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

    private bool CanMoveThisPosition(Tower tower, Vector3 targetPosition)
    {
        if (tower == null) return false;
        return tower.CanPlaceAtWorldPosition(targetPosition, requireTerritory: true, requireEmpty: true);
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
