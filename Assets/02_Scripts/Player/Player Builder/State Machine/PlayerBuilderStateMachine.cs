using UnityEngine;

namespace KIM.Dev
{
    public class PlayerBuilderStateMachine
    {
        private IPlayerState _currentState;
        public IPlayerState CurrentState => _currentState;

        public PlayerBuilderOriginState OriginState;
        public PlayerBuilderDragState DragState;
        public PlayerBuilderTowerBuildState TowerBuildState;
        public PlayerBuilderLaboratoryState LaboratoryState;
        public PlayerBuilderTowerSelectState TowerSelectState;
        public PlayerBuilderTowerMoveState TowerMoveState;

        public PlayerBuilderStateMachine(PlayerBuilder player)
        {
            OriginState = new PlayerBuilderOriginState(player);
            DragState = new PlayerBuilderDragState(player);
            TowerBuildState = new PlayerBuilderTowerBuildState(player);
            LaboratoryState = new PlayerBuilderLaboratoryState(player);
            TowerSelectState = new PlayerBuilderTowerSelectState(player);
            TowerMoveState = new PlayerBuilderTowerMoveState(player);
        }

        public void InitStateMachine()
        {
            _currentState = OriginState;
            _currentState.Enter();
        }

        public void Update()
        {
            _currentState.Update();
        }

        public void LateUpdate()
        {
            _currentState.LateUpdate();
        }

        public void TransitionToState(IPlayerState next)
        {
            if (next == null || ReferenceEquals(_currentState, next))
                return;

            _currentState.Exit();
            _currentState = next;
            _currentState.Enter();
        }
    }
}