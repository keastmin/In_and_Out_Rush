using Fusion;
using ProjectIO.GridPlacement;
using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class PlayerBuilderTowerBuild : NetworkBehaviour
    {
        [SerializeField] private Tower _tower;
        [SerializeField] private TowerGhost _towerGhost;
        [SerializeField] private Cost _buildCost;
        [SerializeField] private NetworkPrefabRef _towerRef;
        [SerializeField] private bool _isStandByBuild = false;

        private TowerData _towerData;
        private TowerBuildManager _towerBuildManager;
        private PlayerBuilder _builder;
        private bool _isBuildRequestPending;
        private int[] _pendingSupplyArray = System.Array.Empty<int>();

        public TowerGhost TowerGhost => _towerGhost;
        public Cost BuildCost => _buildCost;
        public bool IsStandByBuild => _isStandByBuild;
        public bool IsCenterTower => _tower != null && _tower.IsCenter;
        public string TowerID => (_tower != null) ? _tower.TowerID : string.Empty;
        public int BuildRange => (_tower != null) ? _tower.BuildRange : 0;

        private PlayerBuilderTowerSystem _towerSystem;

        public void Init(PlayerBuilderUI builderUI, PlayerBuilderTowerSystem towerSystem)
        {
            _isStandByBuild = false;
            _towerSystem = towerSystem;
            TryGetComponent(out _builder);
            LinkBuildTowerAction(builderUI);
        }

        public void InitializeTowerBuildManager(TowerBuildManager towerBuildManager)
        {
            if (_towerBuildManager != null)
                _towerBuildManager.OnTowerBuildRequestCompleted -= HandleTowerBuildRequestCompleted;

            _towerBuildManager = towerBuildManager;

            if (_towerBuildManager != null)
                _towerBuildManager.OnTowerBuildRequestCompleted += HandleTowerBuildRequestCompleted;
        }

        private void OnDestroy()
        {
            if (_towerBuildManager != null)
                _towerBuildManager.OnTowerBuildRequestCompleted -= HandleTowerBuildRequestCompleted;
        }

        public bool TowerBuildConditionChecker(string towerId)
        {
            if (towerId == TowerIDContainer.TELEPORT_TOWER_ID)
            {
                if (TowerManager.Instance == null)
                    return false;

                return TowerManager.Instance.GetTowerCount(towerId) < TeleportTowerPairManager.MaxTeleportTowerCount;
            }

            if (towerId == TowerIDContainer.SUPPLY_TOWER_ID)
            {
                return SupplyTowerManager.Instance != null &&
                       SupplyTowerManager.Instance.HasPendingSupplies;
            }

            return true;
        }

        public bool CanBuildAt(Vector2Int index)
        {
            if (_towerBuildManager != null)
                return _towerBuildManager.CanBuildAt(_towerData, _builder, index);

            return false;
        }

        public void EvaluateBuildFootprint(Vector2Int centerIndex, HashSet<Vector2Int> validIndices, HashSet<Vector2Int> blockedIndices)
        {
            validIndices?.Clear();
            blockedIndices?.Clear();

            InfiniteGrid grid = InfiniteGrid.Instance;
            if (_tower == null || grid == null)
                return;

            var indices = grid.GetCellIndicesInRange(centerIndex, BuildRange, includeCenter: true);
            for (int i = 0; i < indices.Count; i++)
            {
                Vector2Int targetIndex = indices[i];
                var cellState = new TowerPlacementCellState(
                    grid.IsCellInBuildArea(targetIndex),
                    grid.IsCellBlockedByTrack(targetIndex),
                    grid.IsCellOccupied(targetIndex));
                bool canPlaceCell = TowerPlacementPolicy.CanPlace(cellState);

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

        public void UpdateBuffCellPreview(TowerGhost towerGhost, Vector2Int centerIndex)
        {
            if (towerGhost == null || InfiniteGrid.Instance == null)
                return;

            if (_tower is CellBuffSupportTower buffTower)
            {
                InfiniteGrid.Instance.RegisterOrUpdateBuffPreviewSource(
                    towerGhost.GetInstanceID(),
                    centerIndex,
                    buffTower.BuffCellRange,
                    buffTower.BuffCellColor);
            }
        }

        public void ClearBuffCellPreview()
        {
            InfiniteGrid.Instance?.ClearBuffPreviewSources();
        }

        public void BuildTower(Vector2Int index)
        {
            if (_isBuildRequestPending || _towerBuildManager == null || _towerData == null || _builder == null)
                return;

            bool isSupplyTower = TowerID == TowerIDContainer.SUPPLY_TOWER_ID;
            int[] supplyArray = isSupplyTower && SupplyTowerManager.Instance != null
                ? SupplyTowerManager.Instance.PeekSupplyNumArray()
                : System.Array.Empty<int>();

            if (isSupplyTower && supplyArray.Length == 0)
                return;

            _pendingSupplyArray = supplyArray;
            _isBuildRequestPending = true;
            if (!_towerBuildManager.TryRequestTowerBuild(
                _towerData,
                _builder,
                index,
                supplyArray))
            {
                _isBuildRequestPending = false;
                _pendingSupplyArray = System.Array.Empty<int>();
            }
        }

        public void RevertStandBy()
        {
            _tower = null;
            _towerGhost = null;
            _towerRef = default;
            _towerData = null;
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

            _towerData = data;
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

            if (_tower.IsCenter && (_builder == null || _builder.CenterTowerCount >= _builder.MaxCenterTowerCount))
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

        private void HandleTowerBuildRequestCompleted(bool success, bool isSupplyTower)
        {
            _isBuildRequestPending = false;

            if (!success || !isSupplyTower || SupplyTowerManager.Instance == null)
            {
                _pendingSupplyArray = System.Array.Empty<int>();
                return;
            }

            SupplyTowerManager.Instance.TryConsumePendingSupplies(_pendingSupplyArray);
            _pendingSupplyArray = System.Array.Empty<int>();
        }
    }
}
