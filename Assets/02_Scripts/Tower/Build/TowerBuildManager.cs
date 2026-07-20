using System;
using Dev.Network;
using Fusion;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class TowerBuildManager : NetworkBehaviour
    {
        public static TowerBuildManager Instance { get; private set; }

        public event Action<bool, bool> OnTowerBuildRequestCompleted;

        private TowerUpgradeManager _towerUpgradeManager;

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

        public void Initialize(TowerUpgradeManager towerUpgradeManager)
        {
            _towerUpgradeManager = towerUpgradeManager != null
                ? towerUpgradeManager
                : GetComponent<TowerUpgradeManager>();

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

            ResourceSystem resourceSystem = ResourceSystem.Instance;
            if (resourceSystem == null ||
                resourceSystem.Mineral < tower.Cost.Mineral ||
                resourceSystem.Gas < tower.Cost.Gas)
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
            if (!HasStateAuthority || Runner == null || InfiniteGrid.Instance == null || ResourceSystem.Instance == null)
                return false;

            if (!Runner.TryFindObject(builderId, out NetworkObject builderObject) ||
                !builderObject.TryGetComponent(out PlayerBuilder builder))
            {
                return false;
            }

            if (requester != PlayerRef.None && builderObject.InputAuthority != requester)
                return false;

            if (!CanBuildSpecialTower(requestedTowerId, supplyArray))
                return false;

            Vector3 position = InfiniteGrid.Instance.GetCellCenterPositionFromCellIndex(index);
            NetworkObject towerObject = Runner.Spawn(towerRef, position, Quaternion.identity);
            if (towerObject == null || !towerObject.TryGetComponent(out Tower tower))
            {
                DespawnFailedTower(towerObject);
                return false;
            }

            bool isValid = tower.TowerID == requestedTowerId &&
                           tower.HasGridOccupation &&
                           HasSufficientResources(tower.Cost) &&
                           CanRegisterCenterTower(tower, builder) &&
                           TryInitializeSpecialTower(tower, supplyArray);

            if (!isValid)
            {
                DespawnFailedTower(towerObject);
                return false;
            }

            InjectTowerDependencies(tower);
            RegisterCenterTower(tower, builder);
            ResourceSystem.Instance.Mineral -= tower.Cost.Mineral;
            ResourceSystem.Instance.Gas -= tower.Cost.Gas;
            return true;
        }

        private static bool HasSufficientResources(Cost cost)
        {
            return ResourceSystem.Instance.Mineral >= cost.Mineral &&
                   ResourceSystem.Instance.Gas >= cost.Gas;
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
