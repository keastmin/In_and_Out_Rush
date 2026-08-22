# W-20260822-007 영속 압축 Territory Store

Status: Reserved

## 동기화 기준

- Base Commit: 1e9b401c628b05e0f4adba7d12b8b5ce9b2fc034
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

C009 compact expansion materialization을 면적만큼 다시 전개하지 않고 반복 적용할 수
있는 stable Boundary identity와 persistent compact Territory state.

## 목표

최상위 제품 목표는 다음으로 고정한다.

- Runner가 그린 fixed Trail 좌표와 확장 Boundary 모양을 이동·삭제·근사하지 않는다.
- 확장 횟수와 기존 Territory 크기가 증가해도 이전 전체 Boundary, 전체 Full Chunk와
  전체 snapshot을 매번 복사·순회하지 않는다.
- 내부 Territory는 행별 Full run 압축을 영구 저장 계약으로 유지하고 개별 Full
  coverage로 다시 펼치지 않는다.
- Boundary segment는 전역 연속 sequence를 매 확장마다 재번호화하지 않고 stable
  identity와 결정적 순서를 유지한다.
- C009 결과 적용은 bounded work와 구조 공유 candidate로 진행하며 terminal 전에는
  source state를 바꾸거나 결과를 공개하지 않는다.
- 첫 확장뿐 아니라 연속된 다수 확장 뒤에도 다음 C008 plan과 C009 materialization을
  compact snapshot에서 직접 계속할 수 있어야 한다.

이번 slice는 잘못된 단기 해법인 `C009 Full run -> C006 개별 Full Chunk` 전개나
`전체 Boundary -> 0..N-1 sequence` 재생성을 구현하지 않는다. 초기 C006 shadow
snapshot은 compact state로 한 번 변환할 수 있지만, 그 뒤의 정상 확장은 persistent
Boundary/row 구조의 변경 부분만 갱신한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- `Docs/Work/Completed/W-20260822-005-exact-incremental-expansion-plan.md`
- `Docs/Work/Completed/W-20260822-006-compact-expansion-materialization.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/build-chunk-territory/references/work-session-protocol.md`

## 예상 수정 코드

신규 순수 Domain 파일과 대응 `.meta`:

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryBoundarySegmentId.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactBoundarySegment.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryPersistentBoundaryTree.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryPersistentAvlMap.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactRowCoverage.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactSnapshot.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactSnapshotBuilder.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactStore.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactApplySession.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactApplyMetrics.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactCommitResult.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryBoundarySplice.cs`

stable identity와 compact source 연동을 위해 수정할 기존 Domain 파일:

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryBoundaryLoopIndex.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionPlan.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionSession.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkBoundaryEdit.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionMaterialization.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionMaterializationSession.cs`

신규 테스트와 대응 `.meta`:

- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryCompactSnapshotTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryCompactStoreTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryCompactApplySessionTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryPersistentBoundaryTreeTests.cs`

수정할 기존 테스트:

- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryBoundaryLoopIndexTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkExpansionSessionTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkExpansionMaterializationSessionTests.cs`

문서:

- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- 이 작업 문서

구현 조사 결과 위 파일만으로 stable ordering, exact endpoint residual 또는 persistent
row union을 완성할 수 없으면 예약하지 않은 파일을 변경하기 전에 이 문서의 정확한
경로를 갱신하고 작업자의 다음 진행 요청과 원격 예약 검증을 다시 거친다.

## 예약 Scene·Prefab·Data Asset

없음. Scene, Prefab, Material, Shader, Compute Shader, ScriptableObject, asmdef, Package,
ProjectSettings와 Inspector 참조를 변경하지 않는다. Unity가 자동 갱신하는
`ProjectIO.slnx`와 생성 `.csproj`는 예약·커밋하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- C001-C009 fixed 좌표, Chunk ownership, Trail, C006/C007 legacy shadow state/복제와
  exact materialization 계약은 호환 상태로 유지한다.
- 새 C010은 `persistent compact Territory state and atomic splice apply` 계약이다.
- compact Boundary는 canonical 방향, stable segment identity, exact fixed endpoint와
  결정적 loop order를 가진다. 연속 sequence 값은 저장 계약이 아니며 순서 변경이
  없는 segment는 확장 뒤에도 identity와 저장 node를 공유한다.
- per-Chunk Boundary candidate index와 loop 면적 prefix는 persistent Boundary 변경과
  함께 증분 갱신된다. 다음 C008 session은 전체 loop 재구축 없이 compact snapshot의
  index를 직접 사용한다.
- 내부 Full state는 Y별 정렬·비중첩·최대 병합 inclusive run으로 저장한다. C009 run
  union은 해당 Y row만 갱신하며 면적만큼 Chunk 객체를 만들지 않는다.
- Boundary splice는 제거 arc의 stable identity, 접점에서 남는 exact endpoint residual,
  새 Trail part를 한 transaction으로 검증한다. retained Boundary 전체를 복사·반전·
  재번호화하지 않는다.
