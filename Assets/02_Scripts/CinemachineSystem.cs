using Unity.Cinemachine;
using UnityEngine;
using KIM.Dev;

public class CinemachineSystem : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _playerRunnerCamera;
    [SerializeField] private CinemachineCamera _playerBuilderCamera;

    private PlayerPosition _localPlayerPosition;
    private bool _isInitialized;
    private bool _isLookingAtRunner;

    public void Initialize(PlayerPosition myPosition, PlayerRunner runner, PlayerBuilder builder)
    {
        _localPlayerPosition = myPosition;

        if (_playerRunnerCamera == null || _playerBuilderCamera == null)
        {
            Debug.LogError("CinemachineSystem requires scene camera references.", this);
            return;
        }

        if (runner == null || builder == null)
        {
            Debug.LogError("CinemachineSystem requires player references.", this);
            return;
        }

        SetPriority(myPosition);
        SetTrackingTarget(runner);
        builder.InitializeCinemachineCamera(_playerBuilderCamera);
        _isLookingAtRunner = myPosition == PlayerPosition.Runner;
        _isInitialized = true;
    }

    // 빌더의 러너 카메라 전환 입력 처리
    private void Update()
    {
        if (!_isInitialized || _localPlayerPosition != PlayerPosition.Builder)
            return;

        bool isLookingAtRunner = Input.GetKey(KeyCode.Space);
        if (_isLookingAtRunner == isLookingAtRunner)
            return;

        _isLookingAtRunner = isLookingAtRunner;
        SetPriority(isLookingAtRunner ? PlayerPosition.Runner : PlayerPosition.Builder);
    }

    // 내 역할군에 따라 시네머신 우선순위 결정
    private void SetPriority(PlayerPosition myPosition)
    {
        switch (myPosition)
        {
            case PlayerPosition.Runner:
                _playerRunnerCamera.Priority = 1;
                _playerBuilderCamera.Priority = 0;
                break;
            case PlayerPosition.Builder:
                _playerRunnerCamera.Priority = 0;
                _playerBuilderCamera.Priority = 1;
                break;
        }
    }

    // 시네머신의 타겟을 설정
    public void SetTrackingTarget(Transform transform)
    {
        if (_playerRunnerCamera != null)
            _playerRunnerCamera.Target.TrackingTarget = transform;
    }
    private void SetTrackingTarget(PlayerRunner runner)
    {
        SetTrackingTarget(runner.transform);
    }
}
