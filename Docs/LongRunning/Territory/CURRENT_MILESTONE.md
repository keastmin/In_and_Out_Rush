# Current Milestone

Status: Complete

## Objective

State Authority의 revision 기반 sparse Chunk Territory shadow snapshot을 bounded
Reliable changed-Chunk delta로 Proxy에 복제한다. Networked revision 불일치, packet
거부와 Late Join은 요청 Peer에 targeted 전체 snapshot을 보내 복구한다.

Legacy polygon, 확장 판정, vertex RPC, mesh와 consumer callback은 계속 유일한
게임 권위 경로다.

## Prerequisites

- Active reservation `W-20260822-002-chunk-territory-replication-recovery`
- implementation base `c9772d3d06aec53ebe687ccb70b0e338ce028d39`
- `CONTRACTS.md`의 C001-C007이 Approved일 것
- Chunk Territory state/commit milestone commit `efbe90c`

## Read first

- `AGENTS.md`
- `Docs/Features/Territory.md`
- `Docs/Work/Active/W-20260822-002-chunk-territory-replication-recovery.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## Allowed files

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- 예약된 `Assets/02_Scripts/Territory Refactor/ChunkDomain/` 신규 transfer/replica 파일
- 예약된 `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/` replication stream 파일
- 예약된 `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/` 신규 테스트 파일
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

- State Authority C006 snapshot이 원본이고 Networked revision이 지속적인 수렴
  신호다. Proxy replica는 shadow이며 gameplay mutation을 만들지 않는다.
- delta와 전체 snapshot은 C007 최대 48-word packet과 tick당 최대 2 data packet
  예산을 지킨다.
- Full/Empty run과 Boundary segment fragmentation은 좌표와 방향성 sequence를
  변형하지 않는다.
- 수신자는 transaction과 packet을 완전히 검증한 뒤에만 replica를 원자적으로
  공개하며 실패 시 이전 revision과 coverage를 보존한다.
- Proxy는 revision 불일치에 한 번의 outstanding snapshot 요청만 유지하고 State
  Authority는 요청 Peer에 targeted immutable snapshot을 보낸다.
- Late Join과 복구 도중 새 Commit은 stale/partial 상태를 공개하지 않고 최종
  advertised revision까지 재수렴한다.
- teardown은 pending outbound/inbound/recovery 상태를 소각한다.

## Acceptance criteria

- 최대 payload, tick packet budget, Full/Empty run과 fragmented Boundary round-trip 통과
- delta base/revision, 빈 delta, packet/record gap과 malformed payload 원자성 통과
- 초기 참가와 Late Join targeted snapshot, 복구 중 새 revision 재수렴 절차 확인
- State Authority만 revision을 쓰고 요청자별 recovery 중복이 억제됨
- Legacy Territory, W-012 Trail, RPC, mesh와 consumer 결과 회귀 없음
- ChunkDomain EditMode 테스트와 Fusion Weaver 포함 프로젝트 compile 통과
- Host·Client 정상 확장·거부/Abort 수동 절차 또는 정확한 미검증 기록
- `.meta` pairing, `git diff --check`, Scene·Prefab·설정 diff 없음

## Rollback

`TerritorySystem`의 revision/transfer hook, 신규 transfer/replica/stream/test와 C007
문서만 제거한다. C006 State Authority store와 Legacy polygon/RPC/consumer 경로는
변경 없이 계속 동작한다.

## Out of scope

Chunk 기반 확장 후보 계산, Legacy 256 vertex 보정 제거, GPU, consumer cutover,
AOI별 선택 복제와 Legacy 삭제.

## Current evidence

- 신규 packetizer/replica 7개 assertion과 기존 ChunkDomain 회귀를 포함한 독립
  validation 35/35 통과.
- 48-word 상한과 tick당 2 data packet 예산 검증에서 30-segment Boundary delta가
  4 packet, 2 tick으로 종료됨.
- 1000×1000 사각형 snapshot은 15,876 Chunk를 118 packet/5,641 word로 7.384ms,
  256점·반경 500 snapshot은 12,532 Chunk를 144 packet/6,881 word로 3.282ms에
  packetize함. 로컬 .NET 측정이며 Unity Profiler/Fusion traffic 수치가 아님.
- 신규 source를 직접 포함한 ChunkDomain compile 0 errors, Fusion adapter compile
  0 errors, 임시 target으로 신규 source를 주입한 `Assembly-CSharp` 통합 compile
  0 errors와 기존 warning 13개.
- 실제 Unity import/Fusion Weaver, Host·Client 정상 delta, malformed recovery와
  Late Join runtime은 작업자 검증 대기 중.
- 작업자는 현재 게임이 고정 2인·중도 참가 없음·한 Peer 이탈 시 스테이지 종료라는
  제품 제약을 확인하고 자동 검증 상태로 이번 구현을 Commit·Push하도록 요청했다.
  Host·Client와 Late Join runtime은 수행되지 않았으며 다음 bounded 작업에서
  Late Join·재접속·과도한 요청자 방어를 제거하고 실제 플레이 체감 경로에 집중한다.
