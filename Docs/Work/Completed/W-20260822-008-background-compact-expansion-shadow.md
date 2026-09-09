# W-20260822-008 백그라운드 정밀 확장 Shadow

Status: Complete

## 동기화 기준

- Base Commit: 67049bace6fa587c7fc1c9c9b2d2d9047a4e297c
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

C008 exact expansion plan, C009 compact materialization과 C010 persistent apply를
State Authority의 단일 백그라운드 CPU 작업 큐에서 순서대로 실행하고, 메인 스레드에는
bounded 수집·완료 반영과 profile만 남기는 runtime shadow 연결.

## 목표

최상위 제품 목표는 다음으로 고정한다.

- Runner가 그린 fixed Trail과 확장 Boundary를 단순화하거나 격자 모양으로 바꾸지 않는다.
- 긴 Trail과 넓은 기존 Territory의 계산이 재진입 프레임의 메인 스레드를 막지 않는다.
- 여러 확장 요청이 앞 계산보다 먼저 들어와도 source revision 순서대로 직렬 처리하고,
  이전 전체 Boundary나 Full Chunk를 다시 펼치지 않는다.
- Host Runner와 Client Runner 중 누가 이동했는지와 무관하게 State Authority가 동일한
  confirmed Trail fragment를 한 번만 계산한다.
- 실패, 취소, teardown과 stale 결과는 Legacy gameplay나 마지막 compact snapshot을
  변경하지 않는다.

현재 exact C008-C010은 `decimal`, managed collection과 immutable persistent tree를
사용하므로 Unity Burst Job에서 그대로 실행할 수 없다. 동일 알고리즘을 Native 자료형으로
복제하면 두 계산 경로의 모양·overflow·revision 계약이 달라질 위험이 있다. 이번 slice는
가짜 Job/Burst wrapper나 CPU fallback 이중 경로를 만들지 않고, 순수 Domain 계산을
백그라운드 CPU worker의 유일한 compact 계산 경로로 사용한다. Unity Profiler와 worker
metrics로 실제 wall latency와 메인 스레드 비용을 측정한 뒤, 병목이 입증된 독립 배열
연산만 후속 milestone에서 Burst 후보로 분리한다.

이번 slice는 shadow profile까지만 수행한다. Legacy polygon, 현재 mesh, consumer callback,
vertex RPC와 C006/C007 복제가 계속 gameplay 결과와 양쪽 Peer 표시를 소유한다.

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
- `Docs/Work/Completed/W-20260822-007-persistent-compact-territory-store.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/build-chunk-territory/references/work-session-protocol.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

신규 순수 Domain worker 파일과 대응 `.meta`:

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactExpansionWorkItem.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactExpansionWorkResult.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactExpansionWorker.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactExpansionWorkerMetrics.cs`

신규 State Authority shadow adapter와 폴더 `.meta`, 파일 `.meta`:

- `Assets/02_Scripts/Territory Refactor/Adapters/Unity/TerritoryCompactExpansionShadow.cs`

confirmed Trail fragment의 증분 drain과 runtime 연결을 위해 수정할 기존 파일:

- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailShadowRecorder.cs`
- `Assets/02_Scripts/Territory/TerritorySystem.cs`

신규 테스트와 대응 `.meta`:

- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryCompactExpansionWorkerTests.cs`

필요할 때 수정할 기존 회귀 테스트:

- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryCompactStoreTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailSessionTests.cs`

문서:

- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- 이 작업 문서

구현 조사 결과 비동기 완료를 안전하게 publish하려면 C008-C010 공개 계약 파일 변경이
필요하거나 예약하지 않은 공용 파일이 필요하면, 해당 파일을 변경하기 전에 이 예약을
정확한 경로로 갱신하고 작업자의 다음 진행 요청과 원격 예약 검증을 다시 거친다.

## 예약 Scene·Prefab·Data Asset

없음. Scene, Prefab, Material, Shader, Compute Shader, ScriptableObject, asmdef, Package,
ProjectSettings와 Inspector 참조를 변경하지 않는다. Unity가 자동 갱신하는
`ProjectIO.slnx`와 생성 `.csproj`는 예약·커밋하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- C001-C010 fixed 좌표, Chunk ownership, Trail sequence, exact Boundary와 persistent
  snapshot 계약을 변경하지 않는다.
- 새 C011은 `ordered background compact expansion shadow` 계약이다.
- State Authority가 이미 확정한 `TerritoryTrailFragment`를 이동 중 증분 수집한다.
  재진입 시 전체 긴 Trail을 메인 스레드에서 한 번 더 복사하지 않고 현재 fragment
  collection의 소유권을 work item으로 넘긴다.
- worker는 요청을 source revision 순서대로 하나씩 처리한다. 각 요청은 현재 immutable
  compact snapshot에서 C008 plan, C009 materialization과 C010 candidate를 만들며 다음
  요청은 직전 성공 candidate를 source로 사용한다.
- worker thread에서는 Unity API, Fusion API, Scene object, Legacy `Territory`와 renderer에
  접근하지 않는다. 순수 immutable 입력과 C008-C010만 사용한다.
- 완료 결과는 thread-safe queue를 통해 메인 스레드가 revision 순서대로 최대 한 건씩
  poll한다. source object/revision이 일치할 때만 `TerritoryCompactStore`에 원자 publish한다.
- work failure, cancellation, stale completion과 teardown은 pending request/result를
  소각하고 마지막 published compact snapshot을 보존한다.
- 초기 C006 snapshot의 compact 변환은 최초 설정 시 한 번만 허용한다. 정상 확장은
  C006 개별 Full Chunk나 전역 sequence로 돌아가 재동기화하지 않는다.
- main-thread fragment 수집량, schedule/poll/publish 시간, background C008/C009/C010
  work unit, queue depth와 end-to-end latency를 구분해 profile한다.
- 이번 계약은 single exact CPU worker만 사용한다. GPU fallback, CPU/GPU 이중 실행과
  exact geometry를 복제하는 Burst 경로를 추가하지 않는다.

## 네트워크·Peer 동등성

- 입력 원점은 기존과 같이 Host 또는 Client의 Runner Input Authority다. local owner
  Trail prediction과 confirmed Trail RPC 계약은 변경하지 않는다.
- State Authority는 기존 confirmed sample/fragment를 생성하고 C011 work item을 한 번만
  enqueue한다. Proxy나 Input Authority가 authoritative compact state를 직접 계산·변경하지
  않는다.
- 이번 shadow 결과는 새 RPC나 Networked state로 복제하지 않는다. 기존 Legacy polygon,
  vertex RPC와 C006/C007 shadow replica가 Host·Client의 현재 성공 결과와 표시를 계속
  담당하므로 플레이 기능 가능 여부와 표시 결과가 Peer 역할에 따라 바뀌지 않는다.
- Host가 server와 local Client 역할을 함께 수행해 worker enqueue/publish를 두 번 하지
  않는지 확인한다. Client Runner 경로도 State Authority에서 동일한 request 하나로
  들어오는지 확인한다.
- worker 실패·지연은 요청 Peer의 Trail, 확장 성공, 실패 피드백을 막지 않는다. Debug
  profile만 실패 원인을 기록하고 Legacy 결과를 되돌리지 않는다.
- 제품 범위에 없는 Late Join, reconnect, AOI와 보안 확장은 추가하지 않는다. teardown과
  한 Peer 이탈에 따른 stage 종료에서는 worker cancellation과 결과 소각만 검증한다.
- 실제 Host-local Runner와 Client Input Authority Runner 각각에서 걷기·달리기, 장거리
  Trail, 연속 확장, 자기 선 교차 Abort를 수행해 visible Legacy 체감이 기존과 같고
  State Authority enqueue/publish가 한 번뿐인지 작업자가 runtime으로 확인한다.

## 다른 활성 작업과 겹치는 부분

`CheckStart` 결과 local/upstream은 `67049ba`에서 동기화됐고
`Docs/Work/Active/`에는 안내용 README 외 예약이 없어 겹침이 없다.

## 범위 밖

- compact result를 authoritative gameplay state로 전환
- compact changed-state의 Fusion RPC, packet codec와 Proxy replica
- exact Chunk mesh/renderer와 GPU 표시 가속
- containment, Grid, Fog, Resource, Monster 등 consumer 전환
- Legacy polygon 계산, vertex RPC, C006/C007 store·replication 삭제 또는 우회
- Native container 기반 geometry 재구현과 Burst compile
- Scene, Prefab, Inspector, Bootstrapper, asmdef, Package와 ProjectSettings 변경
- Late Join, reconnect, AOI, 다수 Peer와 보안 확장

## 완료 조건

- 초기 C006 snapshot을 한 번만 C010 compact store로 변환하고 이후 정상 요청은 persistent
  candidate chain에서만 이어진다.
- confirmed Trail fragment를 이동 중 증분 drain하여 재진입 프레임에 전체 긴 Trail
  복사나 전체 Boundary rebuild를 하지 않는다.
- background worker가 C008 -> C009 -> C010을 source revision 순서대로 처리하고 동일
  입력에 기존 동기 순수 테스트와 exact snapshot, area, splice, metrics가 일치한다.
- 최소 100개의 연속 expansion request가 이전 요청 완료를 기다리지 않고 enqueue되어도
  queue 순서와 revision이 유지되고 마지막 결과가 동기 100회 기준과 일치한다.
- 1000×1000 world와 장거리 Trail에서 정상 요청마다 source-wide Boundary/Full scan,
  global renumber, Full Chunk 전개와 unchanged-node copy metrics가 0이다.
- schedule과 completion poll/publish의 메인 스레드 작업은 전체 Territory 면적이나 누적
  expansion 수에 비례하지 않으며 Profiler marker와 worker metrics로 구분된다.
- worker failure, cancellation, stale publish, teardown과 중간 Abort가 마지막 published
  compact snapshot과 Legacy gameplay를 변경하지 않는다.
- State Authority만 work item을 한 번 enqueue하고 Host-local/Client Runner 역할에 따른
  pure result 차이가 없다.
- 기존 C001-C010 회귀와 신규 worker 테스트, Unity EditMode, 프로젝트 compile,
  `git diff --check`가 통과하고 Scene·Prefab·설정·Shader·asmdef·Package diff가 없다.
- 작업자가 실제 Host와 Client에서 각 Runner의 걷기·달리기, 긴 Trail, 연속 확장,
  자기 교차 Abort와 teardown을 검증하고 visible 기능 회귀 및 frame spike 여부를 보고한다.

## 실제 변경

- confirmed Trail fragment를 복사 없이 소유권 이전할 수 있는
  `TerritoryCompactExpansionWorkItem`을 추가했다.
- expected source revision 순서대로 요청을 직렬 처리하는 단일
  `TerritoryCompactExpansionWorker`를 추가했다. worker는 순수 C008 -> C009 -> C010을
  background thread에서 실행하고 성공 candidate를 다음 queued source로 사용한다.
- worker result와 fragment 수, elapsed time, worker thread, plan/materialization/apply
  metrics를 분리한 측정 구조를 추가했다.
- `TerritoryCompactExpansionShadow`가 최초 C006 snapshot을 한 번 compact 변환하고,
  이동 중 새 confirmed fragment만 drain하며, 완료 result를 main thread에서 한 건씩
  `TerritoryCompactStore`에 publish하도록 구현했다.
- `TerritoryTrailShadowRecorder`가 immutable confirmed fragment view를 제공하도록 했다.
- `TerritorySystem` State Authority 경로에 초기화, fragment drain, 성공 재진입 schedule,
  `Render` completion publish, Abort와 teardown cancellation을 연결했다.
- schedule/publish Profiler marker와 debug revision/fragment/work/elapsed/queue log를
  추가했다.
- 100 queued expansion ordering/reference 일치와 failure 원자성 worker 테스트를 추가했다.
- C011 계약과 기능/milestone/handoff/roadmap/test 문서를 갱신했다.

## 검증 결과

- 신규 worker assertion 2개와 기존 ChunkDomain 회귀를 직접 실행해 75/75 통과했다.
- 1000×1000 initial state에서 동기 reference로 미리 만든 100 request를 이전 완료 전에
  모두 enqueue했다. publish revision 순서와 최종 Boundary area/count/identity가 동기
  reference와 일치했다.
- 각 worker apply의 source-wide Boundary/Full scan, global renumber, Full Chunk 전개와
  unchanged-node copy metrics가 모두 0이었다.
- invalid geometry는 candidate를 publish하지 않고 worker를 fault 처리했으며 후속
  enqueue를 거부하고 initial source revision을 보존했다.
- Unity 생성 project를 수정하지 않고 신규 source를 validation target으로 명시한
  `dotnet build ProjectIO.slnx --no-restore`가 오류 0개, 기존 warning 25개로 통과했다.
- `git diff --check` 통과.
- 작업자가 Unity import/Console compile, Territory EditMode와 안내된 Host-local/Client
  Runner의 걷기·달리기·긴 Trail·연속 확장·자기 교차 Abort·teardown 및 Profiler
  runtime 검증을 완료했다.

## 남은 위험

- 백그라운드 계산은 메인 스레드 stall을 제거하는 수단이지 무한한 입력의 총 계산 시간을
  0으로 만들지는 않는다. queue latency가 실제 플레이 간격보다 길면 profile 근거로
  C009의 독립 배열 연산을 Native/Burst로 분리하거나 gameplay publish 정책을 별도
  milestone에서 결정해야 한다.
- 현재 visible 확장은 Legacy가 즉시 수행한다. exact compact 결과의 양쪽 Peer 표시와
  권위 전환은 후속 replication/presentation milestone 전까지 체감에 반영되지 않는다.
- .NET thread pool scheduling과 Unity 플랫폼별 지원은 Editor/목표 빌드 profile이
  필요하다. 지원하지 않는 플랫폼을 위한 동시 CPU/GPU fallback은 이번에 만들지 않는다.
- 현재 main thread에는 Legacy polygon 확장, C006 전체 shadow rebuild, mesh와 vertex
  RPC 비용이 남아 있다. 이번 worker만으로 현재 gameplay 전체 frame spike가 사라졌다고
  판단하면 안 되며 authoritative/consumer cutover가 후속으로 필요하다.
