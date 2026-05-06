using Dev.Local;
using UnityEngine;

namespace Dev.Network
{
    public partial class StageBootstrapper
    {
        [SerializeField] private Store _store;
        [SerializeField] private StageSystem _stageSystem;
        [SerializeField] private ResourceSpawnSystem _resourceSpawnSystem;

        [Header("Gate")]
        [SerializeField] private Gate _gatePrefab;
        [SerializeField] private StageResultView _stageResultView;
        [SerializeField] private float _worldBoundaryRadius = 1000f;

        private bool _isGameOverPresented = false;
        private bool _isReturningToTitle = false;

        public event global::System.Action<PlayerRunner, Gate, object> OnGateEntered;

        private void YOUInitializeHost()
        {
            // Debug.Log("StageBootstrapper: initialize host complete");
        }

        private void YOUCreateObjects()
        {

        }

        private void YOUInitializeObjects()
        {
            _store.PlayerRunnerMovementSpeed = 5f;
            _store.PlayerRunnerDashScaler = 2f;

            _resourceSpawnSystem.SetUp();
            _stageResultView.Hide();
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
