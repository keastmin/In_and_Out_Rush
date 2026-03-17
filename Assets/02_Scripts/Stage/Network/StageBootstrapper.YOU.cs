using Dev.Local;
using UnityEngine;

namespace Dev.Network
{
    public partial class StageBootstrapper
    {
        [SerializeField] private StageManager _stageManager;
        [SerializeField] private StageSystem _stageSystem;
        [SerializeField] private PlayerRunner _playerRunner;
        [SerializeField] private ResourceSpawnSystem _resourceSpawnSystem;

        [Header("Gate")]
        [SerializeField] private Gate _gatePrefab;
        [SerializeField] private StageResultView _stageResultView;
        [SerializeField] private float _worldBoundaryRadius = 1000f;

        private bool _isGameOverPresented = false;
        private bool _isReturningToTitle = false;

        private void YOUInitializeHost()
        {
            // Debug.Log("StageBootstrapper: initialize host complete");
            _playerRunner = _stageManager.PlayerRunner;
        }

        private void YOUCreateObjects()
        {

        }

        private void YOUInitializeObjects()
        {
            _resourceSpawnSystem.SetUp();
            _stageResultView.Hide();
        }

        private void YOUBindObjects()
        {
            _playerRunner.OnDied += HandlePlayerDied;
            // Gate.OnGateEntered += HandleGateEntered;
        }

        private void YOUSetUpObjects()
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

        private void HandlePlayerDied(PlayerRunner runner, object sender)
        {
            _stageSystem.Defeat();
        }

        private void HandleGateEntered(Collider other, Gate gate, object sender)
        {
            _stageSystem.Victory();
        }
    }
}