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

    public void Init(PlayerBuilderUI builderUI, PlayerBuilderTowerSystem towerSystem)
    {
        _isStandByBuild = false;
        _towerSystem = towerSystem;
        LinkBuildTowerAction(builderUI);
    }

    public bool TowerBuildConditionChecker(string towerId)
    {
        bool canBuild = true;

        if (towerId == TowerIDContainer.TELEPORT_TOWER_ID)
        {
            canBuild = TeleportTowerBuildConditionChecker();
        }
        else if (towerId == TowerIDContainer.SUPPLY_TOWER_ID)
        {
            canBuild = SupplyTowerBuildConditionChecker();
        }

        return canBuild;
    }

    public void BuildTower(Vector2Int index)
    {
        if (_towerRef != default && _tower != null)
        {
            Vector3 pos = GridManager.Instance.GetCellCenterPositionFromIndex(index);
            RPC_BuildTower(_towerRef, _buildCost, pos);
        }
    }

    public void RevertStandBy()
    {
        _tower = null;
        _towerGhost = null;
        _towerRef = default;
        _isStandByBuild = false;
    }

    private void LinkBuildTowerAction(PlayerBuilderUI builderUI)
    {
        if (builderUI != null)
        {
            builderUI.OnClickTowerBuildButtonAction += InjectionTowerData;
        }
    }

    private void InjectionTowerData(TowerData data)
    {
        if (data == null)
            return;

        _tower = data.Tower;
        _towerGhost = data.TowerGhost;
        _towerRef = data.TowerPrefabRef;

        if (_tower != null)
        {
            _buildCost = _tower.Cost;
        }

        _isStandByBuild = false;

        if (_tower == null || _towerGhost == null || _towerRef == default)
            return;

        if (!TowerBuildConditionChecker(_tower.TowerID))
            return;

        _isStandByBuild = true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_BuildTower(NetworkPrefabRef towerRef, Cost cost, Vector3 position)
    {
        if (HasStateAuthority)
        {
            ResourceSystem.Instance.Mineral -= cost.Mineral;
            ResourceSystem.Instance.Gas -= cost.Gas;
            Runner.Spawn(towerRef, position, Quaternion.identity);
        }
    }

    private bool TeleportTowerBuildConditionChecker()
    {
        if (TowerManager.Instance.GetTowerCount(TowerIDContainer.TELEPORT_TOWER_ID) >= 2)
            return false;

        return true;
    }

    private bool SupplyTowerBuildConditionChecker()
    {
        if (SupplyTowerManager.Instance == null)
            return false;

        return SupplyTowerManager.Instance.HasPendingSupplies;
    }
}
