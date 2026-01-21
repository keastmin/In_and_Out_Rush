using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBuilderTowerSell : NetworkBehaviour
{
    private PlayerBuilderTowerSystem _towerSystem;

    #region API

    /// <summary>
    /// 타워 판매 컴포넌트 초기화 함수
    /// </summary>
    /// <param name="towerSystem">타워 관리 시스템 컴포넌트 참조</param>
    public void InitTowerSell(PlayerBuilderTowerSystem towerSystem)
    {
        _towerSystem = towerSystem;
    }

    /// <summary>
    /// 타워를 판매하는 함수
    /// </summary>
    /// <param name="grid">육각 그리드</param>
    /// <param name="towers">판매할 타워들</param>
    public void SellTower(HexagonGrid grid, HashSet<Tower> towers, PlayerBuilder builder)
    {
        if (towers == null || towers.Count == 0) return;

        NetworkId[] ids = new NetworkId[towers.Count];
        Cost[] costs = new Cost[towers.Count];
        int n = 0;

        foreach (var t in towers)
        {
            if (t == null) continue;

            CellStateChange(grid, t); // 셀 상태 변경
            NetworkObject no = t.Object;
            if (no == null) continue;

            costs[n] = t.Cost;
            ids[n++] = no.Id;

            // 센터 타워일 경우 카운트 재설정 후 잔여 속성 재설정
            if (t.IsCenter)
            {
                if (t.TryGetComponent(out CenterTower centerTower))
                {
                    builder.SetCenterTowerCount(builder.CenterTowerCount - 1);
                }
            }

            // 각 타워가 판매될 때 수행되어야 하는 절차 수행
            TowerSellProcess(t);
        }

        if (n == 0) return;
        if (n != ids.Length) Array.Resize(ref ids, n);

        towers.Clear();
        RPC_SellTower(ids, costs);      
    }

    #endregion

    #region Core

    // 타워가 있던 셀의 상태를 None으로 변경
    private void CellStateChange(HexagonGrid grid, Tower tower)
    {
        Vector2Int index = grid.GetNearIndex(tower.transform.position); // 타워 근처 인덱스 찾기
        grid.ChangeCellState(index, CellState.None); // 셀 상태 변경
    }

    // 타워를 Despawn하는 것을 요청하고 금액 환불
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SellTower(NetworkId[] towerIds, Cost[] costs)
    {
        // Despawn
        foreach(var id in towerIds)
        {
            if (!Runner.TryFindObject(id, out NetworkObject obj))
                continue;

            Runner.Despawn(obj);
        }

        // 환불
        foreach(var cost in costs)
        {
            ResourceSystem.Instance.Mineral += (int)(cost.Mineral * 0.5f);
            ResourceSystem.Instance.Gas += (int)(cost.Gas * 0.5f);
        }
    }

    // 각 타워가 판매될 때 수행하는 함수
    private void TowerSellProcess(Tower tower)
    {
        string id = tower.TowerID;
        if (id == TowerIDContainer.TELEPORT_TOWER_ID)
            TeleportTowerSellProcess(tower);
    }

    // 텔레포트 타워가 판매될 때 수행되어야 하는 절차
    private void TeleportTowerSellProcess(Tower tower)
    {
        if (tower.TryGetComponent(out TeleportTower teleportTower))
        {
            _towerSystem.RemoveTeleportTowerArray(teleportTower);
        }
    }

    #endregion
}
