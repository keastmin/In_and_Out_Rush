using Unity.VisualScripting;
using UnityEngine;

public class PlayerBuilderLaboratoryState : IPlayerState
{
    private PlayerBuilder _player;

    public PlayerBuilderLaboratoryState(PlayerBuilder player)
    {
        _player = player;
    }

    public void Enter() 
    {
        Debug.Log("Laboratory State");

        _player.BuilderUI.ActivationLaboratoryUI(true);
    }

    public void Update() 
    {
        if (Input.GetMouseButtonDown(1))
            _player.IsOpenLaboratory(false);

        _player.CamMover.Move();
        TransitionTo();
    }

    public void LateUpdate()
    {

    }

    public void Exit() 
    {
        _player.BuilderUI.ActivationLaboratoryUI(false);
    }

    private void TransitionTo()
    {
        if(!_player.IsOpeningLaboratory)
        {
            _player.StateMachine.TransitionToState(_player.StateMachine.OriginState);
        }
    }
}
