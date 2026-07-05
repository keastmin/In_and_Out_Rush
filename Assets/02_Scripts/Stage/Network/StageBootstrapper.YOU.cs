using Dev.Local;
using UnityEngine;

namespace Dev.Network
{
    public partial class StageBootstrapper
    {
        [SerializeField] private Store _store;
        [SerializeField] private StageSystem _stageSystem;
        [SerializeField] private ResourceSpawnSystem _resourceSpawnSystem;
        [SerializeField] private TimeSystem timeSystem;
        [SerializeField] private TrackSystem roundTrackSystem;
        [SerializeField] private TerritorySystem territorySystem;
        [SerializeField] private TrackMonsterSpawnSystem trackMonsterSpawnSystem;

        [Header("Gate")]
        [SerializeField] private Gate _gatePrefab;
        [SerializeField] private StageResultView _stageResultView;
        [SerializeField] private float _worldBoundaryRadius = 1000f;

        private bool _isGameOverPresented = false;
        private bool _isReturningToTitle = false;

        public event global::System.Action<PlayerRunner, Gate, object> OnGateEntered;
        public TrackSystem RoundTrackSystem => roundTrackSystem;
        public TerritorySystem TerritorySystem =>
            territorySystem != null ? territorySystem : UnityEngine.Object.FindFirstObjectByType<TerritorySystem>();

        private void YOUInitializeHost()
        {
            // Debug.Log("StageBootstrapper: initialize host complete");
        }

        private void YOUSetUpRoundSystems(NetworkSystemBase[] systems)
        {
            ResolveRoundSystemReferences(systems);
            BindRoundSystemEvents();
            timeSystem?.SetUp();
        }

        private void YOUDisposeRoundSystems()
        {
            UnbindRoundSystemEvents();
        }

        private void ResolveRoundSystemReferences(NetworkSystemBase[] systems)
        {
            if (roundTrackSystem == null)
                roundTrackSystem = FindNetworkSystem<TrackSystem>(systems);

            if (territorySystem == null)
                territorySystem = FindNetworkSystem<TerritorySystem>(systems);

            if (trackMonsterSpawnSystem == null)
                trackMonsterSpawnSystem = FindNetworkSystem<TrackMonsterSpawnSystem>(systems);

            if (timeSystem == null)
                timeSystem = UnityEngine.Object.FindFirstObjectByType<TimeSystem>();
        }

        private T FindNetworkSystem<T>(NetworkSystemBase[] systems) where T : NetworkSystemBase
        {
            foreach (var system in systems)
            {
                if (system is T typedSystem)
                    return typedSystem;
            }

            return UnityEngine.Object.FindFirstObjectByType<T>();
        }

        private void BindRoundSystemEvents()
        {
            if (timeSystem == null)
            {
                Debug.LogWarning("StageBootstrapper could not find TimeSystem. Round progression will not run.");
                return;
            }

            timeSystem.OnRoundStarting -= HandleRoundStarting;
            timeSystem.OnRoundEnded -= HandleRoundEnded;
            timeSystem.OnBerserkStarted -= HandleBerserkStarted;

            timeSystem.OnRoundStarting += HandleRoundStarting;
            timeSystem.OnRoundEnded += HandleRoundEnded;
            timeSystem.OnBerserkStarted += HandleBerserkStarted;
        }

        private void UnbindRoundSystemEvents()
        {
            if (timeSystem == null)
                return;

            timeSystem.OnRoundStarting -= HandleRoundStarting;
            timeSystem.OnRoundEnded -= HandleRoundEnded;
            timeSystem.OnBerserkStarted -= HandleBerserkStarted;
        }

        private void HandleRoundStarting(int round, TimeSystem sender, object context)
        {
            if (!HasStateAuthority)
                return;

            if (trackMonsterSpawnSystem == null)
            {
                Debug.LogWarning("TrackMonsterSpawnSystem is missing. Track monster spawn skipped.");
                return;
            }

            if (round == 5 || round == 8 || round >= 11)
            {
                trackMonsterSpawnSystem.StrengthenTrackMonsters();
                Debug.Log($"New track monsters strengthened before round {round}.");
            }

            if (roundTrackSystem == null)
            {
                Debug.LogWarning("TrackSystem is missing. Track monster spawn skipped.");
                return;
            }

            trackMonsterSpawnSystem.SpawnMonsters(roundTrackSystem.Track);
            Debug.Log($"Track monsters spawned for round {round}.");
        }

        private void HandleRoundEnded(int round, TimeSystem sender, object context)
        {
            if (!HasStateAuthority)
                return;

            if (round == 3 || round == 7 || round == 9)
            {
                if (roundTrackSystem == null)
                {
                    Debug.LogWarning("TrackSystem is missing. Track expansion skipped.");
                    return;
                }

                roundTrackSystem.ExpandTrack();
                Debug.Log($"Track expanded after round {round}.");
            }
        }

        private void HandleBerserkStarted(TimeSystem sender, object context)
        {
            if (!HasStateAuthority)
                return;

            Debug.Log("StageBootstrapper received berserk start.");
        }

        private void YOUCreateObjects()
        {

        }

        private void YOUInitializeObjects()
        {
            _store.PlayerRunnerMovementSpeed = 5f;
            _store.PlayerRunnerDashScaler = 2f;

            if (NetworkLaboratory != null)
                _resourceSpawnSystem.SetStartPosition(NetworkLaboratory.transform.position);
            else
                Debug.LogWarning("ResourceSpawnSystem could not receive the laboratory start position.");

            _resourceSpawnSystem.SetUp();
            _stageResultView.Hide();

            YOUSetUpRoundSystems(systems);
        }

        private void YOUBindObjects()
        {
            Globals.Store = _store;

            PlayerRunner.OnDied += HandlePlayerDied;
        }

        private void YOUSetUpObjects()
        {
            if (!HasStateAuthority)
                return;

            // TODO: Field Object 생성
            var randomAngle = Random.value * 360f;
            var randomDistance = _worldBoundaryRadius;
            var gatePosition = new Vector3(
                Mathf.Cos(randomAngle * Mathf.Deg2Rad) * randomDistance,
                0f,
                Mathf.Sin(randomAngle * Mathf.Deg2Rad) * randomDistance
            );
            var gate = Runner.Spawn(_gatePrefab, gatePosition, Quaternion.identity);
            gate.OnPlayerRunnerEntered += HandleGateEntered;
        }

        private void HandlePlayerDied(PlayerRunner runner, object sender)
        {
            Debug.Log($"PlayerRunner died. Health: {runner.Health}");
            _stageSystem.Defeat();
        }

        private void HandleGateEntered(PlayerRunner runner, Gate gate, object sender)
        {
            OnGateEntered?.Invoke(runner, gate, this);
            _stageSystem.Victory();

            //// RPC를 통해 클라이언트에게 게임 승리/종료 팝업 띄우기
            //if (InterfaceManager.Instance != null)
            //{
            //    InterfaceManager.Instance.SetVictoryUIActivation(true);
            //}
        }

    }
}
