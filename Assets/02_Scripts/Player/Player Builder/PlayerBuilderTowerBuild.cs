using Fusion;
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

    private PlayerBuilderTowerSystem _towerSystem;

    #region API

    /// <summary>
    /// 외부에서 호출해야하는 초기화 함수
    /// </summary>
    /// <param name="builderUI">빌더의 UI 컴포넌트</param>
    public void Init(PlayerBuilderUI builderUI, PlayerBuilderTowerSystem towerSystem)
    {
        _isStandByBuild = false;
        _towerSystem = towerSystem;
        LinkBuildTowerAction(builderUI);
    }

    /// <summary>
    /// 해당 타워를 건설 가능한지 확인하는 함수
    /// </summary>
    /// <param name="towerId">설치할 타워의 ID</param>
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
    /// 건설 준비중인 타워를 설치하는 함수
    /// </summary>
    /// <param name="grid">타워를 설치할 때 기반이 되는 육각 그리드</param>
    /// <param name="index">타워를 설치할 그리드의 인덱스</param>
    public void BuildTower(HexagonGrid grid, Vector2Int index)
    {
        if(_towerRef != default && _tower != null)
        {
            Vector3 pos = grid.GetNearCellPositionFromIndex(index);
            grid.ChangeCellState(index, CellState.Tower); // 타워 설치 인덱스에 타워 상태로 변경
            RPC_BuildTower(_towerRef, _buildCost, pos);
        }
    }

    /// <summary>
    /// 타워 건설 준비중을 원래대로 되돌리는 함수
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
    /// 빌더 UI의 타워 건설 버튼을 누르면 작동할 액션 연결
    /// </summary>
    /// <param name="builderUI">빌더의 UI 컴포넌트</param>
    private void LinkBuildTowerAction(PlayerBuilderUI builderUI)
    {
        if(builderUI != null)
        {
            builderUI.OnClickTowerBuildButtonAction += InjectionTowerData;
        }
    }

    // 설치할 타워 데이터 주입
    private void InjectionTowerData(TowerData data)
    {
        if(data != null)
        {
            _tower = data.Tower;
            _towerGhost = data.TowerGhost;
            _towerRef = data.TowerPrefabRef;

            if(_tower != null)
            {
                _buildCost = _tower.Cost;
            }

            // 타워 설치 준비
            if (_tower != null && _towerGhost != null && _towerRef != null)
                _isStandByBuild = true;
        }
    }

    // Host에게 자원 차감과 타워 스폰 요청
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_BuildTower(NetworkPrefabRef towerRef, Cost cost, Vector3 position)
    {
        if (HasStateAuthority)
        {
            ResourceSystem.Instance.Mineral -= cost.Mineral; // 미네랄 차감
            ResourceSystem.Instance.Gas -= cost.Gas; // 가스 차감
            NetworkObject tower = Runner.Spawn(towerRef, position, Quaternion.identity); // 타워 스폰
            TeleportTowerSetting(tower); // 텔레포트 타워라면 빌더에게 텔레포트 관련 세팅을 요청
        }
    }

    // 텔레포트 타워일시 설치 후 텔레포트 타워 관련 설정 세팅
    private void TeleportTowerSetting(NetworkObject no)
    {
        if (no.TryGetComponent(out Tower tower))
        {
            if (tower.TowerID == TowerIDContainer.TELEPORT_TOWER_ID)
            {
                RPC_TeleportTowerSetting(no);
            }
        }
    }

    // 빌더에게 텔레포트 타워를 저장할 것을 요청
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_TeleportTowerSetting(NetworkObject no)
    {
        if (HasInputAuthority)
        {
            if (no.TryGetComponent(out TeleportTower tower))
                _towerSystem.AddTeleportTowerArray(tower);
        }
    }

    // 텔레포트 타워 조건을 확인하는 함수
    private bool TeleportTowerBuildConditionChecker()
    {
        if (_towerSystem.TeleportTowerCount == 2)
            return false;
        return true;
    }

    #endregion
}
