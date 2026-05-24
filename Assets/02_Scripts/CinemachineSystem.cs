using Unity.Cinemachine;
using UnityEngine;
using KIM.Dev;

public class CinemachineSystem : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _playerRunnerCamera;
    [SerializeField] private CinemachineCamera _playerBuilderCamera;

    private CinemachineCamera _runnerCamera;
    private CinemachineCamera _builderCamera;

    public void InitCinemachineCamera(PlayerPosition myPosition, PlayerRunner runner, PlayerBuilder builder)
    {
        InstantiateCinemachineCamera();
        SetPriority(myPosition);
        SetTrackingTarget(runner);
    }

    // 시네머신 생성
    private void InstantiateCinemachineCamera()
    {
        _runnerCamera = Instantiate(_playerRunnerCamera);
        _builderCamera = Instantiate(_playerBuilderCamera);
    }

    // 내 역할군에 따라 시네머신 우선순위 결정
    private void SetPriority(PlayerPosition myPosition)
    {
        switch (myPosition)
        {
            case PlayerPosition.Runner:
                _runnerCamera.Priority = 1;
                _builderCamera.Priority = 0;
                break;
            case PlayerPosition.Builder:
                _runnerCamera.Priority = 0;
                _builderCamera.Priority = 1;
                break;
        }
    }

    // 시네머신의 타겟을 설정
    public void SetTrackingTarget(Transform transform)
    {
        _runnerCamera.Target.TrackingTarget = transform;
    }
    private void SetTrackingTarget(PlayerRunner runner)
    {
        _runnerCamera.Target.TrackingTarget = runner.transform;
    }
}
