# Grid and Obstacles

Status: Current

Last reviewed: 2026-08-20

## 책임

무한 Grid 셀 좌표, 점유 상태, 표시 Chunk, Territory·Track 기반 표시 변경, 월드 장애물 배치와 제거를 관리한다.

## 주요 진입점

- `Assets/02_Scripts/Grid/InfiniteGrid.cs`
- `Assets/02_Scripts/Features/Grid/Logic/TowerPlacementPolicy.cs`
- `InfiniteGridObstacleSpawner`
- `InfiniteGridOccupancyIndex`, `InfiniteGridVisualController`
- `IWorldObstacleConsumer`

## 소비자와 입력

- PlayerBuilder와 Tower가 셀 점유를 사용한다.
- Territory와 Track 변경이 Grid 표시와 차단 셀을 갱신한다.
- 활성 Sanctuary는 Territory 밖의 셀을 임시 Tower 건설 가능 영역으로 등록한다.
- Resource Spawn과 Stage가 장애물 목록을 소비한다.
- `PlayerBuilderTowerBuild.EvaluateBuildFootprint` 미리보기는 Grid 조회 결과를 `TowerPlacementCellState`로 변환해 순수 `TowerPlacementPolicy`로 판정한다. 실제 건설과 Host 검증은 계속 `InfiniteGrid.CanPlaceAt`을 사용한다.

## 관련 Asset

- `GameWorld.unity`
- `Assets/03_Prefabs/Grid/`
- Grid layout, rendering, guide, track blocking 설정

## 변경 시 확인

- 월드 좌표와 셀 좌표 변환
- 변경 Chunk만 갱신되는지
- Tower 점유와 Track 차단이 일치하는지
- 활성 Sanctuary 셀의 건설 판정과 소멸 시 Tower 점유 해제가 일치하는지
- Host에서 장애물 Spawn·Despawn 결과가 Client에 반영되는지

## 기술 부채

`InfiniteGrid`가 표시, 점유, Territory, Track, Tower 정리를 폭넓게 조정한다. 새 로직은 기존 보조 클래스처럼 책임별로 추출한다.
