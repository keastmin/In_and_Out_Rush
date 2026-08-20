using System;
using Dev.Network;
using Fusion;
using ProjectIO.Construction;
using ProjectIO.ResourceEconomy.Adapters.Fusion;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class TowerBuildManager : NetworkBehaviour
    {
        public static TowerBuildManager Instance { get; private set; }

        public event Action<bool, bool> OnTowerBuildRequestCompleted;

        private static readonly TowerConstructionUseCase ConstructionUseCase = new();

        private TowerUpgradeManager _towerUpgradeManager;
        private ResourcePaymentFusionAdapter _resourcePayment;

        public override void Spawned()
        {
            Instance = this;
            ResolveTowerUpgradeManager();
            InjectTowerDependenciesToSpawnedTowers();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this)
                Instance = null;
        }

        public void Initialize(
            TowerUpgradeManager towerUpgradeManager,
            ResourcePaymentFusionAdapter resourcePayment)
        {
            _towerUpgradeManager = towerUpgradeManager != null
                ? towerUpgradeManager
                : GetComponent<TowerUpgradeManager>();
            _resourcePayment = resourcePayment;

            InjectTowerDependenciesToSpawnedTowers();
        }

        public bool CanBuildAt(TowerData towerData, PlayerBuilder builder, Vector2Int index)
        {
            if (towerData == null || towerData.Tower == null || towerData.TowerPrefabRef == default)
                return false;

            Tower tower = towerData.Tower;
            if (!CanBuildSpecialTower(tower.TowerID, null))
                return false;

            int centerTowerCount = HasStateAuthority ? GetCenterTowerCount() : builder?.CenterTowerCount ?? 0;
            if (tower.IsCenter && (builder == null || centerTowerCount >= builder.MaxCenterTowerCount))
                return false;

            if (_resourcePayment == null || !_resourcePayment.CanAfford(tower.Cost))
            {
                return false;
            }

            return InfiniteGrid.Instance != null &&
                   InfiniteGrid.Instance.CanPlaceAt(index, tower.BuildRange);
        }

        public bool TryRequestTowerBuild(
            TowerData towerData,
            PlayerBuilder builder,
            Vector2Int index,
            int[] supplyArray)
        {
            if (towerData == null || towerData.Tower == null || towerData.TowerPrefabRef == default ||
                builder == null || builder.Object == null)
            {
                return false;
            }

            RPC_RequestTowerBuild(
                towerData.TowerPrefabRef,
                towerData.Tower.TowerID,
                builder.Object.Id,
                index,
                supplyArray ?? Array.Empty<int>());
            return true;
        }

        public void InjectTowerDependencies(Tower tower)
        {
            ResolveTowerUpgradeManager();

            if (tower != null && _towerUpgradeManager != null)
                tower.InitializeTowerUpgradeManager(_towerUpgradeManager);
        }

        private void ResolveTowerUpgradeManager()
        {
            if (_towerUpgradeManager == null)
                TryGetComponent(out _towerUpgradeManager);
        }

        private void InjectTowerDependenciesToSpawnedTowers()
        {
            ResolveTowerUpgradeManager();
            if (_towerUpgradeManager == null)
                return;

            Tower[] spawnedTowers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
            foreach (Tower tower in spawnedTowers)
                InjectTowerDependencies(tower);
        }

        private bool TryBuildTower(
            NetworkPrefabRef towerRef,
            string requestedTowerId,
            NetworkId builderId,
            Vector2Int index,
            int[] supplyArray,
            PlayerRef requester)
        {
            if (!HasStateAuthority ||
                Runner == null ||
                InfiniteGrid.Instance == null ||
                _resourcePayment == null)
            {
                return false;
            }

            if (!Runner.TryFindObject(builderId, out NetworkObject builderObject) ||
                !builderObject.TryGetComponent(out PlayerBuilder builder))
            {
                return false;
            }

            if (requester != PlayerRef.None && builderObject.InputAuthority != requester)
                return false;

            if (!CanBuildSpecialTower(requestedTowerId, supplyArray))
                return false;

            var operation = new HostTowerConstructionOperation(
                this,
                towerRef,
                requestedTowerId,
                builder,
                index,
                supplyArray);
            return ConstructionUseCase.Execute(operation) == TowerConstructionResult.Success;
        }

        private static bool CanBuildSpecialTower(string towerId, int[] supplyArray)
        {
            if (towerId == TowerIDContainer.TELEPORT_TOWER_ID)
            {
                return TowerManager.Instance != null &&
                       TowerManager.Instance.GetTowerCount(towerId) < TeleportTowerPairManager.MaxTeleportTowerCount;
            }

            if (towerId == TowerIDContainer.SUPPLY_TOWER_ID)
            {
                return supplyArray == null
                    ? SupplyTowerManager.Instance != null && SupplyTowerManager.Instance.HasPendingSupplies
                    : supplyArray.Length > 0;
            }

            return true;
        }

        private static bool TryInitializeSpecialTower(Tower tower, int[] supplyArray)
        {
            if (tower.TowerID != TowerIDContainer.SUPPLY_TOWER_ID)
                return true;

            return tower.TryGetComponent(out SupplyTower supplyTower) &&
                   supplyTower.TryLoadSupplies(supplyArray);
        }

        private static bool CanRegisterCenterTower(Tower tower, PlayerBuilder builder)
        {
            return !tower.IsCenter || GetCenterTowerCount() <= builder.MaxCenterTowerCount;
        }

        private static void RegisterCenterTower(Tower tower, PlayerBuilder builder)
        {
            if (tower.IsCenter)
                builder.SetCenterTowerCount(GetCenterTowerCount());
        }

        private static int GetCenterTowerCount()
        {
            if (InfiniteGrid.Instance == null || InfiniteGrid.Instance.HostOnlyReadTowers == null)
                return 0;

            int count = 0;
            foreach (Tower tower in InfiniteGrid.Instance.HostOnlyReadTowers)
            {
                if (tower != null && tower.IsCenter)
                    count++;
            }

            return count;
        }

        private void DespawnFailedTower(NetworkObject towerObject)
        {
            if (towerObject == null)
                return;

            if (towerObject.TryGetComponent(out Tower tower))
                tower.ReleaseGridOccupation();

            Runner.Despawn(towerObject);
        }

        private sealed class HostTowerConstructionOperation : ITowerConstructionOperation
        {
            private readonly TowerBuildManager _manager;
            private readonly NetworkPrefabRef _towerRef;
            private readonly string _requestedTowerId;
            private readonly PlayerBuilder _builder;
            private readonly Vector2Int _index;
            private readonly int[] _supplyArray;

            private NetworkObject _towerObject;
            private Tower _tower;
            private bool _rolledBack;

            public HostTowerConstructionOperation(
                TowerBuildManager manager,
                NetworkPrefabRef towerRef,
                string requestedTowerId,
                PlayerBuilder builder,
                Vector2Int index,
                int[] supplyArray)
            {
                _manager = manager;
                _towerRef = towerRef;
                _requestedTowerId = requestedTowerId;
                _builder = builder;
                _index = index;
                _supplyArray = supplyArray;
            }

            public bool TrySpawn()
            {
                InfiniteGrid grid = InfiniteGrid.Instance;
                if (grid == null || _manager.Runner == null)
                    return false;

                Vector3 position = grid.GetCellCenterPositionFromCellIndex(_index);
                _towerObject = _manager.Runner.Spawn(_towerRef, position, Quaternion.identity);
                return _towerObject != null && _towerObject.TryGetComponent(out _tower);
            }

            public bool ValidatePlacement()
            {
                return _tower != null &&
                       _tower.TowerID == _requestedTowerId &&
                       _tower.HasGridOccupation;
            }

            public bool TryInitialize()
            {
                return _tower != null &&
                       CanRegisterCenterTower(_tower, _builder) &&
                       TryInitializeSpecialTower(_tower, _supplyArray);
            }

            public bool TryPay()
            {
                return _tower != null &&
                       _manager._resourcePayment != null &&
                       _manager._resourcePayment.TryPay(_tower.Cost);
            }

            public void Commit()
            {
                if (_tower == null)
                    return;

                _manager.InjectTowerDependencies(_tower);
                RegisterCenterTower(_tower, _builder);
            }

            public void Rollback()
            {
                if (_rolledBack)
                    return;

                _rolledBack = true;
                _manager.DespawnFailedTower(_towerObject);
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestTowerBuild(
            NetworkPrefabRef towerRef,
            string requestedTowerId,
            NetworkId builderId,
            Vector2Int index,
            int[] supplyArray,
            RpcInfo rpcInfo = default)
        {
            bool success = TryBuildTower(
                towerRef,
                requestedTowerId,
                builderId,
                index,
                supplyArray,
                rpcInfo.Source);
            bool isSupplyTower = requestedTowerId == TowerIDContainer.SUPPLY_TOWER_ID;
            SendTowerBuildResult(rpcInfo.Source, success, isSupplyTower);
        }

        private void SendTowerBuildResult(PlayerRef requester, bool success, bool isSupplyTower)
        {
            if (requester == PlayerRef.None)
            {
                OnTowerBuildRequestCompleted?.Invoke(success, isSupplyTower);
                return;
            }

            RPC_NotifyTowerBuildResult(requester, success, isSupplyTower);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_NotifyTowerBuildResult(
            [RpcTarget] PlayerRef requester,
            bool success,
            bool isSupplyTower)
        {
            OnTowerBuildRequestCompleted?.Invoke(success, isSupplyTower);
        }
    }
}
