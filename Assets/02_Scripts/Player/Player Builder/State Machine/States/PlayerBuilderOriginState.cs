using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerBuilderOriginState : IPlayerState
{
    private PlayerBuilder _player;

    public PlayerBuilderOriginState(PlayerBuilder player)
    {
        _player = player;
    }

    public void Enter()
    {
        Debug.Log("Origin State");
    }

    public void Update()
    {
        if (_player.HasInputAuthority)
        {
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                _player.SetClickValue(true);
                _player.ClickLeftMouseDownOnWorld();
            }
            if (_player.IsClick && Input.GetMouseButtonUp(0))
            {
                _player.SetClickValue(false);
                _player.ClickLeftMouseUpOnWorld();
            }

            // 클릭 시작 설정
            if (_player.IsClick)
            {
                _player.SetCurrentMousePoint(Input.mousePosition);
            }
        }
        TransitionTo();
    }

    public void LateUpdate()
    {
        // 마우스를 통한 화면 이동
        _player.BuilderCamMove();
    }

    public void Exit()
    {

    }

    private void TransitionTo()
    {
        if (_player.IsOpeningLaboratory)
        {
            _player.StateMachine.TransitionToState(_player.StateMachine.LaboratoryState);
        }
        else if (_player.BuilderTowerBuild.IsStandByBuild)
        {
            _player.StateMachine.TransitionToState(_player.StateMachine.TowerBuildState);
        }
        else if (_player.SelectedTowersCount > 0) // 선택된 타워가 있을 경우
        {
            _player.StateMachine.TransitionToState(_player.StateMachine.TowerSelectState);
        }
        else if(_player.IsClick && (Vector2.Distance(_player.StartMousePoint, _player.CurrentMousePoint) >= _player.DragThresholdPixel))
        {
            _player.StateMachine.TransitionToState(_player.StateMachine.DragState);
        }
    }
}
