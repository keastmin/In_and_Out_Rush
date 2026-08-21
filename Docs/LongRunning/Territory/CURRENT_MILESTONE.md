# Current Milestone

Status: Complete

## Objective

State Authority에 revision 기반 sparse Chunk Territory shadow 저장소를 추가한다.
저장소는 전역 polygon을 보관하지 않고 Chunk별 `Empty`, `Full`, `Boundary`를
소유하며, 정상 빌드가 끝난 snapshot만 원자적으로 Commit한다.

Legacy polygon, 확장 판정, vertex RPC, mesh와 consumer callback은 계속 유일한
게임 권위 경로다.

## Prerequisites

- Active reservation `W-20260822-001-chunk-territory-state-commit`
- implementation base `908b09cc307f8d366cfd3c9517ec6567a0fa2400`
- `CONTRACTS.md`의 C001-C006이 Approved일 것
- predicted/confirmed Trail milestone commit `6ba172f`

## Read first

- `AGENTS.md`
- `Docs/Features/Territory.md`
- `Docs/Work/Completed/W-20260822-001-chunk-territory-state-commit.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## Allowed files

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- 예약된 `Assets/02_Scripts/Territory Refactor/ChunkDomain/` 신규 상태 파일
- 예약된 `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/` 신규 테스트 파일
- `Docs/Features/Territory.md`
- 이번 milestone 장기 문서와 Active 작업 문서

## Reserved Scene·Prefab·Data Asset

없음. Inspector 참조와 직렬화 Asset을 변경하지 않는다.

## Prohibited changes

- Legacy polygon, 확장 판정, vertex RPC와 consumer callback의 권위 전환·삭제
- changed-Chunk Fusion 복제, recovery와 Late Join snapshot
- GPU/CPU presentation, consumer migration
- Scene, Prefab, Bootstrapper, asmdef, Package와 Fusion 설정 변경

## Required behavior

- sparse map에 없는 Chunk는 `Empty`, 전체 내부 Chunk는 payload 없는 `Full`,
  경계를 포함하는 Chunk는 방향성 fixed local 선분을 가진 `Boundary`다.
- fixed 양자화 뒤 추가 tolerance, vertex budget 단순화나 점 이동을 적용하지 않는다.
- snapshot은 immutable이고 revision은 초기 성공 1부터 단조 증가한다.
- Commit은 expected base revision이 현재 revision과 일치할 때만 성공한다.
- invalid polygon, stale base, overflow와 빌드 실패는 이전 snapshot을 보존한다.
- changed-Chunk는 생성·변경 상태와 `Empty` tombstone을 결정적 순서로 제공한다.
- State Authority만 초기화와 Legacy 정상 확장 성공 뒤 shadow Commit을 한 번 실행한다.
- shadow 실패는 Legacy 성공 결과나 Host·Client presentation을 바꾸지 않는다.

## Acceptance criteria

- 음수 좌표, half-open 경계, Full/Boundary/Empty와 fixed local 경계 보존 테스트 통과
- 초기 revision, 연속 Commit, 동일 상태 빈 delta, stale/invalid/overflow 실패 원자성 통과
- State Authority 초기화·정상 확장 뒤 한 번만 Commit하고 실패/Abort에는 증가하지 않음
- Legacy Territory, W-012 Trail, RPC, mesh와 consumer 결과 회귀 없음
- ChunkDomain EditMode 테스트와 Fusion Weaver 포함 프로젝트 compile 통과
- Host·Client 정상 확장·거부/Abort 수동 절차 또는 정확한 미검증 기록
- `.meta` pairing, `git diff --check`, Scene·Prefab·설정 diff 없음

## Rollback

`TerritorySystem`의 shadow store hook과 신규 Chunk state/store/test, C006 문서만
제거한다. Legacy polygon과 기존 RPC/consumer 경로는 변경 없이 계속 동작한다.

## Out of scope

Chunk 기반 확장 후보 계산, Legacy 256 vertex 보정 제거, changed-Chunk replication,
Late Join 복원, GPU, consumer cutover와 Legacy 삭제.

## Current evidence

- 예약된 신규 Chunk state/store 테스트 9개를 실제 신규 소스와 NUnit assertion으로
  직접 실행해 모두 통과했다.
- 실제 신규 소스 wildcard domain compile과 `Assembly-CSharp` 통합 compile 0 errors.
- 1000×1000 fixed 사각형은 15,875 Chunk를 6~11ms, 256점·반경 500 world-unit
  원형은 12,532 Chunk를 17~20ms에 빌드했다. 측정은 로컬 .NET Debug/Release
  validation이며 Unity Profiler 수치가 아니다.
- 작업자가 Unity import/compile과 `ProjectIO.Territory.Tests` 전체 실행 완료를
  보고했다.
- 작업자가 Host 로컬/Client Input Authority 정상·연속 확장, 실패·Abort와
  revision 단일 증가 runtime 절차 완료를 보고했다.
