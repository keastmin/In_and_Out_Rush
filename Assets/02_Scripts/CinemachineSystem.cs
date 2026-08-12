using Unity.Cinemachine;
using UnityEngine;
using KIM.Dev;

public class CinemachineSystem : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _playerRunnerCamera;
    [SerializeField] private CinemachineCamera _playerBuilderCamera;

    private PlayerRunnerCinemachineController _runnerController;
    private PlayerBuilderCinemachineController _builderController;
    private Transform _laboratoryTarget;
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

        if (!_playerRunnerCamera.TryGetComponent(out _runnerController) ||
            !_playerBuilderCamera.TryGetComponent(out _builderController))
        {
            Debug.LogError("CinemachineSystem requires role camera controllers.", this);
            return;
        }

        _runnerController.Initialize(runner.transform);
        _builderController.Initialize(myPosition == PlayerPosition.Builder);
        _builderController.SetLaboratoryTarget(_laboratoryTarget);
        builder.InitializeCinemachineController(_builderController);

        SetPriority(myPosition);
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

        SetRunnerObservation(isLookingAtRunner);
    }

    private void SetRunnerObservation(bool isActive)
    {
        _isLookingAtRunner = isActive;
        if (isActive)
        {
            _runnerController.ApplyObservationView(_builderController.CineCamera);
            _builderController.SetViewActive(false);
            SetPriority(PlayerPosition.Runner);
        }
        else
        {
            _builderController.AdoptView(_playerRunnerCamera);
            SetPriority(PlayerPosition.Builder);
            _builderController.SetViewActive(true);
        }
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

    public void SetLaboratoryTarget(Transform laboratoryTarget)
    {
        _laboratoryTarget = laboratoryTarget;
        _builderController?.SetLaboratoryTarget(laboratoryTarget);
    }
}
