using UnityEngine;

namespace KIM.Dev
{
    public class PlayerBuilderDragState : IPlayerState
    {
        PlayerBuilder _player;

        public PlayerBuilderDragState(PlayerBuilder player)
        {
            _player = player;
        }

        public void Enter()
        {
            Debug.Log("Drag State 진입");
            _player.DragStart();

            // 클릭 오브젝트가 있다면 취소 처리
            _player.ClickObjectClear();
        }

        public void Update()
        {
            _player.SetCurrentMousePoint(Input.mousePosition);
            _player.Dragging();

            CollectAttackTowerInDragRange();

            if (Input.GetMouseButtonUp(0))
            {
                _player.SetClickValue(false);
            }

            TransitionTo();
        }

        public void LateUpdate()
        {

        }

        public void Exit()
        {
            CompleteDrag();
            _player.DragEnd();
        }

        private void TransitionTo()
        {
            if (!_player.IsClick)
            {
                _player.StateMachine.TransitionToState(_player.StateMachine.OriginState);
            }
        }

        // 드래그 범위 안에 있는 공격 타워 수집
        private void CollectAttackTowerInDragRange()
        {
            Vector2 startPos = _player.StartMousePoint;
            Vector2 endPos = _player.CurrentMousePoint;
            float minX = Mathf.Min(startPos.x, endPos.x);
            float maxX = Mathf.Max(startPos.x, endPos.x);
            float minY = Mathf.Min(startPos.y, endPos.y);
            float maxY = Mathf.Max(startPos.y, endPos.y);
            DraggingCollector.FrustumDraggingCollectCollider(_player.DragObjectHash, _player.CurrentFrameDetectDragObjectHash,
                _player.CurrentFrameRemoveDragObjectList, _player.DragSelectedColliders, _player.DragDetectLayer, Camera.main,
                minX, minY, maxX, maxY);
        }

        // 드래그 영역 확정
        private void CompleteDrag()
        {
            _player.DragObjectsCompleteCall();
        }
    }
}