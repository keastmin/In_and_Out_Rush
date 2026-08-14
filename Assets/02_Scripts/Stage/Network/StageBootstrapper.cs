using System;
using System.Collections;
using Dev.Local;
using Fusion;
using Unity.Profiling;
using UnityEngine;
using KIM.Dev;

namespace Dev.Network
{
    public partial class StageBootstrapper : Entity
    {
        private static readonly ProfilerMarker FixedUpdateMarker = new("StageBootstrapper.FixedUpdateNetwork");
        private static readonly ProfilerMarker ConfigureAreaOfInterestMarker =
            new("StageBootstrapper.ConfigureAreaOfInterestGrid");
        private static readonly ProfilerMarker RegisterAreaOfInterestMarker =
            new("StageBootstrapper.RegisterPlayerAreaOfInterest");
        private const float PlayerAreaOfInterestRadius = 128f;
        private const int AreaOfInterestCellSize = 64;

        [Networked] public PlayerRunner PlayerRunner { get; private set; }
        [Networked] public PlayerBuilder PlayerBuilder { get; private set; }

        public static StageBootstrapper Instance { get; private set; }

        [Space(10)]

        [Header("Player")]
        [SerializeField] private PlayerRunner playerRunnerPrefab;
        [SerializeField] private PlayerBuilder playerBuilderPrefab;

        [Space(10)]

        [Header("Network Systems")]
        [SerializeField] private NetworkInputSystem networkInputSystemPrefab;
        [SerializeField] private NetworkSystemBase[] systems;
        [SerializeField] private PingSystem _pingSystem;
        [SerializeField] private KIM.Dev.TowerUpgradeManager _towerUpgradeManager;
        public KIM.Dev.ResourceSystem ResourceSystem;

        [Space(10)]

        [Header("Local Systems")]
        [SerializeField] private FogOfWarSystem _fogOfWarSystem;
        public CinemachineSystem CinemachineSystem;
        public StageUIController UIController;

        [Space(10)]

        [Header("Territory")]
        public TerritoryVisible TerritoryVisible;
        public TrackVisible TrackVisible;

        private bool _initialized = false;
        private bool _isAreaOfInterestGridConfigured;

        public bool IsInitialized => _initialized;

