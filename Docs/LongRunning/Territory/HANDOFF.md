# Handoff

## Completed outcome

C011 background compact expansion shadow를 구현하고 작업자 runtime 검증을 완료했다.
State Authority가 confirmed Trail fragment를 이동 중 증분 수집하고, 재진입 성공 뒤
collection 소유권을 단일 background CPU queue에 넘긴다. worker는 C008-C010을 immutable
source revision 순서대로 처리하며 main thread는 `Render`에서 한 completion만 stale
검증 뒤 publish한다. Legacy gameplay와 현재 Peer 표시는 변경하지 않았다.

아래 C010 persistent compact 기반은 그대로 유지된다.

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
- `TerritoryCompactExpansionWorkItem`, `TerritoryCompactExpansionWorker`, worker result/metrics
- `TerritoryCompactExpansionShadow`, confirmed fragment drain과 `TerritorySystem` 연결
- background worker 회귀 테스트와 C011/milestone/roadmap/test 문서

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
- exact C008-C010을 Native/Burst로 복제하지 않는다. 현재 단일 background CPU worker가
  유일한 compact runtime 계산 경로이며 profile로 입증된 독립 배열 병목만 후속 가속
  후보로 분리한다.
- work item은 expected source revision 순서대로 직렬 처리하고 다음 request가 먼저
  들어와도 직전 immutable candidate에서 이어진다.

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
- W-008 신규 worker assertion 2개 포함 직접 회귀 75/75 통과
- 1000×1000 initial state에서 100 request 선행 enqueue, 동기 reference와 최종
  Boundary area/count/identity 및 revision 순서 일치
- invalid geometry 뒤 partial publish 없음, worker fault와 후속 enqueue 거부 통과
- 신규 source를 명시적으로 포함한 전체 project compile 오류 0개, 기존 warning 25개
- 작업자가 Unity import/Console compile, Territory EditMode와 Host-local/Client Runner
  걷기·달리기·긴 Trail·연속 확장·자기 교차 Abort·teardown 및 Profiler 검증을 완료했다.

## Serialized or manual setup

Scene·Prefab·Inspector 연결 변경은 없다. W-007에 이어 작업자가 W-008의 다음 검증도
완료했다.

1. 신규 script와 `.meta` import 및 Console compile error 없음
2. EditMode `ProjectIO.Territory.Tests` 전체 통과
3. 신규 `TerritoryCompactSnapshotTests`, `TerritoryPersistentBoundaryTreeTests`,
   `TerritoryCompactApplySessionTests`, `TerritoryCompactStoreTests` 통과

4. Host-local Runner와 Client Runner 각각 걷기·달리기, 긴 Trail과 연속 확장
5. 자기 교차 Abort와 stage teardown 뒤 background completion이 적용되지 않음
6. Profiler의 `CompactExpansionSchedule`/`CompactExpansionPublish`가 짧고, debug log의
   revision이 순서대로 한 번씩 publish됨

W-008은 visible 결과를 바꾸지 않으므로 양쪽 Peer의 Trail/영역 모양이 기존과 다르면
회귀다. Scene 설정과 GPU 토글 검증은 필요하지 않다.

## Known risks and failures

- C008-C010 exact 계산은 background로 이동했지만 Legacy polygon 확장, C006 전체 shadow
  rebuild, mesh와 vertex RPC는 아직 main thread다. 전체 gameplay frame 비용 개선은
  authoritative cutover까지 완성되지 않는다.
- background queue 총 latency는 입력 크기에 따라 0이 될 수 없다. 실제 플레이 간격보다
  길어지는지 Host·Client Profiler로 확인해야 한다.
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

- `TerritoryCompactCommitResult`
- `TerritoryCompactSnapshot`
- `TerritoryCompactExpansionWorkResult`
- `TerritoryChunkReplicationStream`
- `TerritorySystem`의 compact publish seam
- `Docs/LongRunning/Territory/CONTRACTS.md`의 C010-C011
