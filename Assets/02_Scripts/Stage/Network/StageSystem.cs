using System;
using System.Threading.Tasks;
using Dev.Local;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using KIM.Dev;

namespace Dev.Network
{
    public class StageSystem : System
    {
        [SerializeField] private StageResultView _stageResultView;

        private bool _isGameOverPresented = false;
        private bool _isReturningToTitle = false;

        public void Victory()
        {
            //if (Object.HasStateAuthority)
            //    RPC_ShowStageResult();
            Debug.Log("닿음");

            RPC_ShowVictoryUI();
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_ShowVictoryUI()
        {
            if (InterfaceManager.Instance != null)
            {
                Debug.Log("띄움");
                InterfaceManager.Instance.SetVictoryUIActivation(true);
            }
            else
                Debug.LogError("인터페이스 매니저가 없음");
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_ShowStageResult()
        {
            if (_isGameOverPresented)
                return;

            if (_stageResultView == null)
            {
                Debug.LogError("StageResultView is missing. Cannot show stage result.");
                return;
            }

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

        public void Defeat()
        {
            Debug.Log("Stage defeat requested.");
            RPC_ShowStageResult();
        }
    }
}
