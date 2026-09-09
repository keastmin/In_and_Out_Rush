# W-20260822-003 고정 2인 Territory 동기화 단순화

Status: Complete

## 동기화 기준

- Base Commit: c1e70a86b22ee8d3b7a4f23e50e0f916efa38c3e
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Territory, Player Runner Trail, 고정 2인 Host·Client 동기화와 frame 안정성.

## 목표

현재 게임의 실제 세션 계약인 “최대 2인, 두 Peer가 스테이지 시작부터 함께함,
한 Peer가 이탈하면 스테이지 종료”에 맞춰 Chunk Territory 복제를 단순화한다.
Late Join·재접속 snapshot, recovery retry, targeted PlayerRef 전송과 다수 요청자 방어를
제거하고 State Authority→기존 Client Proxy의 bounded Reliable changed-Chunk delta만
유지한다.

Input Authority owner의 즉시 Trail prediction과 State Authority confirmed Trail 계약은
Host Runner와 Client Runner에 동일하게 적용한다. 이번 작업은 Trail을 새로 설계하지
않고 걷기·달리기·속도 전환·긴 외부 이동·정상 확장의 실제 Peer 동등성과 frame
profile을 완료 조건으로 고정한다. Legacy polygon과 consumer는 계속 gameplay
authority다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- `Docs/Work/Completed/W-20260822-002-chunk-territory-replication-recovery.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/build-chunk-territory/references/work-session-protocol.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryChunkReplicationStream.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferKind.cs` 삭제
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferKind.cs.meta` 삭제
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferPacket.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferPacketizer.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkReplica.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkTransferPacketizerTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkReplicaTests.cs`
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- 이 작업 문서

## 예약 Scene·Prefab·Data Asset

없음. Scene, Prefab, Material, Shader, Compute Shader, ScriptableObject, asmdef, Package,
Fusion 설정과 Inspector 참조를 변경하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- C001-C006과 C005 owner-predicted/confirmed Trail 좌표·순서·수명 계약은 변경하지
  않는다.
- C007을 고정 2인 delta-only 계약으로 축소한다. Snapshot transfer kind,
  `[Networked]` recovery revision, mismatch/retry timer, targeted snapshot RPC와
  PlayerRef request validation을 제거한다.
- State Authority의 C006 store는 계속 원본이며 정상 Commit의
  `BaseRevision -> Revision` changed-Chunk만 기존 Proxy에 ordered Reliable stream으로
  보낸다. packet당 48 word와 TerritorySystem당 tick 최대 2 data packet 상한은
  유지한다.
- Proxy는 spawn된 NetworkObject가 delta를 받을 수 있는 시점부터 transaction을
  수용한다. Additive `SetUp`이 RPC보다 늦어도 이미 받은 inbound 상태를 다시
  초기화하지 않으며, TearDown에서는 즉시 소각한다.
- sequence/base/malformed 검증은 보안 확장이 아니라 current-session state 손상을
  막는 최소 안정성 계약으로 유지한다.
- Bootstrapper, Legacy vertex RPC, public consumer callback과 Trail RPC 형식은
  변경하지 않는다.

## 네트워크·Peer 동등성

- Host 또는 Client가 담당하는 Player Runner 모두 각자의 Input Authority에서 이동과
  local Trail prediction을 즉시 처리한다. State Authority가 기존 규칙으로 Trail과
  정상 영역 확장을 한 번만 확정한다.
- owner Trail은 local prediction을 사용하므로 Host/Client 역할에 따라 러너보다
  앞서거나 뒤처지는 구조적 차이가 없어야 한다. 다른 Peer는 confirmed packet과
  Unreliable live head를 표시한다.
- Chunk shadow delta는 State Authority만 생성하고 Proxy는 완전한 transaction만
  적용한다. Host가 State/Input Authority를 함께 가져도 delta, Legacy 확장 이벤트,
  mesh와 consumer callback을 중복 실행하지 않는다.
- 두 Peer는 모두 스테이지 시작 시 존재한다. Late Join, reconnect, 세 번째 Peer,
  AOI 재진입 snapshot과 해당 요청자 인증은 지원하거나 검증하지 않는다. 한 Peer
  이탈 시 기존 게임 흐름대로 스테이지가 종료되고 pending Trail/Chunk 상태를
  정리한다.
- Host Runner와 Client Runner 각각 걷기, 달리기, 속도 전환, 긴 외부 경로, 정상
  확장, 자기 교차/Abort를 실제 실행해 선 연속성, 확장 결과, 성공·실패 체감과 frame
  profile을 별도로 기록한다. 컴파일만으로 동등성을 완료 처리하지 않는다.

## 다른 활성 작업과 겹치는 부분

`CheckStart`와 `Docs/Work/Active/` 확인 결과 안내용 README 외 활성 예약이 없어
겹침이 없다.

## 범위 밖

- Late Join, reconnect, 세 번째 Peer, 세션 지속, AOI recovery와 보안 hardening
- Trail sample 주기·packet 크기·live-head transport의 근거 없는 재설계
- Chunk/GPU presentation, mask/SDF, Material·Shader·Compute Shader
- Chunk 기반 polygon union, Legacy 256 vertex 보정 제거와 authoritative cutover
- containment, Grid, Fog, Resource, Monster 등 consumer migration
- Scene, Prefab, Bootstrapper, asmdef, Package와 Fusion 설정 변경

