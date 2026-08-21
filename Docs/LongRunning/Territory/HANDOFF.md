# Handoff

## Completed outcome

State Authority에 revisioned sparse Chunk Territory shadow store를 연결했다. snapshot은
전역 polygon을 보관하지 않고 미저장 `Empty`, payload 없는 `Full`, fixed Chunk-local
방향성 경계 선분을 가진 `Boundary` coverage로 구성된다.

초기 Territory와 Legacy 정상 확장 결과만 candidate로 빌드하며, 전체 성공 뒤에만
revision과 changed-Chunk를 원자적으로 공개한다. Legacy polygon, mesh, vertex RPC,
consumer callback과 W-012 Trail은 계속 유일한 게임 경로다.

## Changed files

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkFill.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkLocalPoint.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkBoundarySegment.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkCoverage.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkSnapshot.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkCommitResult.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkStateBuilder.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkStore.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkStateBuilderTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkStoreTests.cs`
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/`의 milestone/contract/handoff/test 문서
- `Docs/Work/Completed/W-20260822-001-chunk-territory-state-commit.md`

## Decisions used

- `CONTRACTS.md` C001-C006
- State Authority shadow only; Legacy gameplay authority 유지
- revision은 initial 1부터 단조 증가 `ulong`
- changed coverage와 `Empty` tombstone은 `(Y, X)` 오름차순
- changed-Chunk Fusion replication과 Late Join은 다음 milestone

## Verification evidence

- 예약된 신규 builder/store NUnit assertion 9개를 실제 신규 source로 직접 실행해 통과
- 음수/half-open 분류, fixed local 경계 재구성, invalid/self-intersection 거부 통과
- initial/repeated revision, 동일 coverage 빈 delta, ordered tombstone 통과
- stale base, invalid candidate와 `ulong` overflow가 기존 snapshot을 보존함을 확인
- 실제 신규 source domain compile 0 errors
- 신규 domain DLL을 사용한 `Assembly-CSharp` 통합 compile 0 errors, 기존 warning 13개
- 1000×1000 사각형: 15,875 Chunk, 6~11ms
- 256점·반경 500 world-unit 원형: 12,532 Chunk, 17~20ms
- 위 시간은 로컬 .NET Debug/Release validation이며 Unity Profiler 측정이 아님
- 작업자가 Unity import/compile과 `ProjectIO.Territory.Tests` 전체 실행 완료를 보고
- 작업자가 Host 로컬/Client Input Authority 정상·연속 확장, 실패·Abort와
  revision 단일 증가 runtime 절차 완료를 보고

## Serialized or manual setup

Scene·Prefab·Inspector 연결 변경은 없다. 열린 Unity Editor가 신규 script/meta를
import하고 compile을 끝낸 뒤 다음 절차를 수행한다.

1. Console error가 없고 EditMode `ProjectIO.Territory.Tests` 전체가 통과하는지 확인한다.
2. Host Runner 세션 시작 시 `Chunk Territory shadow committed` revision 1 로그가
   State Authority에서 한 번만 나오는지 확인한다.
3. Host Runner로 정상 확장 두 번을 수행한다. 성공마다 revision 2, 3이 한 번씩
   증가하고 Legacy 영역 모양·consumer 결과·Trail 정리가 기존과 같은지 확인한다.
4. Client Input Authority Runner로 정상 확장한다. Host State Authority에만 다음
   revision Commit 로그가 한 번 나오고 양쪽의 Legacy 영역 결과가 같은지 확인한다.
5. 자기 교차, Lifeline/Abort와 확장 거부를 실행한다. 성공하지 않은 시도에는
   revision Commit 로그가 없어야 하며 다음 정상 확장은 정확히 1만 증가해야 한다.
6. 가능하면 Unity Profiler에서 `TerritorySystem.ExpandTerritoryFromCurrentPath`를
   확인해 shadow rebuild가 실제 장거리 확장 frame에 더하는 시간을 기록한다.

새 Chunk snapshot이 Client/Late Join에 복원되지 않는 것은 이번 shadow 범위의
의도된 제한이며 현재 게임 결과는 기존 Legacy vertex RPC로 복원된다.

## Known risks and failures

- Legacy 256 vertex 보정과 단일 polygon 확장 hitch는 아직 제거되지 않았다.
- shadow snapshot rebuild는 동기식이다. synthetic 1000×1000·256점 입력에서
  17~20ms였으므로 Unity Profiler 결과에 따라 incremental/time-slice가 필요할 수 있다.
- changed-Chunk replication과 Late Join snapshot은 milestone 5 대상이다.

## Remaining legacy consumers

모든 Territory consumer, authoritative expansion, mesh와 vertex replication. Chunk
snapshot은 아직 shadow 진단 상태이며 게임 판정이나 표시를 생산하지 않는다.

## Next bounded milestone

W-013이 Complete됐으므로 State Authority changed-Chunk를 bounded Reliable payload로
복제하고 recovery/Late Join snapshot을 제공한다. GPU와 consumer migration은
포함하지 않는다.

## Exact starting files

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkSnapshot.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkCommitResult.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkCoverage.cs`
- 신규 `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/` Chunk replication 파일
- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Docs/LongRunning/Territory/CONTRACTS.md`
