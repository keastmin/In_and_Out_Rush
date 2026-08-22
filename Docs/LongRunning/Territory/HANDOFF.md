# Handoff

## Completed outcome

C009 materialization을 반복 적용할 수 있는 C010 persistent compact Territory state를
구현했다. 최초 C006 shadow snapshot만 한 번 읽어 counter-clockwise stable Boundary와
Y별 merged Full run으로 변환한다. 이후 확장은 C006 개별 Full entry나 전역 연속
Boundary sequence로 돌아가지 않는다.

Boundary는 stable `ulong` identity, exact fixed local endpoint, 순서와 면적 합계를 가진
persistent balanced tree다. Chunk 후보와 Full row도 persistent map이며 변경 경로만 새
node로 만들고 나머지는 이전 immutable revision과 공유한다. C008 planner와 C009
materializer는 compact snapshot에서 직접 다음 확장을 시작한다.

C009 결과는 제거되는 contiguous stable arc, 접점에서 남는 source residual과 exact
Trail part를 검증한 뒤 `TryStep(maxWorkUnits)`로 적용한다. terminal 이전에는 candidate가
보이지 않고 Store가 같은 source object/revision의 완료 candidate만 다음 revision으로
원자 공개한다.

이번 slice는 순수 Domain뿐이다. Legacy gameplay, C006/C007 runtime, Fusion, GPU,
renderer, Scene과 Inspector는 변경하지 않았다.

## Main changed files

- `TerritoryBoundarySegmentId`, `TerritoryCompactBoundarySegment`
- `TerritoryPersistentAvlMap`, `TerritoryPersistentBoundaryTree`
- `TerritoryCompactRowCoverage`, `TerritoryCompactSnapshot`,
  `TerritoryCompactSnapshotBuilder`
- `TerritoryBoundarySplice`, `TerritoryCompactApplySession`,
  `TerritoryCompactStore`, apply metrics/commit result
- stable identity를 전달하도록 확장한 C008 plan/index와 C009 edit/materialization
- persistent snapshot/tree/apply/store 신규 테스트 4개 파일
- Territory 기능 문서와 C010/milestone/roadmap/test 문서

## Decisions used

- authoritative geometry와 저장은 C001 fixed CPU domain 한 경로만 사용한다.
  GPU/CPU fallback은 만들지 않는다.
- Boundary의 stable identity와 저장 순서는 분리한다. 유지되는 segment를 확장마다
  `0..N-1`로 재번호화하지 않는다.
- 넓은 내부는 Y별 inclusive run으로 계속 보존하고 개별 Full Chunk로 전개하지 않는다.
- persistent root는 parent delta chain이 아니므로 현재 snapshot 질의가 확장 횟수만큼
  이전 revision을 따라가지 않는다.
- C009 stable splice는 endpoint가 segment 끝과 정확히 일치할 때 zero-length 제거
  segment를 제외하고 실제 non-zero contiguous arc만 교체한다.
- 정상 apply metrics에서 전체 scan, global renumber, Full 전개와 unchanged-node copy는
  항상 0이며 초기 C006 변환 scan과 분리한다.

## Verification evidence

- 신규 source를 포함한 순수 ChunkDomain compile: warning 0, error 0
- 신규 persistent compact 테스트 7개 포함 직접 회귀: 73/73 통과
- 기존 exact Trail/교체 arc, invalid Trail, Abort/stale와 1000×1000 C008/C009 회귀 통과
- budget 1/10000 apply의 snapshot identity/area/count/revision과 metrics 동일
- 완료 전 result 비공개, Abort와 stale Store publish가 source revision을 보존
- 1000×1000 world에서 compact result로 100회 연속 C008/C009/apply 통과
- 100회 모두 source-wide Boundary/Full scan, global renumber, Full Chunk 전개와
  unchanged-node copy metrics 0; 확장하지 않은 왼쪽 Boundary identity 유지
- `git diff --check` 통과
- 작업자가 Unity import/Console compile과 Territory EditMode 전체 검증 완료
- Unity가 재생성한 solution의 `dotnet build ProjectIO.slnx` 오류 0개, 기존 warning
  25개

## Serialized or manual setup

Scene·Prefab·Inspector 연결 변경은 없다. 작업자가 다음 검증을 완료했다.

1. 신규 script와 `.meta` import 및 Console compile error 없음
2. EditMode `ProjectIO.Territory.Tests` 전체 통과
3. 신규 `TerritoryCompactSnapshotTests`, `TerritoryPersistentBoundaryTreeTests`,
   `TerritoryCompactApplySessionTests`, `TerritoryCompactStoreTests` 통과

이번 slice는 runtime 미연결이므로 Host·Client 플레이나 표시 모양이 바뀌면 회귀다.
PlayMode, Scene 설정, GPU 토글 검증은 필요하지 않다.

## Known risks and failures

- 실제 gameplay frame 비용은 아직 바뀌지 않는다. 후속 Job/Burst/frame scheduler와
  State Authority shadow 연결 뒤 Unity Profiler로 budget/latency를 정해야 한다.
- balanced Boundary order key는 exact arbitrary-precision 값을 사용한다. 100회 반복
  stress에서는 퇴화하지 않았지만 한 접점 사이만 극단적으로 반복하는 장기 profile로
  key 비교 비용을 계속 관찰한다.
- apply의 한 work unit은 persistent map의 `O(log N)` path update와 최대 3×3 Chunk 후보
  갱신이라는 고정 상한 작업이다. 후속 frame adapter가 실제 budget 단위를 profile에
  맞춰 조정해야 한다.
- compact delta replication과 presentation은 아직 없으므로 C010을 C006/C007 active
  runtime에 억지로 다시 펼쳐 연결하면 안 된다.

## Remaining legacy consumers

authoritative expansion, State Authority compact apply, replication, containment, mesh,
Grid, Fog, Resource, Monster, vertex RPC와 presentation은 모두 Legacy/C006 shadow 경로다.

## Next starting files

- `TerritoryCompactSnapshot`
- `TerritoryBoundaryLoopIndex`
- `TerritoryChunkExpansionMaterializationSession`
- `TerritoryCompactApplySession`
- `TerritoryCompactStore`
- `Docs/LongRunning/Territory/CONTRACTS.md`의 C008-C010
