# W-20260822-002 Chunk Territory 복제와 복구

Status: Reserved

## 동기화 기준

- Base Commit: da4f5495c387de8cfd1fac19809046ce6e40c9ea
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Territory, Chunk 상태 복제, State Authority 복구 snapshot, Late Join 수렴.

## 목표

State Authority의 revisioned sparse Chunk Territory shadow snapshot을 Host·Client
Proxy에 복제한다. 정상 Commit은 base revision과 새 revision을 가진 changed-Chunk
delta로 보내며, packet 누락·순서 오류·revision 불일치와 Late Join은 State
Authority가 보유한 전체 sparse snapshot을 요청 Peer에 다시 보내 복구한다.

RPC는 bounded Reliable 전송 수단으로만 사용한다. State Authority의 immutable
snapshot이 원본이고 Networked revision은 지속적인 수렴 신호다. 수신 측은 전송
중인 부분 상태를 공개하지 않으며 전체 transaction 검증이 끝난 뒤에만 replica를
원자적으로 교체한다. Legacy polygon, vertex RPC, 확장 판정과 consumer event는
계속 유일한 게임 권위 경로다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/build-chunk-territory/references/work-session-protocol.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferKind.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferKind.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferPacket.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferPacket.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferPacketizer.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferPacketizer.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkReplica.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkReplica.cs.meta`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryChunkReplicationStream.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryChunkReplicationStream.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkTransferPacketizerTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkTransferPacketizerTests.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkReplicaTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkReplicaTests.cs.meta`
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- 이 작업 문서

## 예약 Scene·Prefab·Data Asset

없음. `GameWorld.unity`, `GamePresentation.unity`, `GameRoot.unity`, `Core.prefab`,
`Territory.prefab`, Material, Shader, Compute Shader, ScriptableObject, asmdef와 Fusion
설정은 수정하지 않는다. 작업자 인스펙터 연결도 추가하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- C001-C006 fixed 좌표, Chunk 소유권, Trail과 상태 Commit 계약은 변경하지 않는다.
- C007 Territory revision/changed-Chunk 복제와 복구 계약을 추가한다.
- State Authority의 sparse snapshot이 원본이며 Proxy replica는 shadow 진단 상태다.
  RPC만으로 지속 상태를 소유하지 않고 Networked authoritative revision을 수렴
  신호로 둔다.
- 정상 Commit delta는 `BaseRevision -> Revision` transaction이고, 전체 snapshot은
  빈 replica에서 특정 revision을 재구성하는 transaction이다. packet과 record는
  연속 sequence를 가지며 중복, gap, stale base와 잘못된 terminal을 거부한다.
- `Full`과 `Empty` tombstone은 같은 행의 연속 Chunk를 결정적 run으로 압축할 수
  있다. `Boundary`의 center-inside와 모든 fixed local 방향성 segment 좌표·sequence는
  제거, 이동 또는 tolerance 증가 없이 전달한다.
- 한 Reliable data RPC의 정수 payload와 한 simulation tick의 전송 packet 수에
  고정 상한을 둔다. 대형 delta와 Late Join snapshot을 여러 tick에 나누되 순서를
  유지하고 완료 전 부분 replica를 공개하지 않는다.
- Proxy가 advertised revision과 수렴하지 못하면 한 번의 outstanding 복구 요청만
  유지한다. State Authority는 요청 `PlayerRef`에 immutable 전체 snapshot을 targeted
  Reliable stream으로 보내며, 전송 중 더 최신 revision이 생기면 검증 후 재수렴한다.
- Bootstrapper, 공개 consumer callback과 기존 Legacy vertex RPC 계약은 변경하지
  않는다.

## 네트워크·Peer 동등성

- Input Authority의 입력, owner prediction과 State Authority 확장 판정은 W-012와
  W-013 그대로다. 이번 작업은 Trail 표시나 확장 성공·실패 결과를 변경하지 않는다.
- State Authority만 Chunk Commit 결과를 delta로 enqueue하고 Networked revision을
  변경한다. Host가 State/Input Authority를 함께 가져도 같은 Commit을 중복 전송하지
  않는다.
- Host의 authoritative store와 Client/Proxy replica를 분리한다. Proxy는 RPC로 받은
  부분 packet이나 Networked revision만 보고 authoritative 상태를 직접 변경하지
  않는다.
