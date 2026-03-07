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
        }

        private bool IsHostInitialized()
            => true;

        private void CreateObjects() {}

        private void InitializeObjects()
        {
            _resourceSpawnSystem.SetUp();
            _stageResultView.Hide();
        }

        private void BindObjects()
        {
            Gate.OnGateEntered += HandleGateEntered;
        }

        private void HandleGateEntered(Collider other, Gate gate, object sender)
        {
            if (Object.HasStateAuthority)
                RPC_ShowStageResult();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_ShowStageResult()
        {
            if (_isGameOverPresented)
                return;

            _isGameOverPresented = true;
            _stageResultView.ClearNextButtonListener();
            _stageResultView.OnNextButtonClicked += HandleNextButtonClicked;
            _stageResultView.Show();
        }

        private void HandleNextButtonClicked()
        {
            if (_isReturningToTitle)
                return;

            _ = ShutdownAndReturnToTitleSceneAsync();
        }

        private async Task ShutdownAndReturnToTitleSceneAsync()
        {
            _isReturningToTitle = true;

            _stageResultView.OnNextButtonClicked -= HandleNextButtonClicked;
            _stageResultView.Hide();

            var runner = Runner;
            if (runner != null && runner.IsRunning)
            {
                try
                {
                    var shutdownTask = runner.Shutdown();
                    var completedTask = await Task.WhenAny(shutdownTask, Task.Delay(TimeSpan.FromSeconds(10)));

                    if (completedTask == shutdownTask)
                    {
                        await shutdownTask;
                    }
                    else
                    {
                        Debug.LogWarning("Runner shutdown 대기 시간이 초과되었습니다. 타이틀 씬으로 이동합니다.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Runner shutdown failed: {ex}");
                }
            }

            SceneManager.LoadScene(0);
        }

        private void SetUpObjects()
        {
            // TODO: Field Object 생성
        }
    }
}