# Handoff

## Completed outcome

첫 fixed/Chunk/ordered Trail 도메인에 이어 State Authority 전용 shadow recorder와
Legacy path comparer를 구현했다. 정상 재진입은 Commit 후 sample 순서를 비교하고,
Stop·자기 교차·Lifeline·teardown은 payload를 Abort한다. Shadow는 진단 로그만
남기며 Legacy Territory, RPC, 표시와 consumer 결과는 계속 유일한 게임 경로다.

자동 검증과 안내된 Host·Client 실제 플레이 절차를 완료했다.

## Changed files

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Domain/TerritoryExpansionSession.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailShadowRecorder.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailShadowComparison.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailShadowComparer.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailShadowComparerTests.cs`
- `Docs/LongRunning/Territory/`
- `Docs/Features/Territory.md`
- `Docs/Work/Completed/W-20260821-011-chunk-territory-shadow-trail.md`

## Decisions used

- `CONTRACTS.md` C001-C004

## Verification evidence

- Unity 6000.0.69f1 격리 프로젝트 EditMode 21/21 passed
- 실제 수정된 recorder, Legacy session과 `TerritorySystem` 통합 compile: 0 errors
- 순수 comparer의 일치, 좌표 mismatch, count mismatch와 recorder의 Commit,
  fragment, Abort, 다음 SessionId 테스트 통과
- 신규 Asset/meta pairing, GUID, RPC/Networked diff와 `git diff --check` 통과
- 작업자가 Host·Client 정상 재진입, 자기 교차/Lifeline과 SandTomb pause/resume
  절차 완료를 보고

## Serialized or manual setup

Scene·Prefab·Data Asset 설정은 없다.

Host와 Client 각각 Runner로 영역을 벗어나 여러 Chunk를 지난 뒤 재진입한다.
Host Console에서 `Shadow Trail matched`를 확인하고 Legacy 확장 결과와 선 표시가
기존과 같은지 확인한다. 자기 교차/Lifeline은 `Shadow Trail aborted` 후 다음
정상 경로가 더 큰 SessionId로 matched되는지 확인한다. 강제 이동은 영역 밖에서
SandTomb에 잡혔다 풀려난 뒤 pause 중 점이 추가되지 않고 재진입이 matched되는지
확인한다. Client Console에서는 State Authority shadow 로그가 중복되면 안 된다.

## Known risks and failures

- 실제 Trail 지연과 Territory hitch는 아직 개선되지 않는다.
- Shadow session은 진단용 비지속 상태라 Late Join에 복원되지 않는다.
- GPU/CPU fallback은 아직 구현하지 않았다.

## Remaining legacy consumers

모든 Territory consumer와 authoritative expansion. Shadow 외에는 전환되지 않았다.

## Next bounded milestone

W-011 Host·Client runtime evidence를 확인한 뒤 Input Authority live head 예측과
State Authority confirmed fragment 전송·표시 계약을 한정된 단일 마일스톤으로
설계한다. Territory fill과 GPU는 아직 포함하지 않는다.

## Exact starting files

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailShadowRecorder.cs`
- `Assets/02_Scripts/Territory Refactor/Trail/TerritoryTrailChunkRenderer.cs`
- `Assets/02_Scripts/Player/Player Runner/Network/PlayerRunner.cs`
- `Docs/LongRunning/Territory/CONTRACTS.md`
