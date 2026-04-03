using UnityEngine;
using Fusion;
using Fusion.Addons.FSM;
using System.Collections.Generic;
using Unity.Cinemachine;

public class Player : NetworkBehaviour
{
    [Header("Camera")]
    [SerializeField] private CinemachineCamera _cineCamPrefab;

    protected CinemachineCamera _playerCineCam;

    public override void Spawned()
    {
        InitializeCinemachine();
    }

    private void InitializeCinemachine()
    {
        _playerCineCam = Instantiate(_cineCamPrefab);
        PlayerPosition myPosition = NetworkManager.Instance.Registry.RefToPosition[Runner.LocalPlayer];

        if (this is PlayerRunner)
        {
            // 플레이어 러너 구현
            _playerCineCam.Target.TrackingTarget = this.transform;
            _playerCineCam.Priority = (myPosition == PlayerPosition.Runner) ? 1 : 0;
        }
        else if(this is PlayerBuilder)
        {
            // 플레이어 빌더 구현
            _playerCineCam.Priority = (myPosition == PlayerPosition.Builder) ? 1 : 0;
        }
    }
}