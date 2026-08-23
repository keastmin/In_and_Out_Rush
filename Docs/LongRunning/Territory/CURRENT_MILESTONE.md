# Current Milestone

Status: Complete

## Objective

confirmed Trail fragment를 이동 중 증분 수집하고 C008 exact plan, C009 compact
materialization과 C010 persistent apply를 State Authority의 단일 background CPU queue에
연결한다. 긴 Trail과 넓은 Territory 계산을 재진입 frame의 main thread에서 분리하고,
완료 candidate만 revision 순서대로 한 frame에 최대 한 건 publish한다.

이번 milestone은 C011 runtime shadow와 profile까지만 추가한다. Legacy polygon,
C006/C007 replication, mesh, vertex RPC와 consumer가 계속 gameplay 결과를 소유한다.

## Prerequisites

- Active reservation `W-20260822-008-background-compact-expansion-shadow`
- implementation base `cff010f0631be97c8079511db68276ca9bdd6518`
- `CONTRACTS.md`의 C001-C011이 Approved일 것
- W-007 C010 persistent compact store가 Complete일 것

## Required behavior

- 최초 C006 snapshot만 한 번 compact state로 변환한다.
- confirmed fragment는 Runner 이동 중 새로 닫힌 항목만 증분 drain한다.
- terminal에서 전체 Trail을 복사하지 않고 collection ownership을 work item으로 넘긴다.
- background worker는 request와 source revision을 직렬 처리하고 직전 candidate에서
  다음 C008-C010을 이어간다.
- worker thread는 Unity/Fusion API와 mutable Scene state에 접근하지 않는다.
- main thread는 frame당 최대 한 completion을 stale 검증 뒤 원자 publish한다.
- failure, cancellation, stale result와 teardown은 마지막 compact state와 Legacy
  gameplay를 보존한다.
- exact CPU geometry 한 경로만 사용하며 Burst/GPU fallback을 만들지 않는다.

## Prohibited changes

- Legacy `Territory` authoritative 계산, mesh, vertex RPC와 consumer callback 교체
- C006/C007 active replication 삭제 또는 compact delta로 전환
- Proxy compact state와 exact presentation
- C008-C010 fixed 좌표, exact Boundary, persistent storage 의미 변경
- Native/Burst geometry 재구현, GPU/CPU mask와 Shader
- Scene, Prefab, Inspector, ScriptableObject, asmdef, Package와 ProjectSettings
- Late Join, reconnect, AOI, 다수 Peer와 보안 확장

## Acceptance criteria

- 1000×1000 초기 state와 100 queued expansion이 source revision 순서대로 완료되고
  동기 persistent chain과 최종 exact state가 일치한다.
- 매 정상 apply의 source-wide Boundary/Full scan, global renumber, Full Chunk 전개와
  unchanged-node copy metric이 0이다.
- invalid geometry, cancellation, stale publish와 teardown이 partial state를 공개하지 않는다.
- State Authority만 session당 work item을 한 번 enqueue하고 Host/Client Runner 역할에
  따라 pure result가 달라지지 않는다.
- schedule/poll/publish main-thread marker와 worker elapsed/work/queue metrics가 제공된다.
- 기존 ChunkDomain 회귀, Unity EditMode, project compile과 `git diff --check`가 통과한다.
- 실제 Host와 Client에서 각 Runner의 걷기·달리기, 긴 Trail, 연속 확장, 자기 교차 Abort,
  teardown과 Profiler를 검증한다.

## Current evidence

- 신규 worker 테스트 2개와 기존 ChunkDomain 회귀 직접 실행 75/75 통과.
- 1000×1000 initial state에서 미리 준비한 100 request를 선행 enqueue하고 revision 순서,
  동기 reference와 최종 Boundary area/count/identity 일치를 확인했다.
- 100회 모두 source-wide scan/renumber/Full 전개/unchanged copy metric 0.
- invalid geometry 뒤 candidate 비공개, worker fault와 후속 enqueue 거부, source 보존 통과.
- 신규 source를 명시적으로 포함한 `dotnet build ProjectIO.slnx --no-restore` 오류 0개,
  기존 warning 25개.
- 작업자가 Unity import/Console compile, Territory EditMode와 안내된 Host-local/Client
  Runner의 걷기·달리기·긴 Trail·연속 확장·자기 교차 Abort·teardown 및 Profiler
  runtime 검증을 완료했다.

## Rollback

신규 C011 worker/adapter/test와 `TerritorySystem` 및 Trail recorder 연결 diff를 되돌리면
W-007 상태로 복원된다. Legacy gameplay가 계속 authoritative이므로 gameplay data
migration이나 Scene rollback은 없다.

## Out of scope

compact delta 복제, exact renderer, consumer migration, authoritative cutover, Legacy
제거, Native/Burst 가속, GPU 표시, Late Join/reconnect/AOI/security.

## Next bounded milestone

작업자의 Unity 및 Host·Client Profiler 검증과 W-008 최종 Commit·Push 뒤 C010 changed
Boundary/Full row를 고정 2인 Client Proxy에 bounded delta로 복제한다. reconnect/Late Join
복잡성은 추가하지 않고 exact presentation에 필요한 현재 session 동시성만 준비한다.
