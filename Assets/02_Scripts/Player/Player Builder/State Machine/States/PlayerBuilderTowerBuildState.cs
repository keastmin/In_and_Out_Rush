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
        // 嫄댁꽕 UI ?쒖꽦??
        _player.BuilderUI.ActivationTowerBuildUI(true, "Left Mouse: Build, RightMouse: Cancel");

        if(InfiniteGrid.Instance == null)
        {
            Debug.Log("?몄뒪?댁뒪媛 ?놁뒿?덈떎.");
        }
        InfiniteGrid.Instance.SetCellStateOverlayEnabled(true);

        //// ? ?곹깭 ?ㅻ쾭?덉씠 ?쒖떆
        //GridManager.Instance.SetCellStateOverlayEnabled(true);

        //// 怨좎뒪???앹꽦
        //_towerGhost = Object.Instantiate(_player.BuilderTowerBuild.TowerGhost);
        //if (_player.BuilderTowerBuild.HasBuffRange)
        //{
        //    _towerGhost.SetGhostBuffRange(_player.BuilderTowerBuild.BuffRange);
        //}

        //_canTowerBuild = false;
        //GridManager.Instance.ClearBuildRangePreview();
    }

    public void Update()
    {
        //bool isCenter = _player.BuilderTowerBuild.IsCenterTower;

        //// 留덉슦???꾩튂瑜?湲곕컲?쇰줈 怨좎뒪???ㅻ깄??
        //_canTowerBuild = SnapshotTowerGhost(isCenter);

        //if (Input.GetMouseButtonDown(0) && _canTowerBuild && !EventSystem.current.IsPointerOverGameObject())
        //{
        //    // 醫뚰겢由??ㅼ튂
        //    if (isCenter)
        //        _player.SetCenterTowerCount(_player.CenterTowerCount + 1);

        //    _player.BuilderTowerBuild.BuildTower(_towerBuildIndex);
        //    _player.BuilderTowerBuild.RevertStandBy();
        //}
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

        //// ? ?곹깭 ?ㅻ쾭?덉씠 鍮꾪솢?깊솕
        //GridManager.Instance.ClearBuildRangePreview();
        //GridManager.Instance.SetCellStateOverlayEnabled(false);

        CancelTowerBuild();
    }

    private void TransitionTo()
    {
        if (!_player.BuilderTowerBuild.IsStandByBuild)
        {
            _player.StateMachine.TransitionToState(_player.StateMachine.OriginState);
        }
    }

    // 嫄댁꽕 ?곹깭 醫낅즺
    private void CancelTowerBuild()
    {
        //Object.Destroy(_towerGhost.gameObject);
        _canTowerBuild = false;

        _player.BuilderUI.ActivationTowerBuildUI(false);
    }

    // 留덉슦???덉씠媛 ?좏슚???섍꼍 ?덉씠?대? 留욎톬?붿? 寃??
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

    // 怨좎뒪???꾩튂 諛??ㅼ튂 媛???щ? 媛깆떊
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
