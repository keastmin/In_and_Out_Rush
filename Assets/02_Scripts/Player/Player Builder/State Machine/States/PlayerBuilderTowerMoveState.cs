using UnityEngine;

namespace KIM.Dev
{
    public class PlayerBuilderTowerMoveState : IPlayerState
    {
        private PlayerBuilder _player;

        public PlayerBuilderTowerMoveState(PlayerBuilder player)
        {
            _player = player;
        }

        public void Enter()
        {
            InfiniteGrid.Instance.SetCellStateOverlayEnabled(true);
            InfiniteGrid.Instance.ClearBuildRangePreview();
            InfiniteGrid.Instance.ClearBuffPreviewSources();

            _player.BuilderUI.ActivationTowerBuildUI(true, "Left Mouse: Complete, RightMouse: Cancel");
            _player.BuilderTowerMove.TowerMoveSet(_player.SelectedTowers);
        }

        public void Update()
        {
            if (_player.SelectedTowersCount <= 0 || !_player.BuilderTowerMove.HasMoveTargets)
            {
                _player.StateMachine.TransitionToState(_player.StateMachine.OriginState);
                return;
            }

            Vector3 mouseWorldPos = GetMouseWorldPos();
            bool canMove = _player.BuilderTowerMove.TowerGhostSnapShot(mouseWorldPos);
            bool moveComplete = false;

            if (canMove && Input.GetMouseButtonDown(0))
            {
                moveComplete = true;
                _player.BuilderTowerMove.TowerMove();
            }

            TransitionTo(moveComplete);
        }

        public void LateUpdate()
        {
            _player.CamMover.Move();
        }

        public void Exit()
        {
            InfiniteGrid.Instance.ClearBuildRangePreview();
            InfiniteGrid.Instance.SetCellStateOverlayEnabled(false);

            _player.BuilderTowerMove.TowerMoveClear();
            _player.BuilderUI.ActivationTowerBuildUI(false);
        }

        private void TransitionTo(bool moveComplete)
        {
            if (Input.GetMouseButtonDown(1))
            {
                _player.StateMachine.TransitionToState(_player.StateMachine.TowerSelectState);
            }
            else if (Input.GetMouseButtonDown(0) && moveComplete)
            {
                _player.StateMachine.TransitionToState(_player.StateMachine.TowerSelectState);
            }
        }

        private Vector3 GetMouseWorldPos()
        {
            Vector3 pos = Vector3.zero;
            if (InfiniteGrid.Instance == null || Camera.main == null)
                return pos;

            float height = InfiniteGrid.Instance.GridHeight;
            Plane plane = new Plane(Vector3.up, height);

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float enter))
            {
                pos = ray.GetPoint(enter);
            }

            return pos;
        }
    }
}