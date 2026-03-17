using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerBuilderOriginState : IPlayerState
{
    private readonly PlayerBuilder _player;

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
        if (_player != null && _player.HasInputAuthority)
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

            if (_player.IsClick)
            {
                _player.SetCurrentMousePoint(Input.mousePosition);
            }
        }

        TransitionTo();
    }

    public void LateUpdate()
    {
        if (_player == null) return;
        _player.BuilderCamMove();
    }

    public void Exit()
    {
    }

    private void TransitionTo()
    {
        if (_player == null || _player.StateMachine == null)
        {
            return;
        }

        if (_player.IsOpeningLaboratory)
        {
            _player.StateMachine.TransitionToState(_player.StateMachine.LaboratoryState);
        }
        else if (_player.BuilderTowerBuild != null && _player.BuilderTowerBuild.IsStandByBuild)
        {
            _player.StateMachine.TransitionToState(_player.StateMachine.TowerBuildState);
        }
        else if (_player.SelectedTowersCount > 0)
        {
            _player.StateMachine.TransitionToState(_player.StateMachine.TowerSelectState);
        }
        else if (_player.IsClick && (Vector2.Distance(_player.StartMousePoint, _player.CurrentMousePoint) >= _player.DragThresholdPixel))
        {
            _player.StateMachine.TransitionToState(_player.StateMachine.DragState);
        }
    }
}
