using Fusion;
using Dev.Network;
using System.Collections.Generic;
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
    public bool IsCenterTower => _tower != null && _tower.IsCenter;
    public bool HasBuffRange => _tower != null && _tower.HasBuffRange;
    public float BuffRange => (_tower != null) ? _tower.BuffRange : 0f;
    public string TowerID => (_tower != null) ? _tower.TowerID : string.Empty;
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
        return true;
    }

    public bool HasSufficientResources()
    {
        if (StageBootstrapper.Instance == null || StageBootstrapper.Instance.ResourceSystem == null)
            return false;

        return StageBootstrapper.Instance.ResourceSystem.Mineral >= _buildCost.Mineral &&
               StageBootstrapper.Instance.ResourceSystem.Gas >= _buildCost.Gas;
    }

    public bool CanBuildAt(Vector2Int index)
    {
        if (_tower == null || _towerRef == default)
            return false;

        if (!HasSufficientResources())
            return false;

        if (!TowerBuildConditionChecker(TowerID))
            return false;

        if (InfiniteGrid.Instance == null)
            return false;

        return InfiniteGrid.Instance.CanPlaceAt(index, BuildRange);
    }

    public void EvaluateBuildFootprint(Vector2Int centerIndex, HashSet<Vector2Int> validIndices, HashSet<Vector2Int> blockedIndices)
    {
        validIndices?.Clear();
        blockedIndices?.Clear();

        if (_tower == null || InfiniteGrid.Instance == null)
            return;

        var indices = InfiniteGrid.Instance.GetCellIndicesInRange(centerIndex, BuildRange, includeCenter: true);
        for (int i = 0; i < indices.Count; i++)
        {
            Vector2Int targetIndex = indices[i];
            bool canPlaceCell = !InfiniteGrid.Instance.IsCellBuildBlocked(targetIndex);

            if (canPlaceCell)
            {
                validIndices?.Add(targetIndex);
            }
            else
            {
                blockedIndices?.Add(targetIndex);
            }
        }
    }

    public void BuildTower(Vector2Int index)
    {
        if (_towerRef != default && _tower != null)
        {
            RPC_BuildTower(_towerRef, _buildCost, index, BuildRange);
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

    public TowerGhost CreateTowerGhostInstance()
    {
        if (_towerGhost == null)
            return null;

        TowerGhost towerGhost = UnityEngine.Object.Instantiate(_towerGhost);
        towerGhost.InitializePreview();
        return towerGhost;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_BuildTower(NetworkPrefabRef towerRef, Cost cost, Vector2Int index, int buildRange)
    {
        if (!HasStateAuthority)
            return;

        if (InfiniteGrid.Instance == null)
            return;

        if (ResourceSystem.Instance.Mineral < cost.Mineral || ResourceSystem.Instance.Gas < cost.Gas)
            return;

        if (!InfiniteGrid.Instance.CanPlaceAt(index, buildRange))
            return;

        Vector3 position = InfiniteGrid.Instance.GetCellCenterPositionFromCellIndex(index);
        NetworkObject towerObject = Runner.Spawn(towerRef, position, Quaternion.identity);
        if (towerObject == null)
            return;

        if (towerObject.TryGetComponent(out GridPlaceable placeable) && !placeable.HasGridOccupation)
        {
            Runner.Despawn(towerObject);
            return;
        }

        ResourceSystem.Instance.Mineral -= cost.Mineral;
        ResourceSystem.Instance.Gas -= cost.Gas;
    }
}
