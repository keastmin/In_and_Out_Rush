# Current Milestone

Status: Complete

## Objective

Chunk Territory의 첫 기반으로 프레임, UnityEngine, Fusion에 의존하지 않는
고정소수점 좌표와 순서 보존 Trail session/fragment 도메인을 구현한다. Runner
선분은 여러 Chunk를 한 번에 지나거나 같은 Chunk를 재방문해도 결정론적으로
분할되고 원래 이동 순서를 잃지 않아야 한다.

## Prerequisites

- Active reservation `W-20260821-010-chunk-territory-domain-foundation`
- implementation base `e03777125b34266d8e36e522feb7a15b6c200858`
- `CONTRACTS.md`의 C001-C004가 Approved일 것

## Read first

- `AGENTS.md`
- `Docs/Features/Territory.md`
- `Docs/Work/Active/W-20260821-010-chunk-territory-domain-foundation.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `.agents/skills/build-chunk-territory/SKILL.md`

## Allowed files

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/`
- `Docs/LongRunning/Territory/`
- `Docs/Features/Territory.md`
- `Docs/Work/Active/W-20260821-010-chunk-territory-domain-foundation.md`

## Reserved Scene·Prefab·Data Asset

없음.

## Prohibited changes

- Legacy `Territory`, `TerritorySystem`, `TerritoryVisible`와 현재 Trail 코드
- Fusion RPC, Networked state와 NetworkObject
- Scene, Prefab, Material, Shader, Compute Shader와 ScriptableObject
- Territory consumer와 Bootstrapper

## Required behavior

- 월드 1 unit당 256 fixed unit과 8 world-unit Chunk를 사용한다.
- Chunk는 각 축의 `[minimum, maximum)` half-open 영역을 소유한다.
- 음수 좌표도 mathematical floor로 Chunk를 계산한다.
- 선분을 Chunk 경계에서 나눌 때 원래 순서와 연속성을 유지한다.
- 모서리를 정확히 통과하면 두 축을 같은 step에서 이동해 옆 Chunk를 허위 방문하지
  않는다.
- 같은 Chunk 재방문은 별도 fragment이며 fragment 정렬은 좌표가 아니라 sequence다.
- Abort된 session은 payload를 소각하고 같은 session의 후속 sample/fragment를
  거부한다.

## Acceptance criteria

- `ProjectIO.Territory.ChunkDomain.Tests` EditMode 테스트 통과
- 프로젝트 script compile 통과
- 새 runtime assembly가 Assembly-CSharp, UnityEngine, Fusion을 참조하지 않음
- `.meta` pairing과 asmdef JSON parse 통과
- Legacy runtime과 직렬화 Asset diff 없음
- `git diff --check` 통과

## Completion evidence

- Unity 6000.0.69f1 별도 최소 프로젝트에서
  `ProjectIO.Territory.ChunkDomain.Tests` EditMode 15/15 통과
- 주 프로젝트 `Assembly-CSharp.csproj` 빌드 0 error 통과
  (기존 analyzer warning 16건 유지)
- runtime asmdef 참조 없음과 `noEngineReferences: true` 확인
- 신규 Asset 11개와 폴더 2개의 `.meta` pairing, GUID 중복 없음,
  asmdef JSON parse 통과
- Legacy runtime, Scene, Prefab, Material, Shader, Fusion 직렬화 Asset diff 없음

## Rollback

신규 `ChunkDomain`, `ChunkDomain.Tests`, 장기 문서와 Territory 문서의 이번
마일스톤 기록만 제거한다. 런타임 연결이 없으므로 Legacy 동작에는 rollback
절차가 필요하지 않다.

## Out of scope

Fusion 전송, 예측 Trail 표시, Territory Chunk 저장/fill/revision, GPU 렌더링,
consumer migration과 Legacy cutover.
