# Territory

Status: Migrating to chunk-based pipeline

Last reviewed: 2026-08-15

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
- `.agents/skills/build-chunk-territory/`

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

## 기술 부채

현재 기능 문서와 장기 milestone 문서가 분리되어 있지 않은 부분이 있다. Territory Skill은 장기 마이그레이션 절차를 계속 소유한다.
Legacy polygon은 계속 authoritative다. 이번 성능 slice의 bounds index와 mesh
data는 Chunk Territory cutover가 아니며, 공개 mutable `Vertices`를 직접 쓰는
새 소비자를 추가하지 않는다.
