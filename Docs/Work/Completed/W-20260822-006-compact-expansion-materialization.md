# W-20260822-006 압축 증분 영역 확장 Materialization

Status: Complete

## 동기화 기준

- Base Commit: 2486e54ef10c913a896be84d4dc34f2fb2d8054c
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Territory exact expansion plan의 변경 영역 전용 압축 materialization과 bounded 작업
세션.

## 목표

최상위 제품 목표는 다음으로 고정한다.

- Runner가 영역 밖에서 그린 fixed Trail 모양을 이동·삭제·근사하지 않는다.
- 기존 영역 전체나 긴 Trail 전체를 재결합하는 단일 frame 작업을 만들지 않는다.
- 넓은 내부를 Chunk별 `Full` 객체로 전개하지 않고 행 단위 연속 run으로 압축한다.
- 새 Trail과 실제로 제거되는 기존 Boundary arc만 이용해 추가 영역을 계산한다.
- 계산량은 명시적 work budget으로 분할할 수 있고 source 상태는 완료 전까지 바뀌지
  않는다.
- 같은 입력은 Host/Client 역할과 무관하게 같은 fixed 결과와 작업량을 만든다.

W-005 C008 plan에서 선택된 새 경계는 exact Trail과 유지되는 기존 Boundary arc로
구성된다. 이번 slice는 그 보완 영역, 즉 Trail과 교체되는 기존 Boundary arc가 감싼
추가 영역만 materialize한다. 전체 기존 polygon을 다시 만들거나 C006 전역 Boundary
sequence를 재번호화하지 않는다.

결과는 immutable compact materialization으로서 새 Trail Boundary, 제거할 기존
Boundary sequence 범위, 경계 Chunk의 exact local segment와 내부 Full row run을
제공한다. 이 단계는 아직 C006 snapshot/store, Legacy gameplay, renderer 또는
Fusion에 적용하지 않는다.

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
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/build-chunk-territory/references/work-session-protocol.md`

## 예상 수정 코드

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkFillRun.cs` 신규 및 `.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkBoundaryEdit.cs` 신규 및 `.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionMaterialization.cs` 신규 및 `.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionMaterializationSession.cs` 신규 및 `.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionMaterializationMetrics.cs` 신규 및 `.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkFillRunTests.cs` 신규 및 `.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkExpansionMaterializationSessionTests.cs` 신규 및 `.meta`
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- 이 작업 문서

기존 W-005 public type이나 C006 state type의 변경이 필요해지면 해당 파일을 변경하기
전에 이 예약 범위를 갱신하고 작업자의 다음 진행 요청과 원격 예약 검증을 거친다.

## 예약 Scene·Prefab·Data Asset

없음. Scene, Prefab, Material, Shader, Compute Shader, ScriptableObject, asmdef, Package,
ProjectSettings와 Inspector 참조를 변경하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- C001-C008 좌표, Chunk ownership, Trail, snapshot, 복제와 exact expansion plan 계약은
  변경하지 않는다.
- 새 C009는 `compact changed-region materialization` 계약이다.
- materialization 입력은 동일 source revision의 C006 snapshot,
  `TerritoryBoundaryLoopIndex`와 C008 plan이다. revision/session/접점 불일치는 시작 전에
  거부한다.
- 추가 영역 경계는 plan의 exact Trail과 선택되지 않아 교체되는 기존 Boundary arc만
  사용한다. 유지되는 기존 Boundary arc와 전체 source polygon을 복사·재번호화하지
  않는다.
- Boundary를 통과하는 Chunk에만 exact fixed local segment를 보관한다. 내부 Chunk는
  같은 Y의 연속 X 범위를 inclusive `Full` run 하나로 압축하며 Empty 또는 개별 Full
  payload를 만들지 않는다.
- work session의 `Step(maxWorkUnits)`는 양수 budget 이하의 결정적 primitive work만
  수행한다. 완료 전 결과를 공개하지 않고 Abort/실패는 모든 candidate를 소각한다.
- fixed 정밀도 이후 tolerance, vertex budget, raster resolution, 곡선 단순화와 점
  이동은 없다. 연속 중복점과 exact collinear 중간점만 모양 보존 정규화로 허용한다.
- CPU 순수 배열/정수 데이터와 work cursor를 먼저 고정한다. Job System/Burst adapter는
  이 결과를 그대로 병렬 실행할 후속 runtime milestone이며 이번 slice에서 Unity
  의존성을 ChunkDomain에 넣지 않는다.

## 네트워크·Peer 동등성

이번 slice는 Fusion 상태, RPC, Authority, owner prediction, confirmed Trail과 확장
표시를 변경하지 않는다. Legacy polygon이 계속 게임 결과를 소유한다.

같은 source revision/index/plan과 같은 step budget sequence를 별도 session에 적용하면
Host 역할과 Client 역할에 해당하는 호출 환경과 무관하게 byte-equivalent Boundary
edit, Full run, metrics 또는 같은 실패 reason을 만들어야 한다. 실제 State Authority
적용, 복제 delta와 양쪽 Peer 동시 표시는 후속 통합 milestone에서
`photon-fusion-feature`를 적용해 검증한다.

## 다른 활성 작업과 겹치는 부분

`CheckStart` 결과 local/upstream은 `2486e54`에서 동기화됐고
`Docs/Work/Active/`에는 안내용 README 외 예약이 없어 겹침이 없다.

## 범위 밖

