using Fusion;
using Grid;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerBuilderTowerBuildState : IPlayerState
{
    private readonly PlayerBuilder _player;

    private TowerGhost _towerGhost;
    private Vector3 _towerBuildPosition;
    private Vector2Int _towerBuildIndex;
    private bool _canTowerBuild;

    public PlayerBuilderTowerBuildState(PlayerBuilder player)
    {
        _player = player;
    }

    public void Enter()
    {
        _player.BuilderUI.ActivationTowerBuildUI(true, "Left Mouse: Build, RightMouse: Cancel");

        InfiniteGrid.Instance.SetCellStateOverlayEnabled(true);
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _player.BuilderTowerBuild.RevertStandBy();
        }

        TransitionTo();
    }

    public void LateUpdate()
    {
        _player.BuilderCamMove();
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
        //Object.Destroy(_towerGhost.gameObject);
        _canTowerBuild = false;

        _player.BuilderUI.ActivationTowerBuildUI(false);
    }

    private bool IsValidMouseRay(out Vector3 mousePosition)
    {
        mousePosition = default;
        bool isValid = false;

        var cam = Camera.main;
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 5000f, _player.EnvironmentalLayer))
        {
            mousePosition = hit.point;
            isValid = true;
        }

        return isValid;
    }

    private bool SnapshotTowerGhost(bool isCenter)
    {
        bool canTowerCraft = false;

        if (IsValidMouseRay(out Vector3 mouseHitPoint))
        {
            _towerBuildIndex = GridManager.Instance.GetNearestCellIndex(mouseHitPoint);
            _towerBuildPosition = GridManager.Instance.GetCellCenterPositionFromIndex(_towerBuildIndex);
            _towerGhost.transform.position = _towerBuildPosition;
            GridManager.Instance.SetBuildRangePreview(_towerBuildIndex, _player.BuilderTowerBuild.BuildRange);

            bool canPlaceByArea = GridManager.Instance.CanPlaceInRange(
                _towerBuildIndex,
                _player.BuilderTowerBuild.BuildRange,
                requireEmpty: true,
                requireTerritory: true);

            bool isMineralEnough = StageManager.Instance.ResourceSystem.Mineral >= _player.BuilderTowerBuild.BuildCost.Mineral;
            bool isGasEnough = StageManager.Instance.ResourceSystem.Gas >= _player.BuilderTowerBuild.BuildCost.Gas;
            bool isExceededCenterCount = (!isCenter) || (isCenter && _player.CenterTowerCount < _player.MaxCenterTowerCount);
            bool eachTowerCondition = _player.BuilderTowerBuild.TowerBuildConditionChecker(_player.BuilderTowerBuild.TowerID);

            if (canPlaceByArea && isMineralEnough && isGasEnough && isExceededCenterCount && eachTowerCondition)
            {
                _towerGhost.EnableTower();
                canTowerCraft = true;
            }
            else
            {
                _towerGhost.DisableTower();
            }
        }
        else
        {
            GridManager.Instance.ClearBuildRangePreview();
        }

        return canTowerCraft;
    }
}
