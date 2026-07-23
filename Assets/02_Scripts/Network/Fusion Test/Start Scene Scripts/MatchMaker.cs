using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KIM.Dev
{
    public class MatchMaker : MonoBehaviour, INetworkRunnerCallbacks
    {
        private enum LocalShutdownIntent
        {
            None,
            LeaveRoom,
            QuitGame
        }

        private const string HostLeftLobbyMessage = "Host left lobby";
        private const string HostDisconnectedMessage = "Disconnected from host";
        private const string TeammateQuitGameMessage = "Teammate quit game";
        private const string TeammateDisconnectedMessage = "Disconnected from teammate";
        private const string CreateRoomFailedMessage = "Failed to create session. Please try again.";
        private const string JoinRoomFailedMessage = "Failed to join session. Please check the room code and try again.";
        private const string LobbySceneName = "LobbyScene";
        private const string GameSceneName = "GameScene";

        public static MatchMaker Instance { get; private set; } // 싱글턴 인스턴스

        [Header("Prefab")]
        public NetworkRunner RunnerPrefab; // 러너 프리팹
        public NetworkObject NetworkManagerPrefab; // 네트워크 매니저 프리팹

        public NetworkRunner Runner { get; private set; } // 현재 인스턴스의 러너

        public UnityEvent OnRoomCreated; // 룸을 생성했을 때 호출되는 이벤트
        public UnityEvent OnRoomJoined; // 룸에 참여했을 때 호출되는 이벤트
        public UnityEvent OnRoomLeaved; // 룸을 떠났을 때 호출되는 이벤트
        public UnityEvent OnStartGame; // 게임을 시작했을 때 호출되는 이벤트

        private int _maxPlayerCount = 2; // 최대 플레이어 수

        private int _roomCodeLength = 6; // 룸 코드 길이
        public int RoomCodeLength => _roomCodeLength; // 룸 코드 길이 프로퍼티

        private LocalShutdownIntent _localShutdownIntent = LocalShutdownIntent.None;
        private readonly HashSet<PlayerRef> _intentionalGameLeavers = new();
        private bool _isShutdownInProgress;
        private bool _hostShutdownWasIntentional;
        private bool _hostShutdownWasGame;
        private bool _quitApplicationAfterShutdown;
        private string _pendingRemoteShutdownMessage;

        private void Awake()
        {
            // 싱글턴: 이미 인스턴스가 존재하면 현재 게임 오브젝트 파괴하고 아니라면 인스턴스 설정
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
        }

        // 호스트가 룸을 생성하는 메서드
        public async void CreateRoom()
        {
            InterfaceManager interfaceManager = InterfaceManager.Instance;
            if (interfaceManager == null || !interfaceManager.TryBeginSessionOperation())
                return;

            bool roomCreated = false;
            string failureDetail = null;

            try
            {
                // 러너가 없을 때 러너를 생성
                if (!Runner)
                {
                    // 러너 프리팹 생성
                    Runner = Instantiate(RunnerPrefab);

                    // 러너의 NetworkEvents 컴포넌트를 찾아 PlayerJoined에 이벤트 리스너 추가
                    Runner.GetComponent<NetworkEvents>().PlayerJoined.AddListener((runner, player) =>
                    {
                        if (runner.IsServer && runner.LocalPlayer == player)
                        {
                            Debug.Log("네트워크 매니저 스폰");
                            runner.Spawn(NetworkManagerPrefab);
                        }
                    });
                }

                // 러너에 콜백 추가
                Runner.AddCallbacks(this);

                // 입력을 제공하도록 설정
                Runner.ProvideInput = true;

                // 게임 시작 인자 설정
                var result = await Runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Host, // 호스트 모드로 시작
                    PlayerCount = _maxPlayerCount, // 최대 2명으로 설정
                    SessionName = GenerateRoomCode(), // 룸 코드 랜덤 생성
                    SceneManager = Runner.GetComponent<NetworkSceneManagerDefault>() // 기본 씬 매니저 사용
                });

                roomCreated = result.Ok;
                if (!result.Ok)
                    failureDetail = result.ShutdownReason.ToString();
            }
            catch (Exception exception)
            {
                failureDetail = exception.Message;
                Debug.LogException(exception);
            }
            finally
            {
                interfaceManager.EndSessionOperation();
            }

            if (roomCreated)
            {
                OnRoomCreated?.Invoke();
                return;
            }

            Debug.LogWarning($"{CreateRoomFailedMessage} {failureDetail}");
            interfaceManager.PopupNotificationWindow(CreateRoomFailedMessage);
        }

        // 클라이언트가 룸에 참여하는 메서드
        public async void JoinRoom()
        {
            InterfaceManager interfaceManager = InterfaceManager.Instance;
            if (interfaceManager == null || !interfaceManager.TryBeginSessionOperation())
                return;

            bool roomJoined = false;
            string failureDetail = null;

            try
            {
                // 러너가 없을 때 러너를 생성
                if (!Runner)
                {
                    // 러너 프리팹 생성
                    Runner = Instantiate(RunnerPrefab);
                }

                // 러너에 콜백 추가
                Runner.AddCallbacks(this);

                // 입력을 제공하도록 설정
                Runner.ProvideInput = true;

                // 게임 시작 인자 설정
                var result = await Runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Client, // 클라이언트 모드로 시작
                    SessionName = interfaceManager.JoinSession.RoomCode, // 참여할 룸 코드를 받아옴
                    SceneManager = Runner.GetComponent<NetworkSceneManagerDefault>(), // 기본 씬 매니저 사용
                    EnableClientSessionCreation = false // 클라이언트 세션 생성 비활성화
                });

                roomJoined = result.Ok;
                if (!result.Ok)
                    failureDetail = result.ShutdownReason.ToString();
            }
            catch (Exception exception)
            {
                failureDetail = exception.Message;
                Debug.LogException(exception);
            }
            finally
            {
                interfaceManager.EndSessionOperation();
            }

            if (roomJoined)
            {
                Debug.Log("방 참여 성공");
                OnRoomJoined?.Invoke();
                return;
            }

            Debug.LogWarning($"{JoinRoomFailedMessage} {failureDetail}");
            interfaceManager.PopupNotificationWindow(JoinRoomFailedMessage);
        }

        // 룸을 떠나는 메서드
        public async void LeaveRoom()
        {
            await ShutdownRunnerAsync(LocalShutdownIntent.LeaveRoom);
        }

        // 룸 코드를 랜덤으로 생성하는 메서드
        private string GenerateRoomCode()
        {
            string chars = "";

            for (int i = 0; i < _roomCodeLength; i++)
            {
                int randomIndex = UnityEngine.Random.Range((int)'A', (int)'Z');
                chars += (char)randomIndex;
            }

            return chars;
        }

        // 대기실에서 게임 시작 버튼 클릭 시 호출되는 메서드
        public async void OnClickStartButton()
        {
            var sceneRef = SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath("Assets/01_Scenes/GameScene.unity"));
            await Runner.LoadScene(sceneRef, LoadSceneMode.Single);
        }

        public async void QuitGame()
        {
            await ShutdownRunnerAsync(LocalShutdownIntent.QuitGame);
        }

        public async void QuitApplication()
        {
            _quitApplicationAfterShutdown = true;

            if (!Runner)
            {
                QuitApplicationImmediately();
                return;
            }

            await ShutdownRunnerAsync(LocalShutdownIntent.QuitGame);
        }

        public void MarkPlayerGameShutdownIntent(PlayerRef player)
        {
            if (player != PlayerRef.None)
                _intentionalGameLeavers.Add(player);
        }

        public void MarkHostShutdownIntent(bool wasGame)
        {
            _hostShutdownWasIntentional = true;
            _hostShutdownWasGame = wasGame;
        }

        public void HandleHostShutdownNotice(bool wasGame)
        {
            MarkHostShutdownIntent(wasGame);

            if (!Runner || Runner.IsServer || _localShutdownIntent != LocalShutdownIntent.None || _isShutdownInProgress)
                return;

            _pendingRemoteShutdownMessage = wasGame ? TeammateQuitGameMessage : HostLeftLobbyMessage;
            _isShutdownInProgress = true;
            _ = Runner.Shutdown();
        }

        private async Task ShutdownRunnerAsync(LocalShutdownIntent intent)
        {
            if (!Runner || _isShutdownInProgress)
                return;

            _isShutdownInProgress = true;
            _localShutdownIntent = intent;

            bool wasGame = IsInGame();
            NotifyShutdownIntent(wasGame);
            await Task.Delay(100);

            if (Runner)
            {
                Debug.Log(intent == LocalShutdownIntent.QuitGame ? "Game Shutdown" : "Leave Room Shutdown");
                await Runner.Shutdown();
            }
        }

        private void NotifyShutdownIntent(bool wasGame)
        {
            if (!Runner || NetworkManager.Instance == null || NetworkManager.Instance.Registry == null)
                return;

            if (Runner.IsServer)
            {
                NetworkManager.Instance.Registry.RPC_NotifyHostShutdown(wasGame);
            }
            else if (wasGame)
            {
                NetworkManager.Instance.Registry.RPC_NotifyPlayerGameShutdown(Runner.LocalPlayer);
            }
        }

        private bool IsInGame()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.name == LobbySceneName)
                return false;
            if (currentScene.name == GameSceneName)
                return true;

            return GameManager.Instance != null && GameManager.Instance.CurrentGameState == GameState.Game;
        }

        private void ShutdownBecauseRemotePlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (_isShutdownInProgress)
                return;

            _isShutdownInProgress = true;
            bool intentionalLeave = _intentionalGameLeavers.Contains(player);
            _pendingRemoteShutdownMessage = intentionalLeave ? TeammateQuitGameMessage : TeammateDisconnectedMessage;
            _ = runner.Shutdown();
        }

        #region Victory



        #endregion

        #region INetworkRunnerCallbacks

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log("Shutdown");

            bool isLocalShutdown = _localShutdownIntent != LocalShutdownIntent.None;
            string remoteShutdownMessage = isLocalShutdown ? null : _pendingRemoteShutdownMessage;

            // 씬 이동
            Scene currentScene = SceneManager.GetActiveScene();
            string moveSceneName = LobbySceneName;
            if (currentScene.name != moveSceneName)
                SceneManager.LoadScene(moveSceneName);

            Runner = null;
            if (GameManager.Instance != null)
                GameManager.Instance.SetGameMode(GameState.Lobby);
            if (InterfaceManager.Instance != null)
                InterfaceManager.Instance.CloseInGameSettingUI();
            OnRoomLeaved?.Invoke();

            if (!string.IsNullOrEmpty(remoteShutdownMessage) && InterfaceManager.Instance != null)
            {
                InterfaceManager.Instance.FocusMainMenu();
                InterfaceManager.Instance.PopupNotificationWindow(remoteShutdownMessage);
            }

            _localShutdownIntent = LocalShutdownIntent.None;
            _intentionalGameLeavers.Clear();
            _isShutdownInProgress = false;
            _hostShutdownWasIntentional = false;
            _hostShutdownWasGame = false;
            _pendingRemoteShutdownMessage = null;

            if (_quitApplicationAfterShutdown)
            {
                _quitApplicationAfterShutdown = false;
                QuitApplicationImmediately();
            }
        }

        private void QuitApplicationImmediately()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            if (GameManager.Instance != null && SceneManager.GetActiveScene().name == GameSceneName)
            {
                GameManager.Instance.SetGameMode(GameState.Game);
                OnStartGame?.Invoke();
            }
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer && player != runner.LocalPlayer && IsInGame())
                ShutdownBecauseRemotePlayerLeft(runner, player);
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.Log("Disconnect");

            if (_localShutdownIntent != LocalShutdownIntent.None || _isShutdownInProgress)
                return;

            bool wasGame = _hostShutdownWasIntentional ? _hostShutdownWasGame : IsInGame();
            _pendingRemoteShutdownMessage = wasGame
                ? (_hostShutdownWasIntentional ? TeammateQuitGameMessage : TeammateDisconnectedMessage)
                : (_hostShutdownWasIntentional ? HostLeftLobbyMessage : HostDisconnectedMessage);

            _isShutdownInProgress = true;
            _ = runner.Shutdown();
        }

        public void OnConnectedToServer(NetworkRunner runner) { }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

        public void OnSceneLoadStart(NetworkRunner runner) { }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

        #endregion
    }

}
