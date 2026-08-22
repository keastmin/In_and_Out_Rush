# Current Milestone

Status: Complete

## Objective

C009 exact Boundary edit와 Full row run을 C006 개별 Full Chunk와 전역 연속 sequence로
다시 펼치지 않고 반복 적용하는 C010 persistent compact Territory state를 구현한다.
다음 C008/C009는 compact result에서 직접 이어지며, 확장 횟수와 기존 면적이 커져도
정상 apply는 source 전체를 순회·복사·재번호화하지 않는다.

이번 milestone은 순수 ChunkDomain 저장 기반만 추가한다. Legacy polygon, C006/C007
shadow runtime, Fusion, renderer와 gameplay 결과는 계속 기존 경로가 소유한다.

## Prerequisites

- Active reservation `W-20260822-007-persistent-compact-territory-store`
- implementation base `b1f136346d683f2aafa8e21676358e1380df6763`
- `CONTRACTS.md`의 C001-C010이 Approved일 것
- W-006 C009 compact materialization이 Complete일 것

## Required behavior

- C006 최초 변환만 source Chunk/Boundary 전체를 읽고 canonical stable Boundary와
  Y별 merged Full run을 만든다.
- 정상 확장은 stable identity 제거 arc, exact endpoint residual과 Trail part만
  persistent Boundary에 splice한다.
- Boundary order/면적 prefix, identity lookup, Chunk candidate와 Full row는 balanced
  persistent root를 사용하고 변경되지 않은 branch를 공유한다.
- 다음 C008 index/session과 C009 materialization은 compact snapshot을 직접 사용한다.
- `TryStep(maxWorkUnits)`는 호출 budget 이하로 진행하고 terminal 이전 결과를
  공개하지 않는다.
- stale revision/identity, malformed splice, area mismatch, overflow와 Abort는
  candidate를 소각하고 source를 보존한다.
- 정상 apply의 source-wide Boundary/Full scan, global renumber, Full Chunk 전개와
  unchanged-node copy metric은 0이다.

## Prohibited changes

- `TerritorySystem`, Legacy `Territory`, mesh와 consumer callback
- C006/C007 active shadow store·replication 교체 또는 삭제
- Fusion RPC, Authority, prediction과 presentation
- Job/Burst frame scheduler와 runtime Composition Root
- GPU/CPU mask, Shader, Compute Shader, Material
- Scene, Prefab, ScriptableObject, asmdef, Package와 ProjectSettings

## Acceptance criteria

- C006 CW/CCW snapshot의 exact canonical compact 변환과 row 압축
- stable Boundary identity/order/prefix area와 exact endpoint splice
- budget 1과 큰 budget의 snapshot·metrics 동일성 및 terminal 원자성
- stale/Abort/malformed candidate가 source/Store를 변경하지 않음
- 1000×1000 world에서 compact result로 최소 100회 연속 C008/C009/apply 수행
- 매 정상 apply의 전체 scan/renumber/Full 전개/unchanged copy metric 0
- 기존 ChunkDomain 회귀, Unity EditMode, 프로젝트 compile과 `git diff --check` 통과
- Scene·Prefab·설정·Shader·asmdef diff 없음

## Current evidence

- 신규 source를 포함한 순수 ChunkDomain compile: warning 0, error 0.
- 신규 persistent compact 테스트 7개와 기존 회귀를 직접 실행해 73/73 통과.
- budget 1/10000 apply의 revision, exact area, Boundary count, next stable identity와
  algorithmic metrics가 동일하고 모든 호출이 전달 budget 이하를 사용했다.
- 1000×1000 world에서 compact result를 다음 C008/C009 입력으로 직접 사용해 100회
  연속 확장했다. retained identity가 유지됐고 매 회 전체 Boundary/Full scan,
  global renumber, Full Chunk 전개와 unchanged-node copy metrics는 모두 0이었다.
- 기존 C008/C009 exact Trail, 실패 원자성과 1000×1000 회귀를 포함해 통과했다.
- 작업자가 Unity import/Console compile과 안내된 Territory EditMode 전체 검증을
  완료했다.
- Unity가 재생성한 현재 solution에서 `dotnet build ProjectIO.slnx`는 오류 0개,
  기존 warning 25개로 통과했다.

## Rollback

신규 C010 순수 Domain/test 파일과 기존 C008/C009 stable identity 연결 diff를 되돌리면
W-006 상태로 복원된다. Runtime 연결이 없어 현재 Legacy gameplay rollback은 없다.

## Out of scope

Job/Burst worker와 frame budget adapter, State Authority runtime 적용, compact delta
복제, exact renderer, consumer migration, Legacy 제거, Late Join/reconnect/AOI/security.

## Next bounded milestone

Unity 검증과 W-007 최종 Commit·Push 뒤 persistent planner/materializer/apply를 frame
budget과 Job/Burst worker에 연결하고 State Authority shadow에서 실제 비용을 profile한다.
결과 모양과 C010 저장 계약은 바꾸지 않으며 Fusion 권위 전환과 presentation은 별도
milestone으로 유지한다.
