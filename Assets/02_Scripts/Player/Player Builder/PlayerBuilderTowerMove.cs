using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBuilderTowerMove : NetworkBehaviour
{
    private List<TowerGhost> _ghosts;
    private Dictionary<TowerGhost, Tower> _ghostToTowerDic; // 타워 고스트로 타워 찾기
    private Dictionary<Tower, Vector3> _towerToVecDic; // 타워로 위치 찾기

    private PlayerBuilderTowerSystem _towerSystem; // 타워 시스템 참조

    #region API

    /// <summary>
    /// 타워 이동 컴포넌트 초기화 함수
    /// </summary>
    /// <param name="towerSystem">타워 시스템 참조</param>
    public void InitTowerMove(PlayerBuilderTowerSystem towerSystem)
    {
        _towerSystem = towerSystem;
    }

    public void TowerMoveSet(HashSet<Tower> towers)
    {
        // 각 타워의 타워 고스트 생성
        TowerGhostInstantiate(towers);

        // 타워 그룹의 중심 찾기
        Vector3 pivot = GetPivot(towers);

        // 각 타워가 피벗에서 떨어져있는 벡터 계산
        TowerDistanceVectorCalc(towers, pivot);
    }

    public bool TowerGhostSnapShot(HexagonGrid grid, Vector3 mousePos)
    {
        bool canMove = true;

        foreach(var ghost in _ghosts)
        {
            // 타겟 위치 계산
            Tower targetTower = _ghostToTowerDic[ghost];
            Vector3 diff = _towerToVecDic[targetTower];
            Vector3 targetPos = mousePos + diff;

            // 그리드에 스냅샷할 위치 계산
            Vector2Int snapshotIndex = grid.GetNearIndex(targetPos);
            Vector3 snapshotPos = grid.GetNearCellPositionFromIndex(snapshotIndex);
            ghost.transform.position = snapshotPos;
            ghost.EnableTower();

            // 스탭샷 위치에 설치 가능한지 검사
            bool canMoveThisTower = CanMoveThisPosition(grid, snapshotIndex);
            if (!canMoveThisTower)
            {
                canMove = false;
                ghost.DisableTower();
            }
        }

        return canMove;
    }

    /// <summary>
    /// 타워의 이동 확정
    /// </summary>
    /// <param name="grid">그리드 컴포넌트의 참조</param>
    public void TowerMove(HexagonGrid grid)
    {
        int arrayCount = _ghostToTowerDic.Count;
        int currentCount = 0;
        NetworkId[] netId = new NetworkId[arrayCount];
        Vector3[] vec = new Vector3[arrayCount];

        foreach(var g in _ghosts)
        {
            Tower tower = _ghostToTowerDic[g];

            Vector2Int index = grid.GetNearIndex(tower.transform.position);
            grid.ChangeCellState(index, CellState.None);

            Vector2Int newIndex = grid.GetNearIndex(g.transform.position);
            grid.ChangeCellState(newIndex, CellState.Tower);

            netId[currentCount] = tower.Object.Id;
            vec[currentCount++] = g.transform.position;

            TowerMoveProcess(tower);
        }

        // 타워 위치 이동 RPC 호출
        RPC_TowerMove(netId, vec);
    }

    public void TowerMoveClear()
    {
        for(int i = 0; i < _ghosts.Count; i++)
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
        foreach(var tower in towers)
        {
            var ghost = Instantiate(tower.Ghost);

            // 버프가 있다면 고스트에 반영
            if (tower.HasBuffRange)
            {
                ghost.SetGhostBuffRange(tower.BuffRange);
            }

            _ghosts.Add(ghost);
            _ghostToTowerDic.Add(ghost, tower);
        }
    }

    // 타워 집합의 중심 피벗 찾기
    private Vector3 GetPivot(HashSet<Tower> towers)
    {
        if (towers == null || towers.Count == 0)
            return Vector3.zero;

        bool inited = false;
        Vector3 min = Vector3.zero, max = Vector3.zero;

        foreach(var t in towers)
        {
            if (!t) continue;

            Vector3 p = t.transform.position;

            if (!inited)
            {
                min = max = p;
                inited = true;
            }
            else
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
        }

        if (!inited) return Vector3.zero;
        return (min + max) * 0.5f;
    }

    private void TowerDistanceVectorCalc(HashSet<Tower> towers, Vector3 pivot)
    {
        _towerToVecDic = new Dictionary<Tower, Vector3>();

        foreach(var tower in towers)
        {
            Vector3 p = tower.transform.position;
            Vector3 diff = p - pivot;
            _towerToVecDic.Add(tower, diff);
        }
    }

    private bool CanMoveThisPosition(HexagonGrid grid, Vector2Int index)
    {
        if (grid.IsEmptyCell(index) && grid.IsPointInTerritory(index))
        {
            return true;
        }
        return false;
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

    /// <summary>
    /// 각 타워가 움직임 이후 처리되어야 하는 절차를 수행하는 함수
    /// </summary>
    /// <param name="tower">절차를 처리할 타워</param>
    private void TowerMoveProcess(Tower tower)
    {
        string towerId = tower.TowerID;

        // 텔레포트 타워의 움직임 이후 절차
        if(towerId == TowerIDContainer.TELEPORT_TOWER_ID)
        {
            TeleportTowerMoveProcess(tower);
        }
    }

    // 텔레포트 타워가 수행할 절차
    private void TeleportTowerMoveProcess(Tower tower)
    {
        if(tower.TryGetComponent(out TeleportTower teleportTower))
        {
            teleportTower.SetCoolDown();
        }
    }

    #endregion
}
