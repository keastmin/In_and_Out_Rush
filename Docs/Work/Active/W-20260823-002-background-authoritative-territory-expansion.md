# W-20260823-002 백그라운드 권위 영역 확장

Status: Reserved

## 동기화 기준

- Base Commit: b807704341af7dcd2c39d1de85971efdbaa44011
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex `/root`

## 기능

Territory 영역 확장 계산, 완료 결과의 State Authority 공개와 두 Peer 표시 동기화

## 목표

- Runner가 Territory에 정상 재진입하면 기존 exact compact 계산 입력을 State Authority의
  단일 background CPU worker에 넘기고 게임 simulation과 rendering을 계속 진행한다.
- 계산이 끝나기 전에는 마지막으로 완료된 Territory가 판정·표시·consumer의 유효 상태로
  남고, 부분 candidate나 중간 mesh는 공개하지 않는다.
- worker에서 exact fixed Boundary 결과와 결정적인 triangle index까지 준비한다. main
  thread에서는 Unity/Fusion API가 필요한 최종 상태 교체, Mesh upload와 완료 알림만 한다.
- State Authority와 Proxy는 같은 완료 revision의 vertex/triangle 결과를 bounded packet으로
  받은 뒤 원자 적용한다. 한 번의 거대한 RPC와 Proxy main-thread 재삼각분할은 사용하지 않는다.
- 실패, stale revision, teardown과 취소는 이전 완료 Territory를 보존하며 결과가 중복 적용되지
  않는다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/Features/PlayerRunner.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- `manage-feature-work`
- `build-chunk-territory`
- `photon-fusion-feature`