## 완료 조건

- Snapshot transfer kind, snapshot packetizer/receiver 테스트, Networked recovery
  revision, retry/dedup/targeted RPC와 PlayerRef 방어 경로가 제거된다.
- initial `0 -> 1`과 연속 changed-Chunk delta가 48-word packet, tick당 최대 2 data
  packet으로 기존 Client에 적용되고 빈 delta도 revision을 정확히 전진시킨다.
- Proxy `SetUp`이 initial delta보다 늦어도 수신 상태를 지우지 않고, TearDown은
  transaction/replica/outbound 상태를 정리한다.
- stale base, sequence gap, malformed payload는 현재 replica를 보존하고 current
  session에서 다음 게임 결과나 Legacy 경로를 변경하지 않는다.
- Host Runner와 Client Runner 모두 걷기·달리기·속도 전환에서 owner 선이 연속이며
  러너보다 구조적으로 앞서거나 뒤처지지 않는다.
- 양쪽 Runner의 긴 외부 경로와 정상 확장이 같은 모양·성공 결과·consumer 결과를
  만들고, Abort/자기 교차는 양쪽에서 동일하게 정리된다.
- Unity Profiler에서 Trail 이동 frame, 정상 확장 frame, Chunk delta packetize와
  tick flush를 Host/Client별로 기록한다. 자동 환경에서 실행할 수 없으면 작업자가
  수행할 정확한 절차와 미검증 상태를 남긴다.
- ChunkDomain 집중 테스트, Fusion RPC/Networked 제거를 포함한 프로젝트 compile,
  `.meta` 삭제 pairing, `git diff --check`가 통과한다.
- Scene·Prefab·설정·asmdef diff와 Legacy/Chunk 중복 gameplay side effect가 없다.

## 실제 변경

- `TerritoryChunkTransferKind` script/meta와 Snapshot packetizer/replica 분기를 삭제하고
  최초 `0 -> 1`부터 연속 delta만 표현하도록 packet metadata를 축소했다.
- replica는 current coverage 복사본에 Full/Boundary 변경과 Empty tombstone을 적용하고
  terminal 검증 후에만 새 revision을 공개한다. stale base, sequence gap, malformed
  payload에서는 기존 snapshot을 유지한다.
- `TerritoryChunkReplicationStream`에서 Fusion `PlayerRef`, targeted transfer,
  snapshot queue, recovery timer/dedup을 제거했다. broadcast delta queue와 tick당 최대
  2 data packet 예산만 유지했다.
- `TerritorySystem`에서 `[Networked]` recovery revision, request/targeted Snapshot RPC와
  active-player 검증을 제거했다. State Authority만 setup 시 stream을 초기화해 Proxy가
  Additive `SetUp` 전에 받은 delta를 보존하고 TearDown/Dispose는 전체 상태를
  소각한다.
- `TerritorySystem.ChunkDeltaPacketize`와 `TerritorySystem.ChunkTransferFlush` Profiler
  marker를 추가했다. Legacy 확장, consumer와 Trail transport는 변경하지 않았다.
- C007, Territory 기능 문서와 장기 milestone/handoff/roadmap/test 문서를 실제 고정
  2인 delta-only 계약과 수동 검증 절차로 갱신했다.

## 검증 결과

- 변경 source와 기존 ChunkDomain 테스트를 직접 포함한 독립 validation 35/35 통과.
- 최초 `0 -> 1`, 연속/빈 delta, Full/Empty run, fragmented Boundary, stale base,
  packet gap, malformed terminal과 Reset 소각 assertion 통과.
- adapter validation에서 30-segment Boundary delta가 최대 48-word data packet 4개로
  나뉘고 tick당 2개씩 2 tick에 종료됨.
- 신규 source를 임시 target으로 주입한 `Assembly-CSharp.csproj` 통합 compile 오류
  0개, 기존 warning 16개.
- `TerritoryChunkTransferKind.cs`와 `.meta`가 함께 삭제됐고 Snapshot/recovery/targeted
  source 참조가 남지 않았음을 확인했다.
- 실제 Unity import/Fusion Weaver, EditMode 전체와 Host·Client 걷기·달리기·긴 경로·
  확장·Abort·Profiler 절차는 작업자가 완료했다고 보고했다.

## 남은 위험

- current-session Reliable delta가 실제 제품 조건에서는 충분한지 Host·Client runtime
  전까지 확정할 수 없다. 지원하지 않는 Late Join/reconnect로 범위를 다시 넓혀
  보상하지 않는다.
- Legacy polygon union, 256 vertex 보정과 synchronous Chunk state build는 이번
  단순화 범위 밖이므로 정상 확장 frame hitch의 주원인이면 후속 CPU/GPU milestone이
  필요하다.
- Reliable delta가 Proxy spawn 준비보다 먼저 유실되는 실제 ordering이 있다면
  recovery 없는 shadow replica가 뒤처질 수 있다. 고정 2인 runtime에서 initial
  revision 1 수신을 반드시 확인해야 한다.
