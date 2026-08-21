# Territory

Status: Migrating to chunk-based pipeline

Last reviewed: 2026-08-22

## 책임

플레이어 영역 상태와 확장을 관리하고 Grid, Fog, Resource Spawn, Monster 등 소비자에게 영역 변경을 제공한다.

## 현재 기준과 목표

- 현재 authoritative 경로: `Assets/02_Scripts/Territory/TerritorySystem.cs`와 Legacy `Territory`
- 목표 경로: `Assets/02_Scripts/Territory Refactor/` 아래 Chunk·Trail 기반 구현
- 승인된 cutover 전에는 Legacy가 기준이며 새 경로가 같은 부작용을 중복 실행하면 안 된다.

## 주요 진입점

- `TerritorySystem`, `Territory`, `TerritoryExpansion`, `TerritoryVisible`
- `TerritoryTrailSegmentIndex`, `TerritoryTrailChunkRenderer`
- `TerritoryBoundsIndex`, `TerritoryMeshData`
- 신규 순수 기반: `ProjectIO.Territory.ChunkDomain` assembly의
  `FixedTerritoryPoint`, `TerritoryChunkCoordinate`,
  `TerritorySegmentChunkTraversal`, `TerritoryTrailSession`,
  `TerritoryTrailPacket`, `TerritoryTrailPacketizer`,
  `TerritoryTrailReceiver`, `TerritoryTrailShadowComparer`
- Chunk state 기반: `TerritoryChunkFill`, `TerritoryChunkCoverage`,
  `TerritoryChunkSnapshot`, `TerritoryChunkStateBuilder`, `TerritoryChunkStore`
- State Authority adapter: `TerritoryTrailShadowRecorder`,
  `TerritoryTrailReplicationStream`
- `.agents/skills/build-chunk-territory/`

Input Authority owner는 local fixed Trail을 즉시 표시한다. State Authority는
confirmed sample을 bounded Reliable packet으로 보내고 Proxy는 같은 ordered Chunk
path와 Unreliable live head를 표시한다. 현재 확장 결과와 모든 consumer에는 계속
Legacy polygon만 authoritative하며 prediction이나 transport mismatch가 게임 결과를
결정하지 않는다.

State Authority는 초기 Territory와 Legacy 정상 확장 결과를 revisioned sparse
Chunk shadow snapshot으로도 Commit한다. snapshot은 전역 polygon을 보관하지 않고
미저장 `Empty`, payload 없는 `Full`, fixed local 경계 선분을 가진 `Boundary`로
나뉜다. 이 shadow 상태는 아직 RPC, Late Join, 표시와 consumer에 연결되지 않으며
실패해도 Legacy 성공 결과를 되돌리지 않는다.

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

- `TerritorySystem`은 State Authority에서 진행 중인 사선을 외부 강제 이동 동안
  일시 정지하고 Reliable suspension lifecycle을 보낸다.
- owner prediction은 마지막 confirmed point까지 reconcile하고 pause 중 강제 이동을
  선으로 추가하지 않는다. 재개 시 현재 위치의 강제 경로점부터 이어지며 기존
  자기 교차·Lifeline·영토 확장 규칙을 그대로 적용한다.

## 기술 부채

현재 기능 문서와 장기 milestone 문서가 분리되어 있지 않은 부분이 있다. Territory Skill은 장기 마이그레이션 절차를 계속 소유한다.
Legacy polygon은 계속 authoritative다. 이번 성능 slice의 bounds index와 mesh
data는 Chunk Territory cutover가 아니며, 공개 mutable `Vertices`를 직접 쓰는
새 소비자를 추가하지 않는다.
