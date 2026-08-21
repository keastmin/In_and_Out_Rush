# Current Milestone

Status: Complete

## Objective

State Authority가 현재 Legacy 확장 경로와 함께 Chunk Trail session을 shadow로
기록한다. 성공 재진입에서는 fixed sample 순서를 비교하고, 자기 교차·Lifeline과
외부 강제 이동 중단/재개에서는 Commit/Abort 수명을 확인한다. Shadow 결과는
진단 전용이며 Territory, RPC, 표시와 consumer 결과를 변경하지 않는다.

## Prerequisites

- Active reservation `W-20260821-011-chunk-territory-shadow-trail`
- implementation base `44578520a600abc16b767e1718da3fcbb05577cb`
- `CONTRACTS.md`의 C001-C004가 Approved일 것
- 첫 도메인 마일스톤 commit `3ee8d2a`

## Read first

- `AGENTS.md`
- `Docs/Features/Territory.md`
- `Docs/Work/Completed/W-20260821-011-chunk-territory-shadow-trail.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `.agents/skills/build-chunk-territory/SKILL.md`

## Allowed files

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Domain/TerritoryExpansionSession.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailShadowRecorder.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/`의 shadow comparer 파일
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/`의 shadow comparer 테스트
- `Docs/LongRunning/Territory/`
- `Docs/Features/Territory.md`
- `Docs/Work/Completed/W-20260821-011-chunk-territory-shadow-trail.md`

## Reserved Scene·Prefab·Data Asset

없음.

## Prohibited changes

- Legacy polygon, 계산용 path 단순화, 교차 판정과 consumer 결과 변경
- 신규 Fusion RPC, Networked state와 NetworkObject
- Scene, Prefab, Material, Shader, Compute Shader와 ScriptableObject
- LineRenderer/Trail presentation, Territory consumer와 Bootstrapper

## Required behavior

- Editor/Development Build의 State Authority만 Legacy point와 같은 순서의 shadow
  sample을 기록한다.
- Proxy의 기존 path RPC는 shadow recorder를 실행하지 않는다.
- 성공 재진입은 shadow를 Commit하고 양자화된 Legacy 순서와 비교한다.
- 비교는 point 수와 첫 mismatch index를 결정론적으로 제공한다.
- 자기 교차, Lifeline, Stop과 teardown은 active shadow session을 Abort한다.
- 외부 강제 이동 pause 중에는 sample을 추가하지 않고 resume 강제점을 이어 기록한다.
- shadow 실패와 mismatch는 진단만 남기며 Legacy 결과를 바꾸지 않는다.

## Acceptance criteria

- comparer와 기존 ChunkDomain EditMode 테스트 통과
- recorder, Legacy session과 `TerritorySystem` 통합 컴파일 통과
- Host 로컬/Client Input Authority 정상 재진입과 실패·중단 경로 절차 기록
- 신규 RPC/Networked/Spawn과 Scene·Prefab·직렬화 Asset diff 없음
- `.meta` pairing 통과
- `git diff --check` 통과

## Current evidence

- Unity 6000.0.69f1 격리 프로젝트 EditMode 21/21 통과
- 실제 수정된 recorder, Legacy session과 `TerritorySystem` 통합 컴파일 0 error
- 신규 Asset/meta pairing, GUID와 신규 RPC/Networked diff 검사 통과
- `git diff --check` 통과
- 작업자가 안내된 Host·Client 정상 재진입, 자기 교차/Lifeline과 SandTomb
  pause/resume 런타임 절차 완료를 보고

## Rollback

`TerritorySystem`의 shadow hook과 read-only Legacy path seam, 신규 recorder,
comparer/test와 이번 문서 기록만 제거한다. 기존 Legacy path, RPC와 Territory
결과는 변경 전 경로 그대로 남는다.

## Out of scope

owner-predicted/confirmed Trail 전송·표시, Territory Chunk 저장/fill/revision,
GPU 렌더링, consumer migration과 Legacy cutover.
