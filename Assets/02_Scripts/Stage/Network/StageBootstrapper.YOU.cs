using Dev.Local;
using System.Collections.Generic;
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

        [Header("Sacred Zone")]
        [SerializeField] private SacredZoneSystem sacredZoneSystem;

        [Header("Sanctuary")]
        [SerializeField] private SanctuaryView sanctuaryPrefab;
        [SerializeField] private int sanctuaryRandomSeed = 20260712;
        [SerializeField, Min(0)] private int sanctuarySpawnCount = 3;
        [SerializeField, Min(0f)] private float sanctuaryPlacementRadius = 80f;
        [SerializeField] private Vector2 sanctuaryRadiusRange = new(5f, 12f);
        [SerializeField] private Vector2Int sanctuaryVertexCountRange = new(8, 16);
        [SerializeField, Min(0f)] private float sanctuaryVertexNoise = 1.5f;
        [SerializeField, Min(0.01f)] private float sanctuaryActiveDuration = 15f;
        [SerializeField] private bool spawnInternalizedImmediatelyWhenNoCombat = true;
        [SerializeField, Min(1)] private int sanctuaryPlacementRetryCount = 30;
        [SerializeField, Min(0f)] private float sanctuaryMinimumDistance = 8f;

        [Header("Gate")]
        [SerializeField] private Gate _gatePrefab;
        [SerializeField] private StageResultView _stageResultView;
        [SerializeField] private float _worldBoundaryRadius = 1000f;

        private bool _isGameOverPresented = false;
        private bool _isReturningToTitle = false;
        private readonly List<SanctuaryView> sanctuaries = new();
        private int queuedInternalizedMonsterCount;
        private Gate activeGate;

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
            UnbindSacredZoneEvents();
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

            FlushQueuedInternalizedMonsters();
            trackMonsterSpawnSystem.SpawnMonsters(roundTrackSystem.Track);
            Debug.Log($"Track monsters spawned for round {round}.");
        }

        private void HandleRoundEnded(int round, TimeSystem sender, object context)
        {
            if (!HasStateAuthority)
                return;

            StartInternalizedMonsterSettlement();

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

        private void StartInternalizedMonsterSettlement()
        {
            if (trackMonsterSpawnSystem == null)
            {
                Debug.LogWarning("TrackMonsterSpawnSystem is missing. Internalized monster settlement skipped.");
                return;
            }

            if (PlayerRunner == null)
            {
                Debug.LogWarning("PlayerRunner is missing. Internalized monster settlement skipped.");
                return;
            }

            trackMonsterSpawnSystem.SettleInternalizedMonstersCascade(PlayerRunner);
        }

        private void HandleBerserkStarted(TimeSystem sender, object context)
        {
            if (!HasStateAuthority)
                return;

            Debug.Log("StageBootstrapper received berserk start.");
        }

        private void YOUCreateObjects()
        {
            CreateSanctuaries();
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
            SetUpSacredZone();
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
            activeGate = gate;
            activeGate.SetUnlocked(false);
            SetSacredZoneGate(activeGate);
            gate.OnPlayerRunnerEntered += HandleGateEntered;
        }

        private void Update()
        {
            TickSanctuaries();
        }

        public bool IsPointInActiveSanctuary(Vector3 worldPosition)
        {
            for (int i = 0; i < sanctuaries.Count; i++)
            {
                SanctuaryView sanctuary = sanctuaries[i];
                if (sanctuary != null && sanctuary.IsActive && sanctuary.IsPointInSanctuary(worldPosition))
                    return true;
            }

            return false;
        }

        public bool IsRunnerProtectedBySanctuary(Vector3 runnerPosition)
            => IsPointInActiveSanctuary(runnerPosition);

        private void CreateSanctuaries()
        {
            if (sanctuaryPrefab == null || sanctuarySpawnCount <= 0)
                return;

            sanctuaries.Clear();
            var positions = new List<Vector3>();
            var random = new global::System.Random(sanctuaryRandomSeed);

            for (int i = 0; i < sanctuarySpawnCount; i++)
            {
                if (!TryCreateSanctuaryPosition(random, positions, out Vector3 position))
                    continue;

                SanctuaryView sanctuary = Instantiate(sanctuaryPrefab, position, Quaternion.identity);
                sanctuary.name = $"Sanctuary_{i}";
                sanctuary.Initialize(sanctuaryActiveDuration);
                sanctuary.SetVertices(CreateSanctuaryVertices(random));
                sanctuary.Activated += HandleSanctuaryActivated;
                sanctuary.Expired += HandleSanctuaryExpired;
                sanctuaries.Add(sanctuary);
                positions.Add(position);
            }
        }

        private bool TryCreateSanctuaryPosition(global::System.Random random, List<Vector3> existingPositions, out Vector3 position)
        {
            for (int attempt = 0; attempt < sanctuaryPlacementRetryCount; attempt++)
            {
                Vector2 offset = RandomInsideUnitCircle(random) * sanctuaryPlacementRadius;
                position = new Vector3(offset.x, 0f, offset.y);
                if (territorySystem != null &&
                    territorySystem.Territory != null &&
                    territorySystem.Territory.IsPointInPolygon(new Vector2(position.x, position.z)))
                    continue;

                bool tooClose = false;
                for (int i = 0; i < existingPositions.Count; i++)
                {
                    if (Vector3.SqrMagnitude(existingPositions[i] - position) <
                        sanctuaryMinimumDistance * sanctuaryMinimumDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                    return true;
            }

            position = Vector3.zero;
            return false;
        }

        private List<Vector2> CreateSanctuaryVertices(global::System.Random random)
        {
            int minVertexCount = Mathf.Max(3, Mathf.Min(sanctuaryVertexCountRange.x, sanctuaryVertexCountRange.y));
            int maxVertexCount = Mathf.Max(minVertexCount, Mathf.Max(sanctuaryVertexCountRange.x, sanctuaryVertexCountRange.y));
            int vertexCount = random.Next(minVertexCount, maxVertexCount + 1);

            float minRadius = Mathf.Max(0.1f, Mathf.Min(sanctuaryRadiusRange.x, sanctuaryRadiusRange.y));
            float maxRadius = Mathf.Max(minRadius, Mathf.Max(sanctuaryRadiusRange.x, sanctuaryRadiusRange.y));
            float baseRadius = RandomRange(random, minRadius, maxRadius);

            var vertices = new List<Vector2>(vertexCount);
            float partOfAngle = 2f * Mathf.PI / vertexCount;
            for (int i = 0; i < vertexCount; i++)
            {
                float angle = (vertexCount - 1 - i) * partOfAngle;
                float radius = Mathf.Max(0.1f, baseRadius + RandomRange(random, -sanctuaryVertexNoise, sanctuaryVertexNoise));
                vertices.Add(new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
            }

            return vertices;
        }

        private static Vector2 RandomInsideUnitCircle(global::System.Random random)
        {
            double angle = random.NextDouble() * Mathf.PI * 2f;
            double radius = global::System.Math.Sqrt(random.NextDouble());
            return new Vector2(
                (float)(global::System.Math.Cos(angle) * radius),
                (float)(global::System.Math.Sin(angle) * radius));
        }

        private static float RandomRange(global::System.Random random, float minInclusive, float maxInclusive)
            => minInclusive + (float)random.NextDouble() * (maxInclusive - minInclusive);

        private void TickSanctuaries()
        {
            if (sanctuaries.Count <= 0)
                return;

            for (int i = sanctuaries.Count - 1; i >= 0; i--)
            {
                SanctuaryView sanctuary = sanctuaries[i];
                if (sanctuary == null)
                {
                    sanctuaries.RemoveAt(i);
                    continue;
                }

                if (sanctuary.State == SanctuaryView.SanctuaryState.Inactive &&
                    PlayerRunner != null &&
                    sanctuary.IsPointInSanctuary(PlayerRunner.transform.position))
                {
                    sanctuary.TryActivate();
                }

                sanctuary.Tick(Time.deltaTime);
            }
        }

        private void HandleSanctuaryActivated(SanctuaryView sanctuary)
        {
            if (!HasStateAuthority || sanctuary == null)
                return;

            int internalizedCount = InternalizeWorldMonstersInSanctuary(sanctuary);
            if (internalizedCount <= 0)
                return;

            if (timeSystem != null &&
                timeSystem.Phase == RoundPhase.Maintenance &&
                !spawnInternalizedImmediatelyWhenNoCombat)
            {
                queuedInternalizedMonsterCount += internalizedCount;
                Debug.Log($"{internalizedCount} sanctuary monsters queued for next round.");
                return;
            }

            SpawnInternalizedMonsters(internalizedCount);
        }

        private void HandleSanctuaryExpired(SanctuaryView sanctuary)
        {
            if (sanctuary == null)
                return;

            sanctuary.Activated -= HandleSanctuaryActivated;
            sanctuary.Expired -= HandleSanctuaryExpired;
            sanctuaries.Remove(sanctuary);
            Destroy(sanctuary.gameObject);
        }

        private int InternalizeWorldMonstersInSanctuary(SanctuaryView sanctuary)
        {
            WorldMonster[] worldMonsters = UnityEngine.Object.FindObjectsByType<WorldMonster>(FindObjectsSortMode.None);
            int internalizedCount = 0;

            for (int i = 0; i < worldMonsters.Length; i++)
            {
                WorldMonster monster = worldMonsters[i];
                if (monster == null || !monster.CanAccessNetworkState || !monster.Object.HasStateAuthority)
                    continue;

                if (!sanctuary.IsPointInSanctuary(monster.transform.position))
                    continue;

                monster.DestroyMonster();
                internalizedCount++;
            }

            return internalizedCount;
        }

        private void FlushQueuedInternalizedMonsters()
        {
            if (queuedInternalizedMonsterCount <= 0)
                return;

            int spawnCount = queuedInternalizedMonsterCount;
            queuedInternalizedMonsterCount = 0;
            SpawnInternalizedMonsters(spawnCount);
        }

        private void SpawnInternalizedMonsters(int count)
        {
            if (!HasStateAuthority || count <= 0)
                return;

            if (trackMonsterSpawnSystem == null || roundTrackSystem == null)
            {
                queuedInternalizedMonsterCount += count;
                Debug.LogWarning("Internalized monsters queued because track systems are not ready.");
                return;
            }

            trackMonsterSpawnSystem.SpawnInternalizedMonsters(roundTrackSystem.Track, count);
        }

        private void SetUpSacredZone()
        {
            if (sacredZoneSystem == null)
            {
                Debug.LogWarning("StageBootstrapper requires a SacredZoneSystem reference. Create it in the hierarchy and assign it in the inspector.");
                return;
            }

            sacredZoneSystem.OnQuotaReached -= HandleSacredZoneQuotaReached;
            sacredZoneSystem.OnQuotaReached += HandleSacredZoneQuotaReached;
            sacredZoneSystem.Initialize(TerritorySystem, _worldBoundaryRadius);

            if (activeGate != null)
                SetSacredZoneGate(activeGate);
        }

        private void SetSacredZoneGate(Gate gate)
        {
            if (sacredZoneSystem == null)
                return;

            sacredZoneSystem.SetGate(gate);
        }

        private void UnbindSacredZoneEvents()
        {
            if (sacredZoneSystem != null)
                sacredZoneSystem.OnQuotaReached -= HandleSacredZoneQuotaReached;

            if (activeGate != null)
                activeGate.OnPlayerRunnerEntered -= HandleGateEntered;
        }

        private void HandleSacredZoneQuotaReached(SacredZoneSystem sender)
        {
            Debug.Log("Sacred zone quota reached. Gate unlocked.");
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