- State Authority→Proxy changed-Chunk와 snapshot은 Reliable이며 packet별 크기와
  tick별 발송량이 bounded다. AOI, spawn 시점 또는 transaction 오류로 delta를 적용할
  수 없으면 요청 Peer별 targeted full snapshot으로 복구한다.
- Late Join peer는 Spawn/준비 뒤 Networked revision과 빈 로컬 revision의 차이를
  감지해 snapshot을 요청하고, 완료 후 State Authority와 같은 revision 및 coverage를
  가져야 한다. 복구 중 새 Commit이 발생해도 stale transaction을 공개하지 않고
  최신 advertised revision까지 다시 수렴한다.
- Despawn/TearDown은 outbound queue, inbound transaction, outstanding recovery와
  replica 임시 상태를 정리한다. 재접속은 이전 session의 packet을 수용하지 않는다.
- Host 로컬, 기존 Client, Late Join Client에서 정상·연속 확장, 빈 delta, 큰 delta,
  복구 요청과 중단 정리를 확인한다. Legacy 영역 모양, 성공·거부·실패 피드백과
  consumer 결과는 Peer 종류에 따라 달라지지 않아야 한다.

## 다른 활성 작업과 겹치는 부분

`CheckStart`와 `Docs/Work/Active/` 확인 결과 안내용 README 외 활성 예약이 없어
겹침이 없다.

## 범위 밖

- Chunk snapshot을 실제 영역 권위 원본으로 전환하거나 Legacy polygon/RPC를
  삭제하는 작업
- Chunk 기반 polygon union/확장 후보 계산과 Legacy 256 vertex 보정 제거
- GPU buffer/mask/SDF, Material, Shader, Compute Shader와 CPU fallback presentation
- containment, Grid, Fog, Resource, Monster 등 consumer migration
- 진행 Trail transport, sampling, 자기 교차, Lifeline과 SandTomb 동작 변경
- AOI별 Chunk 가시성 최적화, 디스크 저장, reconnect 영속 저장과 호환성 versioning
- Scene, Prefab, Bootstrapper, asmdef, Package와 직렬화 설정 변경

## 완료 조건

- delta와 full snapshot이 고정 최대 payload를 넘지 않는 ordered Reliable packet으로
  나뉘고 한 tick의 발송 packet 수도 상한을 지킨다.
- `Full`/`Empty` run round-trip과 packet 경계를 넘는 `Boundary` segment round-trip이
  fixed 좌표·순서·center-inside를 변형 없이 보존한다.
- 올바른 base revision의 delta만 원자적으로 적용되고 중복, packet/record gap,
  stale base, 잘못된 terminal과 malformed payload는 기존 replica를 보존한다.
- 빈 delta도 revision을 정확히 한 번 전진시키며 연속 delta가 State Authority
  snapshot과 같은 coverage를 만든다.
- 초기 참가와 Late Join이 targeted full snapshot으로 authoritative revision에
  수렴하고, snapshot 도중 새 Commit이 발생해도 최종 최신 revision으로 복구된다.
- 복구 요청은 Peer별 중복 폭주하지 않고 State Authority가 요청자를 검증한 뒤
  해당 Peer에만 snapshot을 보낸다.
- Host·Client 정상/거부/Abort 플레이 결과, Legacy vertex RPC, mesh, consumer
  callback과 W-012 Trail에 회귀가 없다.
- ChunkDomain EditMode 테스트, Fusion Weaver 포함 프로젝트 compile, Host·Client·Late
  Join 수동 절차 또는 정확한 미검증 기록, `.meta` pairing과 `git diff --check`가
  완료된다.
- Scene·Prefab·Material·Shader·Compute·ScriptableObject·asmdef·Fusion 설정 diff가 없다.

## 실제 변경

예약 단계. 구현 후 기록한다.

## 검증 결과

예약 단계. 구현 후 기록한다.

## 남은 위험

- 대형 snapshot의 실제 bandwidth와 수렴 시간은 Fusion 시뮬레이션과 Unity Profiler로
  확인해야 한다. 이번 작업은 크기와 tick 예산을 제한하지만 AOI별 선택 복제는 하지
  않는다.
- Chunk replica는 계속 shadow이므로 복구 실패가 현재 Legacy 게임 결과를 바꾸지는
  않지만 GPU 및 consumer 전환 전 반드시 수렴 신뢰성을 검증해야 한다.
