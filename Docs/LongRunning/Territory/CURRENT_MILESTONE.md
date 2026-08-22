# Current Milestone

Status: Complete

## Objective

C006 immutable snapshot의 exact fixed Boundary를 revision당 한 번 방향성 loop와
Chunk-local 후보로 색인하고, C003/C004 ordered Trail fragment를 Runner 이동 중
증분 처리해 terminal 재탐색 없이 immutable expansion plan을 만든다.

이번 milestone은 순수 ChunkDomain 계산 기반만 추가한다. Legacy polygon과 mesh,
Fusion transport와 gameplay 결과는 계속 기존 경로가 소유한다.

## Prerequisites

- Active reservation `W-20260822-005-exact-incremental-expansion-plan`
- implementation base `433c2269468e13a498caf6aa3ec08eef393fd3af`
- `CONTRACTS.md`의 C001-C008이 Approved일 것
- W-004 GPU mask 실험이 superseded 상태로 정리됐을 것

## Read first

- `AGENTS.md`
- `Docs/Features/Territory.md`
- `Docs/Work/Active/W-20260822-005-exact-incremental-expansion-plan.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `.agents/skills/build-chunk-territory/SKILL.md`

## Allowed files

- 예약된 `ChunkDomain` index/plan/session/metrics 신규 파일과 `.meta`
- 예약된 `ChunkDomain.Tests` 신규 테스트와 `.meta`
- Territory 기능 문서와 이번 milestone 장기 문서
- 현재 Active 작업 문서

## Reserved Scene·Prefab·Data Asset

없음. Inspector 참조와 직렬화 Asset을 변경하지 않는다.

## Prohibited changes

- `TerritorySystem`, `Territory`, `TerritoryVisible` runtime 연결
- Legacy polygon, vertex RPC, mesh와 consumer callback의 변경·삭제
- C006 snapshot materialization/Commit과 changed-Chunk scheduling
- Fusion RPC, Authority, prediction과 presentation 변경
- GPU/CPU mask, Shader, Compute Shader, Material
- Scene, Prefab, ScriptableObject, asmdef, Package와 ProjectSettings

## Required behavior

- Boundary index는 C006 global sequence와 endpoint 연속성을 정확히 검증하고 음수
  Chunk 및 shared edge/corner에서도 local 후보를 중복 없이 제공한다.
- Trail session은 ordered fragment를 한 번만 append하며 진출 뒤 exact fixed Trail,
  재진입, 누적 면적, 자기 교차와 작업량을 증분 유지한다.
- terminal은 전체 Boundary/Trail을 다시 순회·복사하지 않고 prefix 정보로 두 arc를
  평가해 기존 면적보다 커지는 큰 후보를 고른다.
- fixed 정밀도 뒤 모양을 바꾸는 단순화는 없다. 연속 중복점과 exact collinear
  중간점만 제거할 수 있다.
- gap, open/multiple loop, overflow, boundary overlap, self intersection, 추가 crossing,
  정확한 exit/entry 한 쌍 부재와 비증가 후보는 plan을 공개하지 않는다.
- 같은 입력은 Peer 역할과 무관하게 같은 plan/실패 reason과 work metrics를 만든다.

## Acceptance criteria

- loop 유효성, 음수/shared 경계 lookup, exact 접점과 arc 면적 테스트 통과
- 직선·대각선·곡선 표본·concave 영역과 다중 Chunk Trail의 모양 보존 통과
- Abort/reset과 모든 실패가 pending plan을 소각하고 기존 snapshot을 바꾸지 않음
- 1000×1000 world stress에서 append는 local 후보에만 비례하고 terminal full scan
  metrics가 0임을 확인
- 기존 ChunkDomain 회귀와 프로젝트 compile, `git diff --check` 통과
- Scene·Prefab·설정·Shader·asmdef diff 없음

## Rollback

신규 순수 Domain 파일과 C008 문서 diff를 되돌리면 W-005 전 상태로 복원된다.
Runtime 연결이 없으므로 현재 Legacy gameplay에는 rollback 절차가 필요 없다.

## Out of scope

plan materialization, scheduling, runtime/Fusion 연결, exact renderer, consumer migration,
Legacy 제거, Late Join/reconnect/AOI/security 확장.

## Current evidence

- 신규 Domain source와 현재 ChunkDomain 회귀 source를 함께 직접 compile했으며 warning
  0개, error 0개다.
- 신규 16개를 포함한 순수 assertion 56/56 통과. exact Trail, concave/corner/shared
  Chunk edge, sequence/overflow/overlap/self-intersection 실패 원자성과 1000×1000
  stress를 포함한다.
- stress plan의 terminal Boundary/Trail scan metrics는 모두 0이며 append Boundary
  검사는 전체 loop가 아니라 Chunk-local 후보만 사용한다.
- Unity import가 제거된 W-004 mask 참조를 소각하고 신규 W-005 source/test를 생성
  project file에 포함했다.
- `dotnet build ProjectIO.slnx` 오류 0개, 기존 warning 25개로 통과했다.
- 작업자가 Unity compile과 안내된 Territory EditMode 검증 완료를 보고했다.
