using Grid;
using UnityEngine;

public class PlayerBuilderTowerMoveState : IPlayerState
{
    private PlayerBuilder _player;

    public PlayerBuilderTowerMoveState(PlayerBuilder player)
    {
        _player = player;
    }

    public void Enter()
    {
        // 그리드 오버레이 활성화
        GridManager.Instance.SetCellStateOverlayEnabled(true);
        GridManager.Instance.ClearBuildRangePreview();

        _player.BuilderUI.ActivationTowerBuildUI(true, "Left Mouse: Complete, RightMouse: Cancel");
        _player.BuilderTowerMove.TowerMoveSet(_player.SelectedTowers);
    }

    public void Update()
    {
        Vector3 mouseWorldPos = GetMouseWorldPos();
        bool canMove = _player.BuilderTowerMove.TowerGhostSnapShot(mouseWorldPos);
        bool moveComplete = false;
        
        if(canMove && Input.GetMouseButtonDown(0))
        {
            moveComplete = true;
            _player.BuilderTowerMove.TowerMove();
        }

        TransitionTo(moveComplete);
    }

    public void LateUpdate()
    {
        
    }

    public void Exit()
    {
        // 그리드 오버레이 비활성화
        GridManager.Instance.ClearBuildRangePreview();
        GridManager.Instance.SetCellStateOverlayEnabled(false);

        _player.BuilderTowerMove.TowerMoveClear();
        _player.BuilderUI.ActivationTowerBuildUI(false);
    }

    private void TransitionTo(bool moveComplete)
    {
        if (Input.GetMouseButtonDown(1))
        {
            _player.StateMachine.TransitionToState(_player.StateMachine.TowerSelectState);
        }
        else if(Input.GetMouseButtonDown(0) && moveComplete)
        {
            _player.StateMachine.TransitionToState(_player.StateMachine.TowerSelectState);
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 pos = Vector3.zero;
        float height = GridManager.Instance.GridHeight;
        Plane plane = new Plane(Vector3.up, height);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out float enter))
        {
            pos = ray.GetPoint(enter);
        }
        return pos;
    }
}
