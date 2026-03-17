using System.Collections;
using Dev.Local;
using UnityEngine;

namespace Dev.Network
{
    public class StageBootstrapper : Entity
    {
        [SerializeField] private StageManager _stageManager;
        [SerializeField] private PlayerRunner _playerRunner;
        [SerializeField] private ResourceSpawnSystem _resourceSpawnSystem;

        [Header("Gate")]
        [SerializeField] private Gate _gatePrefab;
        [SerializeField] private StageResultView _stageResultView;
        [SerializeField] private float _worldBoundaryRadius = 1000f;

        private bool _isGameOverPresented = false;
        private bool _isReturningToTitle = false;

        protected override void OnInitialize()
        {
            StartCoroutine(InitializeRoutine());
        }

        private IEnumerator InitializeRoutine()
        {
            yield return new WaitUntil(IsSessionReady); // OK

            if (Object.HasStateAuthority)
                yield return InitializeHost();
            
            yield return new WaitUntil(IsHostInitialized);

            CreateObjects();
            InitializeObjects();
            BindObjects();
            SetUpObjects();
        }

        private bool IsSessionReady()
            => NetworkManager.Instance != null && NetworkManager.Instance.Registry != null;

        private IEnumerator InitializeHost()
        {
            yield return new WaitUntil(() => _stageManager.IsInitialized);

            // Debug.Log("StageBootstrapper: initialize host complete");
            _playerRunner = _stageManager.PlayerRunner;
        }

        private bool IsHostInitialized()
            => true;

        private void CreateObjects() {}

        private void InitializeObjects()
        {
            _resourceSpawnSystem.SetUp();
            _stageResultView.Hide();
        }

        [SerializeField] private StageSystem _stageSystem;
        private void BindObjects()
        {
            _playerRunner.OnDied += HandlePlayerDied;
            // Gate.OnGateEntered += HandleGateEntered;
        }

        private void HandlePlayerDied(PlayerRunner runner, object sender)
        {
            _stageSystem.Defeat();
        }

        private void HandleGateEntered(Collider other, Gate gate, object sender)
        {
            _stageSystem.Victory();
        }

        private void SetUpObjects()
        {
            // TODO: Field Object 생성
            var randomAngle = Random.value * 360f;
            var randomDistance = _worldBoundaryRadius;
            var gatePosition = new Vector3(
                Mathf.Cos(randomAngle * Mathf.Deg2Rad) * randomDistance,
                0f,
                Mathf.Sin(randomAngle * Mathf.Deg2Rad) * randomDistance
            );
            var gate = Runner.Spawn(_gatePrefab, gatePosition, Quaternion.identity);
            gate.OnGateEntered += HandleGateEntered;
        }
    }
}