using Fusion;
using System.Collections.Generic;
using Dev.Local;
using UnityEngine;
using KIM.Dev;

namespace Dev.Network
{
    public partial class StageBootstrapper
    {
        [Header("Scene Load Entities")]
        [SerializeField] private InfiniteGrid _grid;
        [SerializeField] private InfiniteGridObstacleSpawner _obstacleSpawner;

        [Header("Prefabs")]
        [SerializeField] private Laboratory _laboratoryPrefab;

        public InfiniteGrid Grid => _grid;
        [HideInInspector] [Networked] public Laboratory NetworkLaboratory { get; private set; }
        private Laboratory _localLaboratory;
        private TowerBuildManager _towerBuildManager;
        private bool _additiveSceneReferencesReady;

        private void Awake()
        {
            ResolveAdditiveSceneReferences();
        }

        private void ResolveAdditiveSceneReferences()
        {
            _grid ??= FindFirstObjectByType<InfiniteGrid>(FindObjectsInactive.Include);
            _obstacleSpawner ??= FindFirstObjectByType<InfiniteGridObstacleSpawner>(FindObjectsInactive.Include);
            _pingSystem ??= FindFirstObjectByType<PingSystem>(FindObjectsInactive.Include);
            _towerUpgradeManager ??= FindFirstObjectByType<TowerUpgradeManager>(FindObjectsInactive.Include);
            _fogOfWarSystem ??= FindFirstObjectByType<FogOfWarSystem>(FindObjectsInactive.Include);
            CinemachineSystem ??= FindFirstObjectByType<global::CinemachineSystem>(FindObjectsInactive.Include);
            UIController ??= FindFirstObjectByType<global::StageUIController>(FindObjectsInactive.Include);
            TerritoryVisible ??= FindFirstObjectByType<Dev.Local.TerritoryVisible>(FindObjectsInactive.Include);
            TrackVisible ??= FindFirstObjectByType<Dev.Local.TrackVisible>(FindObjectsInactive.Include);
            ResourceSystem ??= GetComponentInChildren<KIM.Dev.ResourceSystem>(true);
            ResourceSystem ??= FindFirstObjectByType<KIM.Dev.ResourceSystem>(FindObjectsInactive.Include);

            _store ??= FindFirstObjectByType<Store>(FindObjectsInactive.Include);
            _stageSystem ??= FindFirstObjectByType<StageSystem>(FindObjectsInactive.Include);
            _resourceSpawnSystem ??= FindFirstObjectByType<ResourceSpawnSystem>(FindObjectsInactive.Include);
            timeSystem ??= FindFirstObjectByType<TimeSystem>(FindObjectsInactive.Include);
            roundTrackSystem ??= FindFirstObjectByType<TrackSystem>(FindObjectsInactive.Include);
            territorySystem ??= FindFirstObjectByType<TerritorySystem>(FindObjectsInactive.Include);
            trackMonsterSpawnSystem ??= FindFirstObjectByType<TrackMonsterSpawnSystem>(FindObjectsInactive.Include);
            worldMonsterSpawnSystem ??= FindFirstObjectByType<WorldMonsterSpawnSystem>(FindObjectsInactive.Include);
            sacredZoneSystem ??= FindFirstObjectByType<SacredZoneSystem>(FindObjectsInactive.Include);
            _stageResultView ??= FindFirstObjectByType<StageResultView>(FindObjectsInactive.Include);

            NetworkSystemBase[] discoveredSystems = FindObjectsByType<NetworkSystemBase>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            if (discoveredSystems.Length > 0)
                systems = OrderSystems(discoveredSystems);

            _stageSystem?.SetStageResultView(_stageResultView);
            _additiveSceneReferencesReady = HasRequiredAdditiveSceneReferences();
        }

        private bool AreAdditiveSceneReferencesReady()
        {
            return _additiveSceneReferencesReady;
        }

        private bool HasRequiredAdditiveSceneReferences()
        {
            return _grid != null &&
                   _obstacleSpawner != null &&
                   _pingSystem != null &&
                   _towerUpgradeManager != null &&
                   _fogOfWarSystem != null &&
                   CinemachineSystem != null &&
                   UIController != null &&
                   TerritoryVisible != null &&
                   TrackVisible != null &&
                   ResourceSystem != null &&
                   _store != null &&
                   _stageSystem != null &&
                   _resourceSpawnSystem != null &&
                   timeSystem != null &&
                   roundTrackSystem != null &&
                   territorySystem != null &&
                   trackMonsterSpawnSystem != null &&
                   worldMonsterSpawnSystem != null &&
                   sacredZoneSystem != null &&
                   _stageResultView != null &&
                   systems != null &&
                   systems.Length > 0;
        }