- `TryStep(maxWorkUnits)` apply candidate는 양수 budget 이하의 결정적 primitive work만
  수행하고 expected source revision과 session이 맞을 때 terminal에서만 다음 immutable
  revision을 공개한다.
- C006 초기 state에서 compact state로의 최초 변환 비용과 정상 증분 apply 비용을
  metrics에서 분리한다. 정상 apply의 source-wide Boundary/Full scan, global renumber와
  unchanged-node copy count는 0이어야 한다.

## 네트워크·Peer 동등성

이번 slice는 Fusion 상태, RPC, Authority, owner prediction, confirmed Trail과 실제
확장 표시를 변경하지 않는다. Legacy polygon과 C006/C007 shadow 경로가 계속 runtime
결과를 소유하므로 `photon-fusion-feature` 적용 범위가 아니다.

동일 compact source revision과 C008/C009 입력은 Host/Client 역할과 무관하게 같은
stable Boundary splice, Full run state, next revision과 metrics 또는 같은 실패 reason을
만들어야 한다. State Authority 적용, bounded delta 복제와 양쪽 Peer 표시 동시성은
후속 runtime/Fusion milestone에서 별도 예약하고 실제 Host·Client로 검증한다.

## 다른 활성 작업과 겹치는 부분

`CheckStart` 결과 local/upstream은 `1e9b401`에서 동기화됐고
`Docs/Work/Active/`에는 안내용 README 외 예약이 없어 겹침이 없다.

## 범위 밖

- C006 `TerritoryChunkSnapshot`, `TerritoryChunkStore`, C007 transfer/replica 삭제·교체
- compact state의 Fusion RPC, packet codec와 State Authority runtime 연결
- Job System/Burst worker, Unity frame scheduler와 main-thread publish adapter
- `TerritorySystem`, Legacy `Territory`, mesh, renderer와 consumer runtime 연결
- GPU, Shader, Compute Shader와 CPU fallback
- Scene, Prefab, Inspector, Bootstrapper와 serialized reference 변경
- authoritative cutover와 Legacy polygon/vertex RPC 제거
- Late Join, reconnect, AOI, 다수 Peer와 보안 확장

## 완료 조건

- C006 snapshot의 최초 compact 변환이 exact Boundary/Full 의미를 보존하고 canonical
  Boundary identity와 merged row run을 만든다.
- C009 materialization apply가 제거 arc, endpoint residual과 새 Trail을 exact fixed
  좌표로 splice하고 source 전체 Boundary/Full state를 순회·복사·재번호화하지 않는다.
- 변경되지 않은 Boundary tree node, Chunk index branch와 Full row branch는 구조적으로
  공유되고 source snapshot은 다음 revision 공개 뒤에도 immutable하다.
- 다음 C008 index/session과 C009 materialization이 compact result에서 직접 시작해
  최소 100회의 연속 확장을 수행하며 매 회 전체 rebuild 없이 동일한 정확도와
  결정적 결과를 유지한다.
- Full query, Boundary candidate query와 containment용 Chunk fill query가 expansion
  chain 길이에 비례해 느려지는 parent-delta 탐색을 사용하지 않는다.
- `TryStep(maxWorkUnits)`가 호출 budget을 넘지 않고 budget 1 반복과 큰 budget 실행의
  최종 snapshot, commit delta와 algorithmic metrics가 동일하다.
- stale revision, duplicate/stale identity, malformed/non-contiguous splice, overflow,
  Abort와 중간 실패가 candidate를 소각하고 기존 revision을 보존한다.
- 1000×1000 world와 장거리 Trail stress에서 정상 apply의 source-wide scan,
  global renumber, unchanged Full expansion과 unchanged node copy metrics가 0이며 저장량과
  작업량이 변경 Boundary/row run에 비례한다.
- 기존 C001-C009 회귀와 신규 순수 테스트, `dotnet build ProjectIO.slnx`,
  `git diff --check`가 통과하고 Scene·Prefab·설정·Shader·asmdef diff가 없다.

## 실제 변경

예약 단계. 구현 후 기록한다.

## 검증 결과

예약 단계. 구현 후 기록한다.

## 남은 위험

- 이번 slice는 반복 확장을 감당할 정확한 저장 기반까지이며 실제 gameplay frame과
  네트워크 체감은 아직 바뀌지 않는다. 후속 Job/Burst scheduling, State Authority
  적용, compact delta와 exact presentation이 연결돼야 제품 체감 개선이 완성된다.
- 무한한 입력을 계산 시간 0으로 만들 수는 없다. 이번 목표는 전체 크기와 확장 횟수에
  따른 불필요한 재계산을 제거하고 작업량 상한을 나눌 수 있게 하는 것이며, 실제
  budget과 완료 latency는 후속 runtime profile로 정한다.
- persistent balanced structure가 한 방향으로 퇴화하거나 expansion chain query가
  선형 누적되지 않음을 stress metrics와 테스트로 반드시 증명한다.
