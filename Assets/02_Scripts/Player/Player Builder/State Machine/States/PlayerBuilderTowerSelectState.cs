using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerBuilderTowerSelectState : IPlayerState
{
    private PlayerBuilder _player;

    public PlayerBuilderTowerSelectState(PlayerBuilder player)
    {
        _player = player;
    }

    public void Enter()
    {
        Debug.Log("Tower Select 상태 진입");

        // 선택된 타워 타입 정하기
        TowerType type = TowerType.Attack;
        foreach(var tower in _player.SelectedTowers)
        {
            if(tower.Type == TowerType.Center || tower.Type == TowerType.Support)
            {
                type = tower.Type;
                break;
            }
        }
        
        _player.BuilderUI.ActivationTowerSelectUI(true, type);
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            _player.ClickLeftMouseDownOnWorld();
            _player.SetClickValue(true);
        }

        TransitionTo();
    }

    public void LateUpdate()
    {

    }

    public void Exit()
    {
        _player.BuilderUI.ActivationTowerSelectUI(false);
    }

    private void TransitionTo()
    {
        if (_player.SelectedTowersCount <= 0)
        {
            _player.StateMachine.TransitionToState(_player.StateMachine.OriginState);
        }
    }
}
