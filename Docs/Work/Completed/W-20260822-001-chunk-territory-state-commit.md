# W-20260822-001 Chunk Territory 상태와 확장 Commit

Status: Complete

## 동기화 기준

- Base Commit: 6ba172f7ecbcaea766bae830391b56a9120d2a6f
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Territory, Chunk 상태 저장, State Authority 확장 Commit, 장기 Chunk Territory 전환.

## 목표

State Authority에 revision 기반 sparse Chunk Territory 저장소를 추가한다. 저장소는
전역 단일 폴리곤을 보관하지 않고 Chunk별 `Empty`, `Full`, `Boundary` 상태를
소유한다. `Empty`는 미저장으로 표현하고 `Boundary`는 C001 fixed 좌표를 Chunk
local 좌표로 변환한 방향성 경계 선분과 내부 판정 기준을 보관한다.

초기 Territory와 Legacy가 승인한 정상 확장 결과를 fixed Chunk snapshot으로
빌드한 뒤, 전체 빌드가 성공했을 때만 revision과 changed-Chunk 목록을 원자적으로
교체한다. 이번 마일스톤의 Chunk 상태는 shadow이며 Legacy polygon, 확장 결과,
vertex RPC와 consumer event가 계속 유일한 게임 권위 경로다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/build-chunk-territory/references/work-session-protocol.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkFill.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkFill.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkLocalPoint.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkLocalPoint.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkBoundarySegment.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkBoundarySegment.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkCoverage.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkCoverage.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkSnapshot.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkSnapshot.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkCommitResult.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkCommitResult.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkStateBuilder.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkStateBuilder.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkStore.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkStore.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkStateBuilderTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkStateBuilderTests.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkStoreTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkStoreTests.cs.meta`
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

- C001-C005 fixed/Chunk/traversal/Trail 계약은 변경하지 않는다.
- C006 Chunk Territory state/commit 계약을 추가한다.
- revision은 0이 아닌 단조 증가 `ulong`이며 초기 성공 snapshot은 revision 1이다.
  Commit은 호출자가 제시한 base revision이 현재 revision과 같을 때만 성공하고,
  overflow, 잘못된 polygon과 빌드 실패는 기존 snapshot을 변경하지 않는다.
- sparse map에 없는 Chunk는 `Empty`다. `Full`은 Chunk 전체가 내부이고 경계
  payload가 없다. `Boundary`는 fixed 양자화 이후 점을 이동·삭제·단순화하지 않은
  방향성 local 경계 선분과 결정적인 내부 기준을 가진다.
- snapshot은 외부에서 변경할 수 없고 changed-Chunk에는 신규/변경 상태와
  `Empty` tombstone이 결정적 Chunk 순서로 포함된다.
- Chunk builder가 받는 Legacy polygon은 Commit 동안만 쓰는 입력이며 snapshot에
  전역 polygon으로 보관하지 않는다.
- Bootstrapper, 공개 consumer callback과 기존 Legacy vertex RPC 계약은 변경하지
  않는다.

## 네트워크·Peer 동등성

- 입력 원점과 Trail 계약은 W-012 그대로다. Host 로컬과 Client Input Authority
  Runner 모두 기존 예측 Trail과 State Authority 확장 요청 경로를 사용한다.
- State Authority만 초기 Chunk snapshot과 정상 확장 후의 shadow Commit을 한 번
  실행한다. Client나 Proxy는 Chunk revision을 직접 쓰거나 Commit하지 않는다.
- 이번 마일스톤은 새 RPC나 `[Networked]` 상태를 추가하지 않는다. 실제 성공 결과,
  거부·실패와 표시 복원은 기존 Legacy polygon vertex Reliable RPC와 consumer
  event가 담당하므로 Host·Client 플레이 결과는 바뀌지 않는다.
- Chunk shadow Commit 실패는 Legacy 성공을 되돌리거나 Client 결과를 막지 않고
  State Authority 진단으로 남긴다. 실패한 revision과 부분 Chunk는 공개하지 않는다.
- Chunk snapshot의 changed-Chunk 복제, recovery와 Late Join 복원은 마일스톤 5다.
  이번 상태는 아직 게임 결과를 소유하지 않으므로 Late Join peer가 shadow
  snapshot을 갖지 않아도 권위 결과 차이는 없다.
- Host가 State/Input Authority를 함께 가져도 Commit은 State Authority 경로에서만
  한 번 실행한다. Host·Client 정상 확장, 거부/Abort, 연속 확장에서 revision이
  성공당 한 번만 증가하고 Legacy 이벤트가 중복되지 않는지 수동 확인한다.

## 다른 활성 작업과 겹치는 부분

`CheckStart`와 `Docs/Work/Active/` 확인 결과 안내용 README 외 활성 예약이 없어
겹침이 없다.

## 범위 밖

- changed-Chunk Fusion packet/RPC, recovery, AOI와 Late Join snapshot
- Chunk 상태를 게임 권위 원본으로 전환하거나 Legacy polygon/RPC를 삭제하는 작업
- Chunk 기반 확장 후보 계산, polygon boolean/union과 Legacy 256 vertex 보정 제거
- GPU buffer/mask/SDF, Material, Shader, Compute Shader와 CPU fallback presentation
- containment, Grid, Fog, Resource, Monster 등 consumer migration
- 진행 Trail transport, sampling, 자기 교차, Lifeline과 SandTomb 동작 변경
- Scene, Prefab, Bootstrapper, asmdef, Package와 직렬화 설정 변경

## 완료 조건

- 음수 좌표와 half-open Chunk 경계에서 `Empty/Full/Boundary`가 결정적으로
  분류되고 Boundary 선분이 Chunk local closed 범위에 보존된다.
- fixed polygon의 경계 순서와 좌표가 Chunk별 선분을 다시 이었을 때 추가 tolerance나
  vertex budget 보정 없이 일치한다.
- 초기 성공 snapshot은 revision 1이며 정상 Commit마다 정확히 1 증가한다.
- stale base revision, invalid/self-intersecting/degenerate polygon, revision overflow와
  빌드 실패가 이전 snapshot을 원자적으로 보존한다.
- Commit 결과가 추가/변경 Chunk와 `Empty` tombstone을 중복 없이 결정적 순서로
  제공하고 동일 polygon 재Commit은 revision 정책에 맞는 빈 delta를 만든다.
- State Authority 초기화와 Legacy 정상 확장 성공 뒤 shadow Commit이 한 번만
  실행되며 실패·Abort에는 revision이 증가하지 않는다.
- Legacy Territory, vertex RPC, mesh, consumer callback, W-012 Trail과 Host·Client
  게임 결과가 변경되지 않는다.
- ChunkDomain EditMode 테스트, 프로젝트 compile, Host·Client 수동 절차 또는 정확한
  미검증 기록, `.meta` pairing과 `git diff --check`가 완료된다.
- Scene·Prefab·Material·Shader·Compute·ScriptableObject·Fusion 설정 diff가 없다.

## 실제 변경

- sparse `Empty/Full/Boundary` coverage, fixed local point/방향성 boundary segment,
  immutable snapshot과 결정적 changed-Chunk commit result를 ChunkDomain에 추가했다.
- `TerritoryChunkStateBuilder`는 C003으로 polygon edge를 Chunk별로 분할하고 행별
  scanline 교차를 재사용해 interior `Full`을 분류한다. 전역 polygon은 빌드 입력으로만
  사용하고 snapshot에 보관하지 않는다.
- `TerritoryChunkStore`는 expected base revision, invalid polygon과 overflow를 검증한
  뒤 candidate 전체가 성공한 경우에만 snapshot을 교체한다. 동일 coverage는 빈
  delta, 제거 Chunk는 `Empty` tombstone이며 `(Y, X)` 순서다.
- `TerritorySystem`은 State Authority에서 초기 Territory와 Legacy 정상 확장 성공
  직후 shadow Commit을 한 번 실행한다. 새 RPC, Networked 상태, consumer 호출과
  Inspector 연결은 추가하지 않았다.
- C006을 Approved로 고정하고 Current Milestone, Roadmap, Handoff, Test Matrix와
  Territory 기능 문서를 갱신했다.

## 검증 결과

- 예약된 builder/store NUnit assertion 9개를 실제 신규 source로 직접 실행해 통과.
- 1000×1000 사각형 15,875 Chunk build 6~11ms, 256점·반경 500 world-unit 원형
  12,532 Chunk build 17~20ms. 로컬 .NET validation 수치이며 Unity Profiler가 아니다.
- 실제 신규 source domain compile 0 errors, `Assembly-CSharp` 통합 compile 0 errors
  (기존 warning 13개).
- 작업자가 Unity import/compile과 `ProjectIO.Territory.Tests` 전체 실행 완료를
  보고했다.
- 작업자가 Host 로컬/Client Input Authority 정상·연속 확장, 거부/Abort와
  revision 단일 증가 runtime 절차 완료를 보고했다.

## 남은 위험

- 이번 마일스톤은 Legacy polygon 결과를 shadow Chunk snapshot으로 변환하므로 기존
  256 vertex 보정과 영역 확장 hitch를 아직 제거하지 않는다.
- Chunk snapshot 복제와 Late Join 복원 전에는 Proxy가 shadow 상태를 소유하지 않는다.
- snapshot rebuild는 아직 동기식이다. 1000×1000·256점 synthetic validation에서
  17~20ms였으므로 실제 Unity Profiler 수치에 따라 후속 incremental/time-slice가
  필요할 수 있다.
