using Dev;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public class NetworkManager : NetworkBehaviour, INetworkRunnerCallbacks
    {
        // 싱글톤 인스턴스
        public static NetworkManager Instance { get; private set; }

        // 플레이어 정보
        public PlayerRegistry Registry { get; private set; }

        // 현재 테스트 모드 여부
        private bool _isTestMode = false;
        private PlayerPosition _testPosition = PlayerPosition.Builder;

        // 다음 스테이지 변수
        private bool _playerReady1 = false;
        private bool _playerReady2 = false;

        public event Action OnLocalPlayerReady;
        public event Action OnNetworkPlayerReady;

        private void Awake()
        {
            Registry = GetComponent<PlayerRegistry>();
        }

        public override void Spawned()
        {
            if (Instance != null)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);

            if (HasStateAuthority)
            {
                Runner.AddCallbacks(this);
            }

            Debug.Log("네트워크 매니저 스폰 완료");
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #region Victory 후처리

        public void RequestNetworkReady()
        {
            if (HasStateAuthority && Runner.IsServer)
            {
                _playerReady1 = !_playerReady1;
                CheckAllPlayerReady();
            }

            OnLocalPlayerReady?.Invoke();
            RPC_RequestNetworkReady();
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = false)]
        private void RPC_RequestNetworkReady()
        {
            if (HasStateAuthority && Runner.IsServer)
            {
                _playerReady2 = !_playerReady2;
                CheckAllPlayerReady();
            }

            OnNetworkPlayerReady?.Invoke();
        }

        // 모든 플레이어가 다음 단계로 갈 준비가 되었음을 확인
        private void CheckAllPlayerReady()
        {
            if (_playerReady1 && _playerReady2)
            {
                RPC_LoadNextScene();
            }
        }

        // 우선은 게임 종료 -> 후에 스테이지 넘어가는 로직으로 변경
        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_LoadNextScene()
        {
            // 게임 종료
            InterfaceManager.Instance.FocusMainMenu();
            MatchMaker.Instance.QuitGame();
        }

        #endregion

        #region 테스트 모드 함수

        // 테스트 모드로 설정
        public void SetTestModeVariable(bool isTestMode, PlayerPosition testPosition)
        {
            _isTestMode = isTestMode;
            _testPosition = testPosition;
        }

        #endregion

        #region INetworkRunner 콜백 구현

        // 플레이어가 접속했을 때 콜백되는 함수
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log("플레이어 접속");

            // 테스트 모드가 아니면 빌더로 추가, 테스트 모드라면 테스트 할 역할군으로 추가
            if (HasStateAuthority && !_isTestMode)
            {
                Registry.AddPlayer(player, PlayerPosition.Builder);
            }
            else if (HasStateAuthority && _isTestMode)
            {
                Registry.AddPlayer(player, _testPosition);
            }
        }

        // 플레이어가 나갔을 때 콜백되는 함수
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log("플레이어 종료");

            // 플레이어를 Registry 딕셔너리에서 제거
            if (HasStateAuthority)
            {
                if (Registry.RefToPosition.ContainsKey(player))
                    Registry.RemovePlayer(player);
            }
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {

        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {

        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {

        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {

        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {

        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {

        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {

        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {

        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {

        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {

        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {

        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {

        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {

        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {

        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {

        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {

        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {

        }
        #endregion
    }

}