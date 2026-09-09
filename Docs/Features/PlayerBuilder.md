# Player Builder

Status: Current

Last reviewed: 2026-08-14

## 책임

Builder의 상태 전환과 타워 건설·선택·이동·판매 입력을 조정한다.

## 주요 진입점

- `Assets/02_Scripts/Player/Player Builder/PlayerBuilder.cs`
- `PlayerBuilderStateMachine`과 States
- `PlayerBuilderTowerBuild`, `PlayerBuilderTowerMove`, `PlayerBuilderTowerSell`
- `DraggingCollector`, `ICanClickObject`, `ICanDragObject`

## 주요 연결

Grid 점유, Resource 비용, TowerBuildManager, Builder UI, 역할별 Cinemachine.

## 관련 Asset

Player Builder prefab, Builder UI prefab, Player Builder Cinemachine Camera.

## 변경 시 확인

- 입력 권한과 State Authority 요청 경로
- 이동·판매 실패 시 Grid와 Resource 롤백
- 상태 전환 중 UI와 Ghost 정리
- 타워와 셀 점유의 일관성

## 기술 부채

Builder 로직과 UI가 직접 연결된 경로가 남아 있다. UI를 의존성 전달 통로로 사용하지 않도록 점진적으로 줄인다.
