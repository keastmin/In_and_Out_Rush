using Fusion;
using UnityEngine;
using KIM.Dev;

namespace Dev.Network
{
    public partial class StageBootstrapper
    {
        [Header("Scene Load Entities")]
        [SerializeField] private InfiniteGrid _grid;

        [Header("Prefabs")]
        [SerializeField] private Laboratory _laboratoryPrefab;

        public InfiniteGrid Grid => _grid;
        [HideInInspector] [Networked] public Laboratory NetworkLaboratory { get; private set; }
        private Laboratory _localLaboratory;
        private TowerBuildManager _towerBuildManager;

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
