using Fusion;
using UnityEngine;

public class PingSystem : NetworkBehaviour
{
    [SerializeField] private PlayerPing _playerPingPrefab;
    [SerializeField] private LayerMask _groundLayer;

    // 러너의 핑 가이드 컴포넌트 참조
    private PlayerRunnerPingGuide _playerRunnerPingGuide;

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if(Physics.Raycast(ray, out hit, 5000f, _groundLayer))
                {
                    RPC_SendPing(hit.point);
                }
            }
        }
    }

    // 핑 시스템 초기화
    public void InitializePingSystem(PlayerRunnerPingGuide runnerPingGuide)
    {
        // 러너의 핑 가이드 캐싱
        _playerRunnerPingGuide = runnerPingGuide;
    }

    // 핑 전송
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SendPing(Vector3 pingPos)
    {
        // 핑 초기화
        PlayerPing ping = Instantiate(_playerPingPrefab, pingPos, Quaternion.identity);
        ping.InitializePlayerPing(_playerRunnerPingGuide);
    }
}