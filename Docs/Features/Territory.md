# Territory

Status: Migrating to chunk-based pipeline

Last reviewed: 2026-08-21

## 책임

플레이어 영역 상태와 확장을 관리하고 Grid, Fog, Resource Spawn, Monster 등 소비자에게 영역 변경을 제공한다.

## 현재 기준과 목표

- 현재 authoritative 경로: `Assets/02_Scripts/System/TerritorySystem.cs`와 Legacy `Territory`
- 목표 경로: `Assets/02_Scripts/Territory Refactor/` 아래 Chunk·Trail 기반 구현
- 승인된 cutover 전에는 Legacy가 기준이며 새 경로가 같은 부작용을 중복 실행하면 안 된다.

## 주요 진입점

- `TerritorySystem`, `Territory`, `TerritoryExpansion`, `TerritoryVisible`
- `TerritoryTrailSegmentIndex`, `TerritoryTrailChunkRenderer`
- `TerritoryBoundsIndex`, `TerritoryMeshData`
- 신규 순수 기반: `ProjectIO.Territory.ChunkDomain` assembly의
  `FixedTerritoryPoint`, `TerritoryChunkCoordinate`,
  `TerritorySegmentChunkTraversal`, `TerritoryTrailSession`,
  `TerritoryTrailShadowComparer`
- State Authority 진단 adapter: `TerritoryTrailShadowRecorder`
- `.agents/skills/build-chunk-territory/`

신규 Chunk domain은 Editor/Development Build의 State Authority에서 Legacy path와
순서를 비교하는 shadow 진단에만 연결됐다. 현재 확장 결과와 모든 consumer에는
계속 Legacy polygon만 authoritative하며 shadow mismatch도 게임 결과를 변경하지
않는다.

## 주요 소비자

Grid 표시, Fog of War, Resource 수집·Spawn, Track·World Monster, Sacred Zone, PlayerRunner 보호 판정.

## 관련 Asset

- `GameWorld.unity`
- Territory·Track 표시 오브젝트와 Core 연결
- 장기 개발 문서는 Territory Skill이 지정한 경로를 따른다.

## 변경 시 확인

- State Authority와 Late Join 복원
- 좌표·revision·변경 Chunk 계약
- Legacy와 새 경로의 중복 이벤트·표현
- 모든 소비자 전환 여부와 롤백
- `Territory.Vertices`를 갱신하는 현재 seam이 `ReplaceVertices`를 통해 bounds
  cache를 같은 시점에 갱신하는지
- 확장 결과의 검증된 triangle data를 Host mesh에 재사용하고 같은 polygon을
  중복 triangulation하지 않는지
- Chunk domain의 1 world unit = 256 fixed unit, 8 world-unit Chunk,
  half-open 소유권과 Trail sequence 계약을 변경하지 않는지. 변경이 필요하면
  `Docs/LongRunning/Territory/CONTRACTS.md`와 migration을 먼저 갱신한다.

## 외부 강제 이동 사선 중단

- `TerritorySystem`은 State Authority에서 진행 중인 사선을 외부 강제 이동 동안 일시 정지할 수 있다. 정지 동안에는 경로·Trail chunk renderer·path RPC를 갱신하지 않는다.
- 재개 시 정지점과 현재 위치를 강제 경로점으로 복제하고, 기존 자기 교차·Lifeline·영토 확장 규칙을 그대로 적용한다.

## 기술 부채

현재 기능 문서와 장기 milestone 문서가 분리되어 있지 않은 부분이 있다. Territory Skill은 장기 마이그레이션 절차를 계속 소유한다.
Legacy polygon은 계속 authoritative다. 이번 성능 slice의 bounds index와 mesh
data는 Chunk Territory cutover가 아니며, 공개 mutable `Vertices`를 직접 쓰는
새 소비자를 추가하지 않는다.
