# Current Milestone

Status: Complete

## Objective

고정 2인 세션에서 State Authority의 revision 기반 sparse Chunk Territory shadow
상태를 bounded Reliable changed-Chunk delta로 기존 Client Proxy에 복제한다. 제품에
없는 Late Join·재접속 snapshot, recovery retry와 targeted PlayerRef 경로는 제거한다.

Legacy polygon, 확장 판정, vertex RPC, mesh와 consumer callback은 계속 유일한
게임 권위 경로다.

## Prerequisites

- Active reservation `W-20260822-003-fixed-two-peer-territory-sync`
- implementation base `2f15dbaf558c905e3d2635770623511da31c8f7a`
- `CONTRACTS.md`의 C001-C007이 Approved일 것
- Chunk Territory state/commit milestone commit `efbe90c`

## Read first

- `AGENTS.md`
- `Docs/Features/Territory.md`
- `Docs/Work/Active/W-20260822-003-fixed-two-peer-territory-sync.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## Allowed files

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- 예약된 `Assets/02_Scripts/Territory Refactor/ChunkDomain/` transfer/replica 파일
- 예약된 `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/` replication stream 파일
- 예약된 `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/` 테스트 파일
- `Docs/Features/Territory.md`
- 이번 milestone 장기 문서와 Active 작업 문서

## Reserved Scene·Prefab·Data Asset

없음. Inspector 참조와 직렬화 Asset을 변경하지 않는다.

## Prohibited changes

- Legacy polygon, 확장 판정, vertex RPC와 consumer callback의 권위 전환·삭제
- Chunk 기반 expansion/union과 Legacy vertex 보정 제거
- GPU/CPU presentation, consumer migration
- Scene, Prefab, Bootstrapper, asmdef, Package와 Fusion 설정 변경

## Required behavior

- State Authority C006 store가 원본이고 Proxy replica는 shadow이며 gameplay
  mutation을 만들지 않는다.
- 최초 `0 -> 1`과 이후 연속 delta는 C007 최대 48-word packet과 tick당 최대 2 data
  packet 예산을 지킨다.
- Full/Empty run과 Boundary segment fragmentation은 좌표와 방향성 sequence를
  변형하지 않는다.
- 수신자는 transaction과 packet을 완전히 검증한 뒤에만 replica를 원자적으로
  공개하며 실패 시 이전 revision과 coverage를 보존한다.
- Additive `SetUp`보다 먼저 받은 Proxy delta는 보존하고 teardown은 pending
  outbound/inbound/replica 상태를 소각한다.
- packet gap, stale base와 malformed transaction은 이전 replica를 보존한다. 지원하지
  않는 세션을 위해 snapshot 복구나 retry를 추가하지 않는다.

## Acceptance criteria

- 최대 payload, tick packet budget, Full/Empty run과 fragmented Boundary round-trip 통과
- delta base/revision, 빈 delta, packet/record gap과 malformed payload 원자성 통과
- 최초 `0 -> 1`, 연속 delta와 빈 delta revision 전진 확인
- Snapshot kind, Networked recovery revision, retry와 targeted RPC 제거 확인
- Legacy Territory, W-012 Trail, RPC, mesh와 consumer 결과 회귀 없음
- ChunkDomain EditMode 테스트와 Fusion Weaver 포함 프로젝트 compile 통과
- Host·Client 정상 확장·거부/Abort 수동 절차 또는 정확한 미검증 기록
- `.meta` pairing, `git diff --check`, Scene·Prefab·설정 diff 없음

## Rollback

이번 단순화 diff를 되돌리면 직전 snapshot recovery 구현으로 복원된다. C006 State
Authority store와 Legacy polygon/RPC/consumer 경로는 변경 없이 계속 동작한다.

## Out of scope

Chunk 기반 확장 후보 계산, Legacy 256 vertex 보정 제거, GPU, consumer cutover,
AOI별 선택 복제와 Legacy 삭제.

## Current evidence

- Snapshot kind와 packetizer, `[Networked]` recovery revision, retry/dedup,
  targeted PlayerRef RPC를 제거했다.
- 최초 `0 -> 1`, 연속/빈 delta, stale base, packet gap, malformed terminal과 Reset
  소각을 포함한 기존 ChunkDomain 독립 validation 35/35 통과.
- 30-segment Boundary delta는 48-word packet 4개로 나뉘고 tick당 2 data packet씩
  2 tick에 종료됐다.
- 신규 transfer/replica/stream source를 직접 포함한 validation과 임시 source 주입
  `Assembly-CSharp` 통합 compile 오류 0개. 기존 프로젝트 warning 16개.
- 작업자가 안내된 Unity import/compile, Host·Client Runner 걷기·달리기·속도 전환,
  긴 외부 경로·정상 확장, Abort와 Profiler runtime 절차 완료를 보고했다.
