# Handoff

## Completed outcome

UnityEngine과 Fusion에 의존하지 않는 fixed coordinate, half-open Chunk ownership,
결정론적 segment traversal과 ordered Trail session/fragment 도메인을 구현했다.
같은 Chunk 재방문, sequence gap, Abort stale payload를 순수 계약에서 처리한다.
Legacy runtime에는 연결하지 않았다.

## Changed files

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/`
- `Docs/LongRunning/Territory/`
- `Docs/Features/Territory.md`
- `Docs/Work/Active/W-20260821-010-chunk-territory-domain-foundation.md`

## Decisions used

- `CONTRACTS.md` C001-C004

## Verification evidence

- Unity 6000.0.69f1 별도 최소 프로젝트 EditMode 15/15 passed
- 주 프로젝트 `Assembly-CSharp.csproj` build: 0 errors, 기존 warning 16건
- runtime asmdef: references 없음, `noEngineReferences: true`
- Asset/meta pairing 13/13, GUID 중복 0, asmdef JSON parse 통과
- `git diff --check` 통과
- Legacy runtime과 직렬화 Asset diff 없음

## Serialized or manual setup

없음. 이번 마일스톤은 Scene·Prefab·Data Asset을 변경하지 않는다.

## Known risks and failures

- Unity/Fusion float 위치를 fixed sample로 바꾸는 runtime adapter는 후속
  마일스톤 범위다.
- 실제 Trail 지연과 Territory hitch는 아직 개선되지 않는다.
- 실제 Host·Client, Late Join과 GPU/CPU fallback은 이번 순수 도메인
  마일스톤에서 실행 검증하지 않았다.

## Remaining legacy consumers

모든 Legacy Territory consumer. 이번 마일스톤은 runtime에 연결되지 않는다.

## Next bounded milestone

State Authority에서 현재 Legacy 경로와 함께 새 Trail session을 shadow로 기록하고,
경로 순서·Abort·강제 이동 중단 결과를 비교하되 게임 결과와 RPC를 변경하지 않는
단일 integration milestone.

## Exact starting files

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Domain/TerritoryExpansionSession.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/`
- `Docs/LongRunning/Territory/CONTRACTS.md`
