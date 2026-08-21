# Current Milestone

Status: Complete

## Objective

Input Authority의 Runner가 fixed 좌표 Trail을 네트워크 왕복 전에 즉시 예측
표시하고, State Authority가 확정 sample을 bounded Reliable packet으로 보내 각
peer가 같은 ordered Chunk fragment를 재구성한다. 아직 packet에 포함되지 않은
최신 점은 Unreliable latest-wins live head로만 표시한다.

Territory polygon, 확장 판정, Kill, Lifeline과 consumer 결과는 계속 Legacy
State Authority 경로가 결정한다.

## Prerequisites

- Active reservation `W-20260821-012-chunk-territory-predicted-confirmed-trail`
- implementation base `90d58aa589ab0fc74aa078723cf2b95d18841ab5`
- `CONTRACTS.md`의 C001-C005가 Approved일 것
- shadow Trail milestone commit `ae457bf`

## Read first

- `AGENTS.md`
- `Docs/Features/Territory.md`
- `Docs/Features/PlayerRunner.md`
- `Docs/Work/Active/W-20260821-012-chunk-territory-predicted-confirmed-trail.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## Implemented files

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailShadowRecorder.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailReplicationStream.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailPacket.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailPacketizer.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailReceiver.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailPacketizerTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailReceiverTests.cs`
- `Assets/02_Scripts/Territory Refactor/Trail/TerritoryTrailChunkRenderer.cs`
- 이번 milestone 문서와 기능 문서

## Reserved Scene·Prefab·Data Asset

없음. 기존 `TerritorySystem.lineRenderer` 참조를 runtime template로 사용한다.

## Required behavior

- Host/Client Input Authority owner는 모두 local fixed prediction으로 즉시 선을 본다.
- State Authority sample만 확정 packet이 되며 packet당 sample은 최대 24개다.
- Reliable Start/packet/suspend/Commit/Abort와 Unreliable latest-wins live head를
  분리한다.
- 수신자는 SessionId, packet sequence와 sample sequence gap, stale terminal을
  거부하고 accepted sample만 C003 traversal로 fragment화한다.
- fixed 양자화 이후 path 단순화나 vertex budget 모양 보정을 하지 않는다.
- LineRenderer 하나의 point 수는 256으로 제한하고 같은 Chunk continuation
  segment를 pool에서 이어 사용한다.
- 자기 교차, Lifeline, Stop과 teardown은 outbound/inbound/prediction/live payload와
  선을 소각한다.
- SandTomb pause는 확정점까지 owner prediction을 reconcile한 뒤 정지하며 resume
  강제점부터 이어 간다.
- Legacy Territory 확장, vertex sync와 consumer callback은 변경하지 않는다.

## Acceptance criteria

- bounded packet, round-trip codec, gap/stale/Abort와 fragment 재구성 테스트 통과
- Fusion Weaver를 포함한 프로젝트 compile 통과
- Host 로컬/Client Input Authority 정상·장거리 Trail, 자기 교차/Lifeline과
  SandTomb pause/resume 실제 플레이 통과
- owner 즉시 선, proxy 확정+live head, terminal 후 잔존/중복 없음 확인
- Scene·Prefab·Material·Shader·Compute·Fusion 설정 diff 없음
- `.meta` pairing과 `git diff --check` 통과

## Current evidence

- ChunkDomain runtime/test project compile: 0 errors
- Unity 6000.0.69f1 EditMode 25/25 통과. 신규 packetizer/receiver/codec 7개와
  기존 fixed/traversal/session/shadow comparer 회귀를 포함한다.
- 최종 Unity compile과 Fusion IL post-process: 0 errors
- 작업자가 안내된 Host 로컬/Client Input Authority 정상·장거리 Trail,
  자기 교차/Lifeline, SandTomb pause/resume 런타임 절차 완료를 보고했다.
- Client 걷기 중 선이 끊기던 거리 누적 회귀를 수정한 뒤 걷기와
  걷기/달리기 전환 재검증도 통과했다.
- owner 즉시 선, 정상 영역 확장과 terminal 정리를 포함한 W-012 수동 검증 완료.

## Rollback

신규 packetizer/receiver/replication stream과 `TerritorySystem` RPC·prediction hook,
renderer paging/live-head 변경 및 C005 기록만 제거한다. 비활성 상태로 남아 있는
기존 Legacy path RPC와 authoritative Territory 확장 경로는 이전 fallback이다.

## Out of scope

Chunk Territory state/fill/revision, changed-Chunk replication, Late Join 진행 Trail,
GPU presentation, consumer migration, Legacy authoritative cutover와 삭제.
