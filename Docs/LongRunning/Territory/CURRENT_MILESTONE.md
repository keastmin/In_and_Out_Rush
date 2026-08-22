# Current Milestone

Status: Complete

## Objective

C008 expansion plan의 exact Trail과 실제로 교체되는 기존 Boundary arc만 이용해
추가 영역을 bounded work session으로 materialize한다. Boundary Chunk는 fixed local
segment를 유지하고 넓은 내부 Full Chunk는 행 단위 inclusive run으로 압축한다.

이번 milestone은 순수 ChunkDomain candidate만 추가한다. C006 store 적용, Legacy
polygon과 mesh, Fusion transport와 gameplay 결과는 계속 기존 경로가 소유한다.

## Prerequisites

- Active reservation `W-20260822-006-compact-expansion-materialization`
- implementation base `82b24716535b7f4569e7536f266e610c6583bd7d`
- `CONTRACTS.md`의 C001-C009가 Approved일 것
- W-005 C008 exact expansion plan이 Complete일 것

## Read first

- `AGENTS.md`
- `Docs/Features/Territory.md`
- `Docs/Work/Active/W-20260822-006-compact-expansion-materialization.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `.agents/skills/build-chunk-territory/SKILL.md`

## Allowed files

- 예약된 `ChunkDomain` materialization/session/edit/run/metrics 신규 파일과 `.meta`
- 예약된 `ChunkDomain.Tests` 신규 테스트와 `.meta`
- Territory 기능 문서와 이번 milestone 장기 문서
- 현재 Active 작업 문서

## Reserved Scene·Prefab·Data Asset

없음. Inspector 참조와 직렬화 Asset을 변경하지 않는다.

## Prohibited changes

- `TerritorySystem`, `Territory`, `TerritoryVisible` runtime 연결
- Legacy polygon, vertex RPC, mesh와 consumer callback의 변경·삭제
- C006 snapshot/store/transfer 구조 변경과 materialization 적용
- persistent Boundary sequence 교체와 changed-Chunk scheduling
- Fusion RPC, Authority, prediction과 presentation 변경
- GPU/CPU mask, Shader, Compute Shader, Material
- Scene, Prefab, ScriptableObject, asmdef, Package와 ProjectSettings

## Required behavior

- 추가 영역 loop는 exact Trail과 교체 arc만 읽고 source 전체 Boundary/Full Chunk를
  순회하거나 전역 sequence를 재번호화하지 않는다.
- Boundary Chunk에는 정확한 local captured-region/Trail/교체 arc segment를 보존한다.
- 내부 Full Chunk는 행별 연속 X run으로 정렬·병합하고 면적만큼 객체화하지 않는다.
- `TryStep(maxWorkUnits)`는 호출 budget을 넘지 않으며 완료 전 결과를 공개하지 않는다.
- stale revision, malformed arc, overflow와 Abort는 candidate를 소각하고 source를
  바꾸지 않는다.
- 같은 입력은 budget 분할과 Peer 역할에 무관하게 같은 결과와 metrics를 만든다.

## Acceptance criteria

- 직선·대각선·concave·음수/shared 경계에서 exact Boundary part와 Full run 테스트 통과
- 작은/큰 step budget의 결과와 algorithmic metrics가 동일함
- Abort와 모든 실패가 pending result를 소각하고 기존 snapshot을 바꾸지 않음
- 1000×1000 world stress에서 Full 저장량이 면적이 아니라 row run 수에 비례함
- 큰 source Boundary의 작은 확장에서 source full scan/renumber metrics가 0임
- 기존 ChunkDomain 회귀와 프로젝트 compile, `git diff --check` 통과
- Scene·Prefab·설정·Shader·asmdef diff 없음

## Rollback

신규 순수 Domain 파일과 C009 문서 diff를 되돌리면 W-006 전 상태로 복원된다.
Runtime 연결이 없으므로 현재 Legacy gameplay에는 rollback 절차가 필요 없다.

## Out of scope

materialization의 persistent store 적용, Job/Burst와 frame scheduling, runtime/Fusion
연결, exact renderer, consumer migration, Legacy 제거, Late Join/reconnect/AOI/security
확장.

## Current evidence

- 신규 C009 source와 현재 ChunkDomain source/test 전체 직접 compile: warning 0,
  error 0.
- 신규 assertion 10개를 포함한 현재 ChunkDomain 회귀 60/60 직접 실행 통과.
- budget 1 반복과 budget 10000 실행의 Boundary edit, Full run과 algorithmic metrics가
  동일하고 모든 `TryStep` 호출이 전달 budget 이하를 사용했다.
- 1000×1000 stress에서 Full Chunk 수가 run 수보다 크고 run 수는 행 수 이하였으며
  source Boundary/Full scan과 전역 renumber metric은 모두 0이었다.
- Unity import/compile과 안내된 Territory EditMode 전체 검증을 작업자가 완료했다.
- 신규 source를 포함한 `dotnet build ProjectIO.slnx --no-restore`는 오류 0개, 기존
  warning 21개로 통과했다.