- C006 `TerritoryChunkSnapshot`, `TerritoryChunkStore`와 transfer codec의 구조 변경
- compact materialization을 authoritative 또는 shadow store에 적용
- 기존 Boundary 전체를 persistent rope/tree로 교체하거나 sequence 계약 삭제
- Job System/Burst adapter, worker thread와 Unity frame scheduler 연결
- `TerritorySystem`, Legacy `Territory`, mesh, renderer와 consumer runtime 연결
- Fusion RPC/packet/revision/Authority와 Host·Client presentation 변경
- GPU, Shader, Compute Shader와 CPU fallback
- Scene, Prefab, Inspector, Bootstrapper와 serialized reference 변경
- Late Join, reconnect, AOI, 다수 Peer와 보안 확장

## 완료 조건

- 추가 영역 loop가 exact Trail과 교체되는 기존 Boundary arc만으로 닫히며 source 전체
  Boundary/Full Chunk를 순회하지 않는다.
- 직선, 대각선, 곡선 표본, concave, 음수 Chunk와 shared edge/corner에서 경계 Chunk
  local segment가 fixed 입력을 이동·삭제하지 않는다.
- 내부 Full Chunk는 `(Y, MinX..MaxX)` run으로 정렬·병합되고 동일 행의 인접 Full을
  개별 객체로 materialize하지 않는다.
- `Step(maxWorkUnits)`가 호출당 budget을 넘지 않고 작은 budget 반복과 큰 budget 한 번의
  최종 결과/metrics가 동일하다.
- 완료 전 immutable 결과를 공개하지 않으며 stale revision, plan/index mismatch,
  overflow, malformed arc, Abort와 중간 실패가 source와 published 결과를 변경하지 않는다.
- 1000×1000 world와 장거리 multi-Chunk Trail stress에서 Boundary 작업은 Trail과 교체
  arc에, 내부 저장량은 Chunk 면적이 아니라 row run 수에 비례함을 deterministic
  metrics로 검증한다.
- 큰 기존 Boundary에 작은 확장을 적용할 때 source 전체 Boundary scan/renumber count가
  0임을 검증한다.
- 동일 입력과 budget sequence의 별도 Peer-role session 결과/실패 reason이 동일하다.
- 신규 순수 테스트와 기존 ChunkDomain 회귀, `dotnet build ProjectIO.slnx`,
  `git diff --check`가 통과하고 Scene·Prefab·설정·Shader·asmdef diff가 없다.

## 실제 변경

- C009 `TerritoryChunkExpansionMaterializationSession`을 추가했다. exact Trail과 교체
  arc의 원본 edge 읽기, 긴 edge의 Chunk part 분할, scanline 교차, Boundary Chunk
  제외, row run과 immutable 결과 공개가 각각 bounded work cursor로 진행된다.
- `TerritoryChunkBoundaryEdit`는 Chunk별 새 Trail local segment, captured-region의
  교체 source arc local segment와 제거 source sequence를 분리 보존한다.
- Boundary가 없는 추가 영역 내부는 개별 `Full` coverage 대신 inclusive
  `TerritoryChunkFillRun(Y, MinX, MaxX)`으로 정렬·병합한다.
- source snapshot의 `Chunks`와 전체 Boundary를 materialization 과정에서 순회하거나
  전역 Boundary sequence를 재번호화하는 경로를 만들지 않았다.
- 완료 전 결과 비공개, Abort/중간 실패 candidate 소각과 완료 결과의 session 재사용
  불변성을 구현했다.
- C009 계약, 기능 문서, milestone/handoff/roadmap/test matrix를 현재 범위에 맞게
  갱신했다.

## 검증 결과

- 신규 source와 현재 ChunkDomain source/test 전체 직접 compile: warning 0, error 0.
- 신규 assertion 10개 포함 현재 ChunkDomain 회귀 60/60 직접 실행 통과.
- exact Trail endpoint/연속성, 교체 arc, 같은 Boundary sequence 접점, 음수·대각선,
  stale revision, Abort, 완료 전 비공개와 session 재사용 불변성 통과.
- budget 1과 10000의 최종 Boundary edit/Full run/metrics 동일성과 호출별 budget 상한
  통과.
- 1000×1000 stress에서 source Boundary full scan, source Full Chunk scan과 Boundary
  renumber metrics 0. 내부 Full Chunk 수가 run 수보다 크고 run 수는 행 수 이하임을
  확인했다.
- Unity import/compile과 안내된 Territory EditMode 전체 검증을 작업자가 완료했다.
- 신규 source를 포함한 `dotnet build ProjectIO.slnx --no-restore`는 오류 0개, 기존
  warning 21개로 통과했다.
- `git diff --check` 통과. Scene·Prefab·설정·Shader·Compute Shader·asmdef diff는
  없다.

## 남은 위험

- 이번 결과는 compact added-region candidate이므로 실제 gameplay frame 비용은 아직
  줄지 않는다. 후속 persistent store apply와 runtime scheduling이 완료돼야 체감
  개선이 생긴다.
- 임의로 무한한 Trail과 변경 영역을 계산 시간 0으로 만들 수는 없다. run 압축은
  넓은 내부의 저장·전송 단위를 줄이고 work budget은 frame spike를 제한하지만,
  실제 완료 지연은 후속 background Job/Burst와 게임 적용 정책으로 검증해야 한다.
- C006의 전역 sequence와 개별 Full sparse entry는 그대로 남는다. compact 결과를
  C006에 다시 전개하면 목표를 훼손하므로 후속 store 계약은 run과 Boundary edit를
  직접 보존해야 한다.
