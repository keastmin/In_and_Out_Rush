using UnityEngine;
using UnityEngine.EventSystems;

namespace KIM.Dev
{
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

            TowerType type = GetSelectedTowerType();

            _player.BuilderUI.ActivationTowerSelectUI(
                true,
                type,
                _player.GetSelectedTowerCapabilities(),
                _player.BuilderTowerMove.CalculateMoveCost(_player.SelectedTowers));
        }

        public void Update()
        {
            _player.BuilderUI.RefreshTowerSelectActions(
                GetSelectedTowerType(),
                _player.GetSelectedTowerCapabilities(),
                _player.BuilderTowerMove.CalculateMoveCost(_player.SelectedTowers));

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

        private TowerType GetSelectedTowerType()
        {
            TowerType type = TowerType.Attack;
            foreach (Tower tower in _player.SelectedTowers)
            {
                if (tower == null)
                    continue;

                if (tower.Type == TowerType.Center || tower.Type == TowerType.Support)
                {
                    type = tower.Type;
                    break;
                }
            }

            return type;
        }
    }
}
