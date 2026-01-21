using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerBuilderTowerBuildState : IPlayerState
{
    private PlayerBuilder _player;

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
        // UI 활성화
        _player.BuilderUI.ActivationTowerBuildUI(true, "Left Mouse: Build, RightMouse: Cancel");

        // 타워 가이드 생성
        _towerGhost = Object.Instantiate(_player.BuilderTowerBuild.TowerGhost);
        if (_player.BuilderTowerBuild.HasBuffRange)
        {
            _towerGhost.SetGhostBuffRange(_player.BuilderTowerBuild.BuffRange);
        }

        // 타워 건설 가능을 false로 초기화
        _canTowerBuild = false;
    }

    public void Update()
    {
        // 센터 타워인가?
        bool isCenter = _player.BuilderTowerBuild.IsCenterTower;

        // 타워 설치 전 예시 타워 설치
        _canTowerBuild = SnapshotTowerGhost(isCenter);

        if (Input.GetMouseButtonDown(0) && _canTowerBuild && !EventSystem.current.IsPointerOverGameObject())
        {
            // 좌클릭을 하면 타워 설치
            if (isCenter) _player.SetCenterTowerCount(_player.CenterTowerCount + 1); // 센터타워라면 카운트 하나 증가 
            _player.BuilderTowerBuild.BuildTower(_player.Grid, _towerBuildIndex); // 타워 설치
            _player.BuilderTowerBuild.RevertStandBy(); // 되돌리기
        }
        else if (Input.GetMouseButtonDown(1))
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
        CancelTowerBuild();
    }

    private void TransitionTo()
    {
        if (!_player.BuilderTowerBuild.IsStandByBuild)
        {
            _player.StateMachine.TransitionToState(_player.StateMachine.OriginState);
        }
    }

    // 타워 빌드 종료
    private void CancelTowerBuild()
    {
        Object.Destroy(_towerGhost.gameObject);
        _canTowerBuild = false;

        _player.BuilderUI.ActivationTowerBuildUI(false);
    }

    // 스크린에서 찍고 있는 마우스가 설정한 레이어에 닿았는지 판별하고 위치를 반환하는 함수
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

    // 타워 예시를 스냅샷하는 함수
    private bool SnapshotTowerGhost(bool isCenter)
    {
        Vector3 mouseHitPoint;
        bool isValid = IsValidMouseRay(out mouseHitPoint);
        bool canTowerCraft = false;

        if (isValid)
        {
            // 셀 중앙으로 스냅샷
            _towerBuildIndex = _player.Grid.GetNearIndex(mouseHitPoint);
            _towerBuildPosition = _player.Grid.GetNearCellPositionFromIndex(_towerBuildIndex);
            _towerGhost.transform.position = _towerBuildPosition;

            bool isInTerritory = _player.Grid.IsPointInTerritory(_towerBuildIndex); // 영역 내부인가?
            bool isMineralEnough = StageManager.Instance.ResourceSystem.Mineral >= _player.BuilderTowerBuild.BuildCost.Mineral; // 미네랄이 충분한가?
            bool isGasEnough = StageManager.Instance.ResourceSystem.Gas >= _player.BuilderTowerBuild.BuildCost.Gas; // 가스가 충분한가?
            bool isExceededCenterCount = (!isCenter) || (isCenter && _player.CenterTowerCount < _player.MaxCenterTowerCount); // 설치할 타워가 센터 타워이고 센터 타워의 개수가 충분한가?
            bool eachTowerCondition = _player.BuilderTowerBuild.TowerBuildConditionChecker(_player.BuilderTowerBuild.TowerID); // 각 타워의 설치 조건을 검사

            // 타워 설치 가능 여부 판별 후 타워 고스트 색 변경
            if (_player.Grid.IsEmptyCell(_towerBuildIndex) && isInTerritory && isMineralEnough && isGasEnough && isExceededCenterCount && eachTowerCondition)
            {
                // 타워 설치가 가능하다면 푸른색으로 변경하고 타워 설치 가능 플래그를 true로 변경
                _towerGhost.EnableTower();
                canTowerCraft = true;
            }
            else
            {
                // 타워 설치가 불가능하다면 붉은색
                _towerGhost.DisableTower();
            }
        }

        return canTowerCraft;
    }
}