        private static NetworkSystemBase[] OrderSystems(NetworkSystemBase[] discoveredSystems)
        {
            var orderedSystems = new List<NetworkSystemBase>(discoveredSystems.Length);
            AddSystem<TerritorySystem>(orderedSystems, discoveredSystems);
            AddSystem<TrackSystem>(orderedSystems, discoveredSystems);
            AddSystem<TrackMonsterSpawnSystem>(orderedSystems, discoveredSystems);
            AddSystem<WorldMonsterSpawnSystem>(orderedSystems, discoveredSystems);

            for (int i = 0; i < discoveredSystems.Length; i++)
            {
                NetworkSystemBase system = discoveredSystems[i];
                if (system != null && !orderedSystems.Contains(system))
                    orderedSystems.Add(system);
            }

            return orderedSystems.ToArray();
        }

        private static void AddSystem<T>(List<NetworkSystemBase> orderedSystems, NetworkSystemBase[] discoveredSystems)
            where T : NetworkSystemBase
        {
            for (int i = 0; i < discoveredSystems.Length; i++)
            {
                if (discoveredSystems[i] is T typedSystem)
                    orderedSystems.Add(typedSystem);
            }
        }

        private void KIMInitializeHost()
        {
            
        }

        private void KIMCreateObjects()
        {
            SpawnLaboratory();
        }

        private void KIMInitializeObjects()
        {
            if (_towerUpgradeManager != null)
                _towerUpgradeManager.TryGetComponent(out _towerBuildManager);

            if (_towerBuildManager == null)
            {
                Debug.LogError("TowerBuildManager 참조를 찾을 수 없습니다.");
            }
            else
            {
                _towerBuildManager.Initialize(_towerUpgradeManager);
                PlayerBuilder?.InjectTowerBuildManager(_towerBuildManager);
            }

            PlayerBuilder?.InjectTowerMoveDependencies(timeSystem, ResourceSystem, Grid);
            UIController.InitializeStageUIController(_towerUpgradeManager, ResourceSystem, timeSystem);
        }

        private void KIMBindObjects()
        {
            if (timeSystem != null)
            {
                timeSystem.OnRoundStarting -= HandleKimRoundStarting;
                timeSystem.OnRoundStarting += HandleKimRoundStarting;
            }
        }

        private void KIMSetUpObjects()
        {

        }

        private void SpawnObstacles()
        {
            if (!HasStateAuthority)
                return;

            _obstacleSpawner.SpawnObstacles();
        }

        private void KIMInitializeWorldObstacleConsumer(IWorldObstacleConsumer consumer)
        {
            if (!HasStateAuthority || consumer == null)
                return;

            consumer.InitializeWorldObstacles(_obstacleSpawner.SpawnedObstacles);
        }

        private void HandleKimRoundStarting(int round, TimeSystem sender, object context)
        {
            if (!HasStateAuthority)
                return;

            Grid?.DestroyTowersBlockedByTrack();
        }

        // 연구소 스폰
        private void SpawnLaboratory()
        {
            if (HasStateAuthority)
            {
                Vector3 labSpawnPos = Grid.GetCellCenterPosition(Vector3.zero);
                NetworkLaboratory = Runner.Spawn(_laboratoryPrefab, labSpawnPos, Quaternion.identity);
            }
        }

        public void OnSpawnedLaboratory(Laboratory laboratory)
        {
            _localLaboratory = laboratory;
            CinemachineSystem?.SetLaboratoryTarget(laboratory.transform);
            TryInjectBuilderUI(laboratory);
        }

        public bool TryOpenLaboratoryUI(Laboratory laboratory)
        {
            if (!TryInjectBuilderUI(laboratory))
                return false;

            if (!laboratory.TryGetBuilderUI(out var builderUI))
                return false;

            builderUI.OnClickLaboratoryButton(true);
            return true;
        }

        public bool TryInjectBuilderUI(Laboratory laboratory)
        {
            if (laboratory == null || !IsLocalPlayerBuilder())
                return false;

            if (UIController == null || UIController.BuilderUI == null)
                return false;

            laboratory.InjectBuilderUI(UIController.BuilderUI);
            return true;
        }

        private void TryInjectSpawnedLaboratory()
        {
            TryInjectBuilderUI(_localLaboratory);
        }

        private bool IsLocalPlayerBuilder()
        {
            if (Runner == null || NetworkManager.Instance == null || NetworkManager.Instance.Registry == null)
                return false;

            var registry = NetworkManager.Instance.Registry;
            return registry.RefToPosition.ContainsKey(Runner.LocalPlayer) &&
                   registry.IsPlayerBuilder(Runner.LocalPlayer);
        }
    }
}
