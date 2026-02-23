using Fusion;
using Grid;
using UnityEngine;

public sealed class PlayerBuilderTowerBuild : NetworkBehaviour
{
    [SerializeField] private Tower _tower;
    [SerializeField] private TowerGhost _towerGhost;
    [SerializeField] private Cost _buildCost;
    [SerializeField] private NetworkPrefabRef _towerRef;
    [SerializeField] private bool _isStandByBuild = false;

    public TowerGhost TowerGhost => _towerGhost;
    public Cost BuildCost => _buildCost;
    public bool IsStandByBuild => _isStandByBuild;
    public bool IsCenterTower => _tower.IsCenter;
    public bool HasBuffRange => _tower.HasBuffRange;
    public float BuffRange => _tower.BuffRange;
    public string TowerID => _tower.TowerID;
    public int BuildRange => (_tower != null) ? _tower.BuildRange : 0;

    private PlayerBuilderTowerSystem _towerSystem;

    #region API

    /// <summary>
    /// 외부에서 호출하는 초기화 함수
    /// </summary>
    /// <param name="builderUI">빌더 UI 컴포넌트</param>
    public void Init(PlayerBuilderUI builderUI, PlayerBuilderTowerSystem towerSystem)
    {
        _isStandByBuild = false;
        _towerSystem = towerSystem;
        LinkBuildTowerAction(builderUI);
    }

    /// <summary>
    /// 해당 타워가 설치 가능한지 검사
    /// </summary>
    /// <param name="towerId">설치할 타워 ID</param>
    /// <returns>설치 가능 여부</returns>
    public bool TowerBuildConditionChecker(string towerId)
    {
        bool canBuild = true;
        if (towerId == TowerIDContainer.TELEPORT_TOWER_ID)
        {
            canBuild = TeleportTowerBuildConditionChecker();
        }
        return canBuild;
    }

    /// <summary>
    /// 선택된 셀 인덱스에 타워 설치 요청
    /// </summary>
    public void BuildTower(Vector2Int index)
    {
        if (_towerRef != default && _tower != null)
        {
            Vector3 pos = GridManager.Instance.GetCellCenterPositionFromIndex(index);
            RPC_BuildTower(_towerRef, _buildCost, pos);
        }
    }

    /// <summary>
    /// 설치 대기 상태 해제
    /// </summary>
    public void RevertStandBy()
    {
        _tower = null;
        _towerGhost = null;
        _towerRef = default;
        _isStandByBuild = false;
    }

    #endregion

    #region Core

    /// <summary>
    /// 빌더 UI의 타워 건설 버튼 액션 연결
    /// </summary>
    private void LinkBuildTowerAction(PlayerBuilderUI builderUI)
    {
        if (builderUI != null)
        {
            builderUI.OnClickTowerBuildButtonAction += InjectionTowerData;
        }
    }

    // 설치할 타워 데이터 주입
    private void InjectionTowerData(TowerData data)
    {
        if (data != null)
        {
            _tower = data.Tower;
            _towerGhost = data.TowerGhost;
            _towerRef = data.TowerPrefabRef;

            if (_tower != null)
            {
                _buildCost = _tower.Cost;
            }

            // 설치 대기 상태 진입
            if (_tower != null && _towerGhost != null && _towerRef != null)
                _isStandByBuild = true;
        }
    }

    // Host에게 자원 차감 + 타워 스폰 요청
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_BuildTower(NetworkPrefabRef towerRef, Cost cost, Vector3 position)
    {
        if (HasStateAuthority)
        {
            ResourceSystem.Instance.Mineral -= cost.Mineral; // 미네랄 차감
            ResourceSystem.Instance.Gas -= cost.Gas; // 가스 차감
            Runner.Spawn(towerRef, position, Quaternion.identity); // 타워 스폰
        }
    }

    // 텔레포트 타워 설치 조건 검사
    private bool TeleportTowerBuildConditionChecker()
    {
        if (TowerManager.Instance.GetTowerCount(TowerIDContainer.TELEPORT_TOWER_ID) >= 2)
            return false;
        return true;
    }

    #endregion
}
