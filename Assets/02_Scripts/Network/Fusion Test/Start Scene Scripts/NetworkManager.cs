using Dev;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    // 싱글톤 인스턴스
    public static NetworkManager Instance { get; private set; }

    // 플레이어 정보
    public PlayerRegistry Registry { get; private set; }

    private HostMigration _hostMigration;

    // 현재 테스트 모드 여부
    private bool _isTestMode = false;
    private PlayerPosition _testPosition = PlayerPosition.Builder;

    private void Awake()
    {
        Registry = GetComponent<PlayerRegistry>();
        transform.Find("Host Migration").TryGetComponent(out _hostMigration);
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
        else
        {
            Runner.AddCallbacks(_hostMigration);
            Debug.Log($"콜백 등록 {_hostMigration}");
        }
        Debug.Log("네트워크 매니저 스폰 완료");
    }

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
            if(Registry.RefToPosition.ContainsKey(player))
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
