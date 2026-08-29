# W-20260829-002 Territory 영역 판정 소비자 전환

Status: Reserved

## 동기화 기준

- Base Commit: 520ab02dce6691f48e263256edc733802bfebd59
- 공용 Upstream: `origin/rebuild-development-environment`

## 담당자

Codex 메인 오케스트레이터. 소비자 분류와 전환 경계는 메인 에이전트가 판단하며,
하위 에이전트는 읽기 전용 사용처 감사만 수행한다.

## 기능

Territory 내부·외부 판정 소비자와 무한 Grid 영역 표시 Chunk 분류.

## 목표

- 현재 런타임에서 Territory 내부·외부 판정이 필요한 소비자를 전수 검색해 공용
  `Territory.IsPointInPolygon`/`IsPointOnBoundary` 사용 여부를 분류한다.
- 타워 설치 가능 여부, Player Builder footprint 미리보기, 몬스터 이동·스폰 판정이
  새 `TerritorySpatialIndex` facade를 사용하는 실제 호출 경로임을 확인한다.
- `Territory.Vertices`를 직접 순회해 별도 scanline 판정을 수행하는
  `InfiniteGridTerritoryChunkClassifier`를 공용 Territory query로 전환한다.
- Grid Chunk 상태 캐시는 유지하되, 셀 색상과 실제 건설 판정이 같은 boundary-inside,
  epsilon, concave, 음수 좌표 의미를 사용하게 한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/Features/GridAndObstacles.md`
- `manage-feature-work`
- `build-chunk-territory`
- `migrate-feature-slice`

## 예상 수정 코드

- `Assets/02_Scripts/Grid/InfiniteGridTerritoryChunkClassifier.cs`
- `Assets/02_Scripts/Territory Refactor/Tests/Editor/InfiniteGridTerritoryChunkClassifierTests.cs`
- `Assets/02_Scripts/Territory Refactor/Tests/Editor/InfiniteGridTerritoryChunkClassifierTests.cs.meta`
- `Docs/Features/Territory.md`
- `Docs/Features/GridAndObstacles.md`
- `Docs/Work/Active/W-20260829-002-territory-query-consumer-cutover.md`

## 예약 Scene·Prefab·Data Asset

없음. Scene, Prefab, ScriptableObject, ProjectSettings, Package와 직렬화 참조는 변경하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- `Territory.IsPointInPolygon(Vector2)`과 `Territory.IsPointOnBoundary(Vector2)`의 공개
  signature와 boundary-inside 의미를 유지한다.
- `InfiniteGrid.IsCellInTerritory`, `IsCellInBuildArea`, `CanPlaceAt` 및 Builder footprint
  preview의 공개 계약은 유지한다.
- Grid 표시 Chunk 캐시와 변경 Chunk invalidation 계약은 유지하고, 캐시를 채우는 membership
  계산만 공용 Territory query로 전환한다.
- `Territory.Vertices`는 Mesh 생성, 경계 변경 감지, 복제 snapshot처럼 Polygon 데이터가
  필요한 경로에서 계속 read-only snapshot으로 사용할 수 있다.

## 네트워크·Peer 동등성

- 새 Networked 상태, RPC, Spawn/Despawn 또는 Authority 변경은 없다.
- 타워 실제 건설은 기존처럼 State Authority의 `InfiniteGrid.CanPlaceAt` 검증을 사용하고,
  Host 로컬과 Client Input Authority의 Builder 미리보기는 각 Peer가 복제 완료된 같은 Territory
  Polygon의 공용 query를 사용한다.
- Grid base atlas는 로컬 presentation cache지만 실제 건설 판정과 같은 query 의미를 사용해야 한다.
  Territory revision 반영 시 Host·Client 양쪽에서 같은 셀 색상으로 갱신되는지 수동 절차를 기록한다.
- 몬스터 AI와 spawn mutation은 기존 State Authority 경로를 유지하며 Client는 기존 복제 결과를
  관찰한다. 이번 작업은 해당 호출자의 코드를 바꾸지 않는다.

## 다른 활성 작업과 겹치는 부분

없음. `CheckStart` 시 `Docs/Work/Active/`에는 안내용 `README.md`만 존재했다.

## 범위 밖

- Territory Polygon, spatial index, 확장·Trail, Fusion 복제 구조 변경
- Grid visual shader, 색상 디자인, Chunk 크기와 cache eviction 정책 변경
- `InfiniteGridTerritoryChangeTracker`의 경계 snapshot 비교. 이는 membership가 아니라 변경된
  표시 Chunk invalidation을 위한 저빈도 계산이다.
- `SanctuaryView.IsCircleOverlappingSanctuary`의 원-다각형 겹침과 Fog의 Mesh triangle 기반
  시야 판정. 두 경로는 점 내부·외부 판정과 다른 공간 query/표현 계약이다.
- Scene·Prefab 변경과 실제 몬스터/타워 규칙 변경

## 완료 조건

- 활성 런타임의 Territory 점 내부·외부 판정 소비자가 모두 공용 Territory query를 사용하거나,
  membership가 아닌 이유가 작업 결과에 명시된다.
- Grid atlas classifier가 `Territory.Vertices`를 직접 순회하거나 자체 boundary/ray-crossing
  알고리즘을 유지하지 않는다.
- Grid Chunk 캐시 갱신 결과가 정형·concave·boundary·음수 좌표에서
  `Territory.IsPointInPolygon` 결과와 일치한다.
- 타워 설치/Builder preview/몬스터 판정 호출 경로를 정적 검증하고 관련 집중 Editor test,
  Unity compile, `git diff --check`, 정확한 변경 파일 검사를 묶어서 수행한다.
- 실제 Host·Client Grid 표시를 실행하지 못하면 동일 Territory 확장 뒤 두 Peer의 셀 색상과
  실제 설치 성공·거부가 일치하는 수동 검증 절차를 남긴다.

## 실제 변경

예약 단계. 구현 후 기록한다.

## 검증 결과

- `CheckStart`: `READY_TO_CHECK_CONFLICTS`, `AHEAD=0`, `BEHIND=0`.
- Active 충돌: 없음.
- 읽기 전용 감사에서 유일한 독립 point membership 경로로
  `InfiniteGridTerritoryChunkClassifier`를 확인했다.
- 타워 설치·Builder footprint·몬스터 이동·스폰의 현재 호출자는 이미
  `Territory.IsPointInPolygon` facade를 사용한다.

## 남은 위험

- 표시 Chunk를 다시 계산할 때 셀 수만큼 공용 point query가 실행되므로 실제 visible atlas
  규모의 Profiler 비용은 런타임에서 확인해야 한다. 기존 Chunk 캐시가 정상인 동안 매 frame
  재계산하지 않는 계약은 유지한다.
- 실제 Host·Client 런타임 Grid 표시와 타워 성공·거부 동등성은 자동 테스트만으로 완전히
  검증할 수 없다.
