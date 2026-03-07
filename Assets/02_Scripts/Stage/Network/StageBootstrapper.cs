using System;
using System.Collections;
using System.Threading.Tasks;
using Dev.Local;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dev.Network
{
    public class StageBootstrapper : Entity
    {
        [SerializeField] private StageManager _stageManager;
        [SerializeField] private PlayerRunner _playerRunner;
        [SerializeField] private ResourceSpawnSystem _resourceSpawnSystem;

        [Header("Gate")]
        public Gate Gate;
        [SerializeField] private StageResultView _stageResultView;

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
            Gate.OnGateEntered += HandleGateEntered;
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
        }
    }
}