## 예상 수정 코드

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory/Territory.cs`
- `Assets/02_Scripts/Territory/TerritoryVisible.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Unity/TerritoryCompactExpansionShadow.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryExpansionReplication.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactExpansionWorker.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactExpansionWorkResult.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactCommitResult.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryBoundaryLoopIndex.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryPersistentBoundaryTree.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionMaterializationSession.cs`
- 신규 exact presentation data/builder 및 bounded replication packet/stream 파일과 대응 `.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryCompactExpansionWorkerTests.cs`
- 신규 presentation/replication 회귀 테스트 파일과 대응 `.meta`
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- 이 작업 문서

## 예약 Scene·Prefab·Data Asset

없음. Inspector 참조 추가나 Scene·Prefab 변경 없이 코드에서 기존 Territory 연결을 전환한다.

## 공용 계약 또는 Bootstrapper 변경

- C011 background compact shadow를 실제 State Authority 완료 결과의 계산 원본으로 전환하는
  새 계약을 추가한다.
- 완료 결과는 `source revision + result revision + exact vertices + triangle indices`의
  immutable payload이며 terminal 전에는 읽을 수 없다.
- Legacy `Territory`, Mesh와 기존 consumer event는 한 번의 main-thread publication에서 같은
  완료 revision으로 갱신한다.
- 기존 동기 Legacy polygon 계산과 결과를 새 경로와 동시에 실행하지 않는다.

## 네트워크·Peer 동등성

- Input Authority는 기존과 같이 로컬 Trail을 즉시 표시하고 State Authority만 확장 요청을
  검증·계산·확정한다.
- Host-local Runner와 Client Runner 모두 같은 confirmed Trail과 background queue 계약을
  사용하며 Host 역할 중첩으로 두 번 계산하거나 두 번 반영하지 않는다.
- State Authority는 완료된 결과만 packet당 고정 상한과 tick당 발송 상한을 지켜 Proxy로
  보낸다. Proxy는 revision, packet sequence, payload 길이와 triangle index를 검증하고
  terminal까지 기존 Territory를 유지한다.
- 두 Peer는 같은 완료 revision을 원자 적용하고 Proxy는 수신 vertex를 다시 삼각분할하지
  않는다.
- 이 게임의 현재 2인 단일 스테이지 범위만 다루며 Late Join, reconnect, AOI recovery,
  보안 확장은 추가하지 않는다.

## 다른 활성 작업과 겹치는 부분

없음. `Docs/Work/Active/README.md` 외 다른 Active 예약이 없다.

## 범위 밖

- 영역 모양 단순화, tolerance 확대, point 제거·이동
- GPU, Burst, Job System 또는 CPU/GPU fallback
- Trail 기록·renderer의 C012 계약 변경
- 전체 Territory consumer를 compact query로 교체하는 작업
- Grid, Fog, Resource, Monster 등 consumer 내부 비용 최적화
- Scene, Prefab, Inspector, ScriptableObject, asmdef, Package와 ProjectSettings
- Late Join, reconnect, AOI, 다수 Peer와 보안 확장

## 완료 조건

- 영역 재진입 frame에서 Legacy `TryExpand`, polygon validation, 전체 삼각분할과 C006 전체
  rebuild를 동기 실행하지 않고 background work만 예약한다.
- background 계산 중 이동·렌더링·네트워크 simulation이 계속되고 마지막 완료 Territory가
  양쪽 Peer에 유지된다.
- worker가 exact fixed Boundary를 순서대로 내보내고 triangle index를 준비하며, 입력 모양을
  단순화하거나 point를 이동·삭제하지 않는다.
- main-thread publication은 한 frame에 최대 한 완료 revision만 처리하고 계산·검증·삼각분할을
  다시 하지 않는다. Unity Mesh upload와 Legacy 상태/consumer 알림은 정확히 한 번 수행한다.
- 완료 payload는 여러 bounded packet/tick으로 전송되며 Proxy는 전체 terminal 검증 뒤 같은
  vertices/triangles를 원자 적용한다.
- 연속 확장은 source revision 순서대로 직렬화되고 실패·stale·teardown 시 부분 결과와 대기
  전송을 소각한 채 마지막 완료 상태를 보존한다.
- Domain과 packet 회귀, Unity compile, Territory EditMode, Host-local/Client Runner 연속 확장,
  계산 중 움직임·프레임 체감과 Profiler marker를 검증한다.
- `git diff --check`와 `git status`로 예약 밖 변경이 포함되지 않았음을 확인한다.

## 실제 변경

- `TerritoryCompactExpansionWorker`가 C008-C010 candidate 뒤 ordered exact fixed Boundary,
  triangle index와 최대 48-word packet을 같은 background thread에서 준비하도록 확장했다.
- `TerritoryExpansionPresentationData`, `TerritoryCompactPresentationBuilder`, result packet,
  packetizer와 atomic replica를 ChunkDomain에 추가했다.
- 청크 분할로 생긴 exact collinear Boundary point를 삭제하지 않고, terminal collinear
  remainder에는 zero-area triangle을 허용해 모든 vertex를 보존한다.
- `TerritoryExpansionReplication`을 완료 result의 source/revision/count/sequence 검증,
  tick당 최대 2 data packet outbound와 terminal inbound apply 흐름으로 교체했다.
- `TerritorySystem` 재진입은 동기 Legacy `TryExpand`, 확장 뒤 C006 전체 rebuild와 vertex
  RPC를 실행하지 않고 background work만 예약한다.
- State Authority는 frame당 한 완료 revision을 Legacy Territory/Mesh/consumer에 한 번
  적용하고 Proxy는 전체 terminal 뒤 같은 vertex/triangle을 재삼각분할 없이 적용한다.
- Scene·Prefab·Inspector와 Trail C012 경로는 변경하지 않았다.
- C013 계약, Territory 기능 문서와 milestone/handoff/roadmap/test matrix를 갱신했다.
- 기울어진 Boundary와 Trail의 수학적 교차점이 fixed 정수 좌표 사이에 있을 때 독립 축 반올림
  결과가 원래 선분에서 벗어나는 runtime 결함이 확인되었다. 일반 tolerance나 임의 스냅이
  아니라 기존 `FixedTerritoryPoint` 양자화 계약인 축별 최대 0.5 fixed unit만 접점으로
  인정하도록 경계 면적·materialization 검증을 일치시킨다. 입력 Trail point는 이동·삭제하지
  않고 이미 산출된 fixed 접점도 다시 이동하지 않는다.

## 검증 결과

- `VerifyReservation`: implementation base
  `ad402ea031a940ba743b029be2cf5ea17970ba8d`, 원격 Active 예약과 동기화 확인.
- 신규 presentation/replica assertion 3개와 변경 worker assertion 2개 직접 실행 5/5 통과.
- ordered Boundary vertex 수·순서 보존, `(N-2)*3` triangle, 48-word 다중 packet exact
  round-trip, terminal 전 revision 보존과 packet gap Abort 통과.
- 신규 source를 validation target으로 명시한 `ProjectIO.Territory.ChunkDomain.Tests.csproj`
  compile 오류 0개, warning 0개.
- 같은 source를 포함한 `Assembly-CSharp.csproj` compile 오류 0개, 기존 warning 16개.
- Unity import/Fusion Weaver와 실제 Host-local/Client Runner runtime/Profiler는 작업자 검증
  전이므로 완료로 표현하지 않는다.

## 남은 위험

- Unity Mesh upload와 기존 consumer callback은 Unity main thread에서 실행해야 한다. 이번
  작업은 기하 계산·검증·삼각분할·Proxy 재계산을 제거하고 최종 반영만 남기지만, 실제
  Profiler에서 Mesh upload 또는 특정 consumer가 별도 spike로 확인되면 그 지점은 후속의
  독립된 작은 작업으로 다룬다.
- exact 결과 크기와 계산 총 latency는 경계 정점 수에 비례한다. background 실행은 frame
  정지를 막지만 결과가 완성되는 실제 시간 자체를 0으로 만들지는 않는다.
- 결과가 완성된 뒤 fixed vertex를 Legacy `Vector2`로 복사하고 Unity Mesh를 upload하며 기존
  consumer callback을 호출하는 최종 경계는 main thread다. 이 중 실제 spike가 확인되면
  해당 presentation 또는 consumer 하나만 후속 작업으로 분리한다.
- 고정 2인 시작 동시 접속 계약만 지원하며 Late Join/reconnect result recovery는 없다.
- 정수가 아닌 실제 교차점을 `FixedTerritoryPoint`로 공개하는 과정에는 축별 최대 0.5 fixed
  unit(월드 단위 약 0.001953)의 표현 오차가 필연적으로 존재한다. 이 범위를 넘는 접점은
  계속 거부하며 기존 Boundary나 Trail을 임의로 보정하지 않는다.
