using Fusion;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KIM.Dev
{
    public class PlayerBuilderTowerBuildState : IPlayerState
    {
        private readonly PlayerBuilder _player;
        private readonly HashSet<Vector2Int> _previewValidIndices = new();
        private readonly HashSet<Vector2Int> _previewBlockedIndices = new();

        private TowerGhost _towerGhost;
        private Vector3 _towerBuildPosition;
        private Vector2Int _towerBuildIndex;

        public PlayerBuilderTowerBuildState(PlayerBuilder player)
        {
            _player = player;
        }

        public void Enter()
        {
            _player.BuilderUI.ActivationTowerBuildUI(true, "Left Mouse: Build, RightMouse: Cancel");

            InfiniteGrid.Instance.SetCellStateOverlayEnabled(true);
            InfiniteGrid.Instance.ClearBuffPreviewSources();
            CreateTowerGhost();
        }

        public void Update()
        {
            if (SnapshotTowerGhost(_player.BuilderTowerBuild.IsCenterTower) &&
                Input.GetMouseButtonDown(0) &&
                !EventSystem.current.IsPointerOverGameObject())
            {
                _player.BuilderTowerBuild.BuildTower(_towerBuildIndex);
                _player.BuilderTowerBuild.RevertStandBy();
                return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                _player.BuilderTowerBuild.RevertStandBy();
            }

            TransitionTo();
        }

        public void Exit()
        {
            InfiniteGrid.Instance.SetCellStateOverlayEnabled(false);

            CancelTowerBuild();
        }

        private void TransitionTo()
        {
            if (!_player.BuilderTowerBuild.IsStandByBuild)
            {
                _player.StateMachine.TransitionToState(_player.StateMachine.OriginState);
            }
        }

        private void CancelTowerBuild()
        {
            if (_towerGhost != null)
            {
                Object.Destroy(_towerGhost.gameObject);
                _towerGhost = null;
            }

            _previewValidIndices.Clear();
            _previewBlockedIndices.Clear();
            InfiniteGrid.Instance?.ClearBuildRangePreview();
            _player.BuilderTowerBuild.ClearBuffCellPreview();

            _player.BuilderUI.ActivationTowerBuildUI(false);
        }

        private void CreateTowerGhost()
        {
            _towerGhost = _player.BuilderTowerBuild.CreateTowerGhostInstance();
        }

        private bool TryGetMouseWorldPositionOnGrid(out Vector3 mousePosition)
        {
            mousePosition = default;
            if (InfiniteGrid.Instance == null)
                return false;

            Camera cam = Camera.main;
            if (cam == null)
                return false;

            Plane plane = new Plane(Vector3.up, InfiniteGrid.Instance.GridOrigin);
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float enter))
            {
                mousePosition = ray.GetPoint(enter);
                return true;
            }

            return false;
        }

        private bool SnapshotTowerGhost(bool isCenter)
        {
            bool canTowerCraft = false;

            if (_towerGhost == null || InfiniteGrid.Instance == null)
                return false;

            if (TryGetMouseWorldPositionOnGrid(out Vector3 mouseHitPoint))
            {
                _towerBuildIndex = InfiniteGrid.Instance.GetCellIndexFromWorldPosition(mouseHitPoint);
                _towerBuildPosition = InfiniteGrid.Instance.GetCellCenterPositionFromCellIndex(_towerBuildIndex);
                _towerGhost.transform.position = _towerBuildPosition;
                _player.BuilderTowerBuild.UpdateBuffCellPreview(_towerGhost, _towerBuildIndex);

                _player.BuilderTowerBuild.EvaluateBuildFootprint(_towerBuildIndex, _previewValidIndices, _previewBlockedIndices);
                InfiniteGrid.Instance.SetBuildRangePreview(_previewValidIndices, _previewBlockedIndices);

                bool canBuild = _previewBlockedIndices.Count == 0 &&
                                _player.BuilderTowerBuild.CanBuildAt(_towerBuildIndex);

                if (canBuild)
                {
                    _towerGhost.EnableTower();
                    canTowerCraft = true;
                }
                else
                {
                    _towerGhost.DisableTower();
                }
            }

            return canTowerCraft;
        }
    }
}
