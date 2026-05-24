using System;
using System.Collections;
using Dev.Local;
using Fusion;
using UnityEngine;
using KIM.Dev;

namespace Dev.Network
{
    public partial class StageBootstrapper : Entity
    {
        [Networked] public PlayerRunner PlayerRunner { get; private set; }
        [Networked] public PlayerBuilder PlayerBuilder { get; private set; }
        public CinemachineSystem CinemachineSystem { get; private set; }

        public static StageBootstrapper Instance { get; private set; }

        [Space(10)]

        [Header("Player")]
        [SerializeField] private PlayerRunner playerRunnerPrefab;
        [SerializeField] private PlayerBuilder playerBuilderPrefab;

        [Space(10)]

        [Header("Network Systems")]
        [SerializeField] private NetworkInputSystem networkInputSystemPrefab;
        [SerializeField] private NetworkSystemBase[] systems;
        public ResourceSystem ResourceSystem;

        [Space(10)]

        [Header("Local Systems")]
        [SerializeField] private CinemachineSystem cinemachineSystemPrefab;
        public StageUIController UIController;

        [Space(10)]

        [Header("Territory")]
        public TerritoryVisible TerritoryVisible;
        public TrackVisible TrackVisible;

        private bool _initialized = false;

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
            => NetworkManager.Instance != null && NetworkManager.Instance.Registry != null;

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
                PlayerRunner = Runner.Spawn(runnerPrefab, Vector3.zero - (Vector3.forward * 4f), Quaternion.identity, runnerPlayer);
            PlayerRunner.name = $"{Runner.name} - Player Runner";

            var builderPlayer = NetworkManager.Instance.Registry.GetPlayerRefFromPosition(PlayerPosition.Builder);
            if (builderPlayer == PlayerRef.None)
                PlayerBuilder = Runner.Spawn(builderPrefab, Vector3.zero, Quaternion.identity);
            else
                PlayerBuilder = Runner.Spawn(builderPrefab, Vector3.zero, Quaternion.identity, builderPlayer);
            PlayerBuilder.name = $"{Runner.name} - Player Builder";

            Debug.Log($"{Runner.name} - Player spawned");
        }

        private void SpawnNetworkInputSystem()
        {
            var instance = Runner.Spawn(networkInputSystemPrefab, Vector3.zero, Quaternion.identity);
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
            YOUInitializeObjects();
            KIMInitializeObjects();
        }

        private void BindObjects()
        {
            YOUBindObjects();
            KIMBindObjects();
        }

        private void SetUpObjects()
        {
            YOUSetUpObjects();
            KIMSetUpObjects();
        }
    }
}
