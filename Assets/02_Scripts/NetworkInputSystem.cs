using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkInputSystem : NetworkBehaviour, INetworkRunnerCallbacks
{
    [Header("Builder")]
    [SerializeField] private LayerMask _environmentalLayer;

    private bool _dashInput = false; // 러너의 대쉬 입력
    private bool _slideInput = false; // 러너의 슬라이드 입력
    private bool _itemInput = false; // 러너 아이템 입력
    private bool _skillInput = false; // 러너 스킬 입력
    private bool _interactInput = false; // 러너 상호작용 입력
    private int _selectedItem = 0; // 러너 아이템
    private bool _mouseButton0 = false; // 마우스 좌클릭
    private bool _mouseButton1 = false; // 마우스 우클릭

    #region MonoBehaviour 메서드

    private void Update()
    {
        _dashInput = _dashInput | Input.GetKey(KeyCode.LeftShift); // 왼쪽 쉬프트를 통해 _dashInput 여부 검사
        _slideInput = _slideInput | Input.GetKeyDown(KeyCode.LeftControl); // 왼쪽 컨트롤을 통해 _slideInput 여부 검사
        _itemInput = _itemInput | Input.GetKeyDown(KeyCode.Q); // Q키를 통해 _itemInput 여부 검사
        _skillInput = _skillInput | Input.GetKeyDown(KeyCode.E); // E키를 통해 _skillInput 여부 검사
        _interactInput = _interactInput | Input.GetKeyDown(KeyCode.F); // F키를 통해 _interactInput 여부 검사
        _mouseButton0 = _mouseButton0 | Input.GetMouseButtonDown(0); // 마우스 좌클릭 여부 검사
        _mouseButton1 = _mouseButton1 | Input.GetMouseButtonDown(1); // 마우스 우클릭 여부 검사
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _selectedItem = 1;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _selectedItem = 2;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            _selectedItem = 3;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            _selectedItem = 4;
        }
    }

    #endregion

    #region NetworkBehaviour virtual 메서드

    public override void Spawned()
    {
        Debug.Log("InputManager 스폰됨");
        Runner.AddCallbacks(this); // 스폰되면 콜백 등록
    }

    #endregion

    #region INetworkRunnerCallbacks 메서드

    // 플레이어의 Input 수집 - 매 네트워크 틱마다 수행
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // 새로운 데이터 struct 생성
        var data = new NetworkInputData();

        // 러너 Input -----------------------------------------------------------------------------

        // 평행 이동 처리
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical);
        data.PlayerRunnerDirection = direction;

        // 대쉬 처리
        data.DashInput.Set(NetworkInputData.DASH_INPUT, _dashInput);
        _dashInput = false;

        data.SlideInput.Set(NetworkInputData.SLIDE_INPUT, _slideInput);
        _slideInput = false;

        // 아이템 사용
        data.ItemInput.Set(NetworkInputData.ITEM_INPUT, _itemInput);
        _itemInput = false;
        data.SelectedItem = _selectedItem;

        // 스킬 사용
        data.SkillInput.Set(NetworkInputData.SKILL_INPUT, _skillInput);
        _skillInput = false;
        data.SelectedSkill = _selectedItem;

        // 타워 상호작용
        data.InteractInput.Set(NetworkInputData.INTERACT_INPUT, _interactInput);
        _interactInput = false;

        // 연구소 상호작용
        data.LaboratoryInput.Set(NetworkInputData.LABORATORY_INPUT, Input.GetKey(KeyCode.Space));

        // 무기 사용
        data.WeaponInput.Set(NetworkInputData.WEAPON_INPUT, Input.GetMouseButtonDown(0));

        // ---------------------------------------------------------------------------------------

        // 빌더 Input -----------------------------------------------------------------------------

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 100f, _environmentalLayer))
        {
            // 마우스 위치
            data.MousePosition = hit.point;
        }

        // 좌클릭 처리
        data.MouseButton0.Set(NetworkInputData.MOUSEBUTTON0, _mouseButton0);
        _mouseButton0 = false;

        // 우클릭 처리
        data.MouseButton1.Set(NetworkInputData.MOUSEBUTTON1, _mouseButton1);
        _mouseButton1 = false;

        // ---------------------------------------------------------------------------------------


        // Input 데이터 전송
        input.Set(data);
    }

    public void OnConnectedToServer(NetworkRunner runner){}
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason){}
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token){}
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data){}
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason){}
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken){}
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input){}
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){}
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){}
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player){}
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player){}
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress){}
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data){}
    public void OnSceneLoadDone(NetworkRunner runner){}
    public void OnSceneLoadStart(NetworkRunner runner){}
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList){}
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason){}
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message){}

    #endregion
}