        protected override void OnInitialize()
        {
            Debug.Log("StageBootstrapper Spawned");
            Instance = this;
            StartCoroutine(InitializeRoutine());
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            YOUDisposeRoundSystems();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private IEnumerator InitializeRoutine()
        {
            yield return new WaitUntil(IsSessionReady); // OK

            if (Object.HasStateAuthority)
            {
                SpawnPlayer();
                SpawnNetworkInputSystem();
            }

            yield return new WaitUntil(ArePlayersReady);

            InitCinemachineSystem();
            SetUpNetworkSystems();
            InitUIController();
            LaboratoryUIInjectionPlayerRunner(UIController.BuilderUI, PlayerRunner);
            BuilderReferenceBind(PlayerBuilder, UIController.BuilderUI);
            TryInjectSpawnedLaboratory();

            Debug.Log("StageBootstrapper init complete");
            _initialized = true;

            if (Object.HasStateAuthority)
                yield return InitializeHost();

            yield return new WaitUntil(IsHostInitialized);

            CreateObjects(); // 오브젝트 스폰
            InitializeObjects(); // 스폰된 오브젝트를 초기화
            BindObjects(); // 참조 연결
            SetUpObjects(); // 참조 연결 부재로 인해 보류된 초기화 로직 실행, 후순위 초기화 로직 실행
        }

        private bool IsSessionReady()
        {
            if (!AreAdditiveSceneReferencesReady())
                ResolveAdditiveSceneReferences();

            return NetworkManager.Instance != null &&
                   NetworkManager.Instance.Registry != null &&
                   AreAdditiveSceneReferencesReady();
        }

        private bool ArePlayersReady()
            => PlayerRunner != null && PlayerBuilder != null;

        private IEnumerator InitializeHost()
        {
            YOUInitializeHost();
            KIMInitializeHost();
            yield break;
        }

        private bool IsHostInitialized()
            => true;

        private void SpawnPlayer()
        {
            var runnerPrefab = ResourceManager.Instance != null ? ResourceManager.Instance.PlayerRunnerPrefab : playerRunnerPrefab;
            var builderPrefab = ResourceManager.Instance != null ? ResourceManager.Instance.PlayerBuilderPrefab : playerBuilderPrefab;

            var runnerPlayer = NetworkManager.Instance.Registry.GetPlayerRefFromPosition(PlayerPosition.Runner);
            if (runnerPlayer == PlayerRef.None)
                PlayerRunner = Runner.Spawn(runnerPrefab, Vector3.zero - (Vector3.forward * 4f), Quaternion.identity);
            else
            {
                PlayerRunner = Runner.Spawn(runnerPrefab, Vector3.zero - (Vector3.forward * 4f), Quaternion.identity, runnerPlayer);
                Runner.SetPlayerObject(runnerPlayer, PlayerRunner.Object);
            }
            PlayerRunner.name = $"{Runner.name} - Player Runner";

            var builderPlayer = NetworkManager.Instance.Registry.GetPlayerRefFromPosition(PlayerPosition.Builder);
            if (builderPlayer == PlayerRef.None)
                PlayerBuilder = Runner.Spawn(builderPrefab, Vector3.zero, Quaternion.identity);
            else
            {
                PlayerBuilder = Runner.Spawn(builderPrefab, Vector3.zero, Quaternion.identity, builderPlayer);
                Runner.SetPlayerObject(builderPlayer, PlayerBuilder.Object);
            }
            PlayerBuilder.name = $"{Runner.name} - Player Builder";

            Debug.Log($"{Runner.name} - Player spawned");
        }

        private void SpawnNetworkInputSystem()
        {
            if (Runner == null)
            {
                Debug.LogError("StageBootstrapper cannot spawn NetworkInputSystem because Runner is null.", this);
                return;
            }

            if (networkInputSystemPrefab == null)
            {
                Debug.LogError("StageBootstrapper requires a NetworkInputSystem prefab reference.", this);
                return;
            }

            var networkObjectPrefab = networkInputSystemPrefab.GetComponent<NetworkObject>();
            if (networkObjectPrefab == null)
            {
                Debug.LogError("NetworkInputSystem prefab must have a NetworkObject component.", networkInputSystemPrefab);
                return;
            }

            var spawnedObject = Runner.Spawn(networkObjectPrefab, Vector3.zero, Quaternion.identity);
            if (spawnedObject == null)
            {
                Debug.LogError("Runner failed to spawn NetworkInputSystem prefab.", networkObjectPrefab);
                return;
            }

            var instance = spawnedObject.GetComponent<NetworkInputSystem>();
            if (instance == null)
            {
                Debug.LogError("Spawned NetworkInputSystem object does not have a NetworkInputSystem component.", spawnedObject);
                return;
            }

            instance.name = $"{Runner.name} - NetworkInputSystem";
            Debug.Log($"{Runner.name} - NetworkInputSystem spawned");
        }

        private void SetUpNetworkSystems()
        {
            foreach (var system in systems)
            {
                if (system == null)
                    continue;

                system.SetUp();
            }
        }

        private void InitUIController()
        {
            var playerPosition = NetworkManager.Instance.Registry.RefToPosition[Runner.LocalPlayer];
            UIController.SetPlayerUI(playerPosition);
        }

        private void InitCinemachineSystem()
        {
            var playerPosition = NetworkManager.Instance.Registry.RefToPosition[Runner.LocalPlayer];
            CinemachineSystem.Initialize(playerPosition, PlayerRunner, PlayerBuilder);
        }

        private void LaboratoryUIInjectionPlayerRunner(PlayerBuilderUI builderUI, PlayerRunner runner)
        {
            builderUI.LaboratoryUIInjectionRunner(runner);
        }

        private void BuilderReferenceBind(PlayerBuilder builder, PlayerBuilderUI builderUI)
        {
            builder.PlayerBuilderReferenceInjection(builderUI);
        }

        private void CreateObjects() 
        {
            YOUCreateObjects();
            KIMCreateObjects();
        }

        private void InitializeObjects()
        {
            SpawnObstacles(); // 장애물 오브젝트 스폰
            YOUInitializeObjects();
            KIMInitializeObjects();
        }

        private void BindObjects()
        {
            InitializePingSystem(); // 핑 시스템 초기화
            YOUBindObjects();
            KIMBindObjects();
        }

        private void SetUpObjects()
        {
            YOUSetUpObjects();
            KIMSetUpObjects();
        }

        // 플레이어 러너가 스폰되고나면 호출되는 함수
        public void LocalPlayerRunnerSpawned(PlayerRunner playerRunner)
        {
            PlayerRunnerReferenceInjectToUI(playerRunner);
            _fogOfWarSystem.InitializeFogOfWarSystem(playerRunner);
            _fogOfWarSystem.SetTerritorySource(TerritoryVisible.GetComponent<MeshFilter>(), TerritoryVisible.GetComponent<MeshRenderer>());
            _fogOfWarSystem.SetWorldBounds(_worldBoundaryRadius);
        }

        // 플레이어 러너 참조를 UI 컨트롤러에 전달하는 함수
        private void PlayerRunnerReferenceInjectToUI(PlayerRunner playerRunner)
        {
            UIController.GetPlayerRunnerReference(playerRunner);
        }

        // 핑 시스템 초기화
        private void InitializePingSystem()
        {
            // 러너의 핑 가이드 참조 전달
            _pingSystem.InitializePingSystem(PlayerRunner.PingGuide);
        }

        public override void FixedUpdateNetwork()
        {
            using (FixedUpdateMarker.Auto())
            {
                if (!Runner.IsServer)
                    return;

                ConfigureAreaOfInterestGrid();

                using (RegisterAreaOfInterestMarker.Auto())
                {
                    foreach (PlayerRef player in Runner.ActivePlayers)
                    {
                        if (!Runner.TryGetPlayerObject(
                                player,
                                out NetworkObject playerObject))
                        {
                            continue;
                        }

                        if (Runner.GameMode != GameMode.Shared)
                            Runner.ClearPlayerAreaOfInterest(player);

                        Runner.AddPlayerAreaOfInterest(
                            player,
                            playerObject.transform.position,
                            PlayerAreaOfInterestRadius);
                    }
                }
            }
        }

        private void ConfigureAreaOfInterestGrid()
        {
            if (_isAreaOfInterestGridConfigured)
                return;

            if (Runner.GameMode == GameMode.Shared)
            {
                _isAreaOfInterestGridConfigured = true;
                return;
            }

            using (ConfigureAreaOfInterestMarker.Auto())
                Runner.SetAreaOfInterestCellSize(AreaOfInterestCellSize);

            _isAreaOfInterestGridConfigured = true;
        }
    }
}
