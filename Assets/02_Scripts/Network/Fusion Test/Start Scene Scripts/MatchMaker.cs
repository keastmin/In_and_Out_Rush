using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class MatchMaker : MonoBehaviour, INetworkRunnerCallbacks
{
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

    private void Awake()
    {
        // 싱글턴: 이미 인스턴스가 존재하면 현재 게임 오브젝트 파괴하고 아니라면 인스턴스 설정
        if(Instance != null)
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
        // 러너가 없을 때 러너를 생성
        if(!Runner)
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

        // 룸 생성에 성공하면 룸 생성 이벤트 호출
        if (result.Ok)
        {
            OnRoomCreated?.Invoke();
        }
    }

    // 클라이언트가 룸에 참여하는 메서드
    public async void JoinRoom()
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
            SessionName = InterfaceManager.Instance.JoinSession.RoomCode, // 참여할 룸 코드를 받아옴
            SceneManager = Runner.GetComponent<NetworkSceneManagerDefault>(), // 기본 씬 매니저 사용
            EnableClientSessionCreation = false // 클라이언트 세션 생성 비활성화
        });

        // 룸 참여에 성공하면 룸 참여 이벤트 호출
        if (result.Ok)
        {
            Debug.Log("방 참여 성공");
            OnRoomJoined?.Invoke();
        }
    }

    // 룸을 떠나는 메서드
    public async void LeaveRoom()
    {
        // 러너가 있다면 종료 작업 수행
        if (Runner)
        {
            Debug.Log("LeaveRoom Shutdown 진행");
            await Runner.Shutdown();
            OnRoomLeaved?.Invoke();
        }
    }

    // 룸 코드를 랜덤으로 생성하는 메서드
    private string GenerateRoomCode()
    {
        string chars = "";

        for(int i = 0; i < _roomCodeLength; i++)
        {
            int randomIndex = UnityEngine.Random.Range((int)'A', (int)'Z');
            chars += (char)randomIndex;
        }

        return chars;
    }

    // 대기실에서 게임 시작 버튼 클릭 시 호출되는 메서드
    public async void OnClickStartButton()
    {
        var sceneRef = SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath("Assets/01_Scenes/NetworkGameScene.unity"));
        await Runner.LoadScene(sceneRef, LoadSceneMode.Single);
    }

    #region INetworkRunnerCallbacks

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Runner = null;
        if (shutdownReason == ShutdownReason.HostMigration)
        {
            Debug.Log("호스트 마이그레이션으로 인한 종료");
            return;
        }
        else
            OnRoomLeaved?.Invoke();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        OnStartGame?.Invoke();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {}

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason){}

    public void OnConnectedToServer(NetworkRunner runner){}

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason){}

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token){}

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data){}

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken){}

    public void OnInput(NetworkRunner runner, NetworkInput input){}

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input){}

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){}

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){}

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress){}

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data){}

    public void OnSceneLoadStart(NetworkRunner runner){}

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList){}

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message){}

    #endregion
}
