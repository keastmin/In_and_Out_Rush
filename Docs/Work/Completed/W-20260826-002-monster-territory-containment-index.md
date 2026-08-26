# W-20260826-002 Monster Territory containment index

Status: Reserved

## 동기화 기준

- Base Commit: ad0ca641053bcb0f5798f6c9983ebc89d2a2e575
- 공용 Upstream: `origin/rebuild-development-environment`

## 담당자

- Codex (`/root`)

## 기능

- Territory
- Monster와 Projectile

## 목표

Legacy `Territory` polygon을 authoritative source로 유지하면서, Monster
`FixedUpdateNetwork` hot path의 containment query가 전체 polygon edge를 반복
순회하지 않도록 query-only Y-uniform `TerritoryContainmentIndex`를 추가한다.
기존 EPS(`0.0001f`)와 boundary-inside 판정, full-scan reference 구현 및
안전 fallback의 최종 bool 결과를 보존한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/Features/MonstersAndProjectiles.md`
- `manage-feature-work`, `build-chunk-territory`, `photon-fusion-feature`
- `Assets/02_Scripts/Territory/Territory.cs`
- `Assets/02_Scripts/Monster/Monster.cs`
- `Assets/02_Scripts/Monster/WorldMonster.cs`
- `Assets/02_Scripts/Monster/Strider.cs`
- `Assets/02_Scripts/Monster/Stalker.cs`
- `Assets/02_Scripts/Monster/Centipede.cs`
- `Assets/02_Scripts/Monster/TrackMonster.cs`

## 예상 수정 코드

- `Assets/02_Scripts/Territory/Territory.cs`
  - Legacy full scan을 내부 reference/fallback으로 보존하고 containment index
    rebuild 및 query 경로를 연결한다.
- `Assets/02_Scripts/Territory/TerritoryContainmentIndex.cs`
  - Y 구간 uniform bucket query-only index, 8 world-unit 후보 bucket,
    mathematical-floor 음수 좌표 처리, EPS 포함 edge 등록, 병적 reference
    fallback과 최소 Profiler 계측을 구현한다.
- `Assets/02_Scripts/Territory/TerritoryContainmentIndex.cs.meta`
- `Assets/02_Scripts/Territory Refactor/Tests/Editor/TerritoryContainmentIndexTests.cs`
  - reference full scan과 indexed 결과의 정형·고정-seed randomized·수천 정점·
    rebuild·fallback·allocation 범위 테스트를 추가한다. 이 Editor test는
    Legacy authoritative query만 검증하며 Chunk consumer cutover를 수행하지 않는다.
- `Assets/02_Scripts/Territory Refactor/Tests/Editor/TerritoryContainmentIndexTests.cs.meta`
- `Assets/02_Scripts/Monster/WorldMonster.cs`
  - `allowTerritoryExit=false`인 일반 patrol 이동에서 endpoint safe-zone
    사전 확인과 path endpoint sample의 의미가 동일함을 코드로 명시하고 한
    containment 경로로 통합한다.
- `Assets/02_Scripts/Monster/Strider.cs`
  - 일반 slide target/slide tick의 동일한 중복 endpoint 판정을 통합한다.
- `Assets/02_Scripts/Monster/Stalker.cs`
  - 일반 chase tick의 동일한 중복 endpoint 판정을 통합한다.
- `Docs/Features/Territory.md`
- `Docs/Features/MonstersAndProjectiles.md`
- `Docs/Work/Active/W-20260826-002-monster-territory-containment-index.md`

## 예약 Scene·Prefab·Data Asset

없음. Scene, Prefab, ScriptableObject, ProjectSettings, Package 변경은 하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- Legacy `Territory.Vertices`와 `TerritorySystem`의 현재 확장 결과가 계속
  authoritative source다. Chunk Territory state/consumer cutover는 하지 않는다.
- `ApplyNewPolygon` 및 `ReplaceVertices`가 정상 mutation seam으로 index를
  rebuild한다. 공개 `Vertices`를 직접 수정하는 새 경로는 추가하지 않는다.
- `EPS = 0.0001f`, boundary-inside, convex/concave, 음수 좌표, 수평/수직/
  대각선 edge의 기존 의미를 유지한다.
- State Authority의 이동/안전구역 결정, AI cadence, 이동 속도,
  `MaxSafeZonePathSampleDistance`, NetworkTransform 관찰 계약은 변경하지 않는다.

## 네트워크·Peer 동등성

- 입력 원점: 해당 없음. Monster AI는 현재 `Monster.FixedUpdateNetwork`의
  State Authority 경로에서만 실행된다.
- 검증·mutation owner: State Authority가 기존 이동과 안전구역 판정을 계속
  수행한다. containment index는 동일한 local query 최적화일 뿐이다.
- 복제/결과: 새 RPC, Networked 상태, Spawn/Despawn 또는 authority 변경이 없다.
  Client는 기존 NetworkTransform 결과를 관찰한다.
- Host/Client: Host-local 및 Client peer의 최종 게임 결과가 reference full scan과
  같아야 한다. 구현 뒤 Host와 Client에서 이동 결과가 동일한지 수동 절차를 기록한다.
- 중복 실행·준비/Late Join/정리: 새 network lifecycle이 없고 State Authority guard를
  유지한다. Territory polygon mutation 뒤 index rebuild가 오래된 결과를 남기지 않는지
  테스트한다.

## 다른 활성 작업과 겹치는 부분

없음. 확인 시 `Docs/Work/Active/`에는 `README.md`만 존재했다.

## 범위 밖

- Chunk Territory authoritative source 또는 consumer cutover
- Territory expansion 알고리즘·결과, Fusion RPC/Networked 상태, Scene/Prefab 변경
- `allowTerritoryExit` 특수 규칙이 있는 Centipede와 patrol candidate 경로의
  중복 제거
- TrackMonster의 이동 로직 변경

## 완료 조건

- AABB reject 뒤 Y bucket candidate edge만 boundary/ray-crossing 검사하고 query 중
  컬렉션 생성·LINQ 없이 managed allocation 0을 목표로 한다.
- 수평 edge와 EPS Y 범위, 긴 edge의 모든 교차 bucket, bucket 경계와 음수
  mathematical floor를 포함한다.
- index 무효 또는 reference 수가 과도한 경우 기존 full scan으로 fallback한다.
- 신규 index 결과가 reference full scan과 정형·randomized·대규모 polygon·rebuild·
  fallback cases에서 같다.
- query 횟수, 전체 edge 수, candidate edge 검사 수, rebuild 비용을 최소 범위의
  ProfilerMarker 또는 개발용 계측으로 확인 가능하게 한다. 매 tick log는 추가하지 않는다.
- 일반 WorldMonster, Strider, Stalker에서만 endpoint 중복 판단을 통합하고,
  path endpoint sample과 Territory·Sanctuary 결과가 동일한 근거를 테스트/문서에 남긴다.
- 관련 Editor tests와 프로젝트 컴파일을 실행하고 `git diff --check`, `git status`를
  확인한다. 수행 불가한 Host/Client runtime 및 profiler 측정은 정확히 기록한다.

## 실제 변경

- `TerritoryContainmentIndex`를 Legacy Territory query-only index로 추가했다.
  8 world-unit Y bucket은 mathematical floor로 계산하고, edge의 Y min/max에
  `EPS`를 적용해 수평·긴 edge를 포함한다. 전체 reference가 edge 수의 16배를
  넘으면 index를 무효화해 reference full scan으로 fallback한다.
- `Territory.ApplyNewPolygon`과 `ReplaceVertices`에서 bounds와 함께 index를
  rebuild하고, `IsPointInPolygon`은 AABB reject 뒤 index 성공 시 candidate edge만
  검사하고 그렇지 않으면 기존 `PointInPolygon` full scan을 사용한다.
- ProfilerMarker 4개와 query/candidate/rebuild/fallback 개발용 계측을 추가했다.
- 일반 `allowTerritoryExit=false` WorldMonster patrol, Strider slide,
  Stalker chase 및 WorldMonster 기본 patrol candidate에서 endpoint의
  `IsPositionInRunnerSafeZone` 사전 query를 제거했다. 경로 sample이 endpoint를
  포함해 동일 Territory·Sanctuary 판정을 수행한다. Centipede/특수 exit 경로와
  TrackMonster는 변경하지 않았다.
- Editor equivalence/rebuild/fallback/allocation test와 Territory·Monster feature
  문서를 추가·갱신했다.

## 검증 결과

- `CheckStart`: READY_TO_CHECK_CONFLICTS (`AHEAD=0`, `BEHIND=0`)
- Active 충돌: 없음
- `CheckReservation`, 예약 문서 단독 commit/push, `VerifyReservation` 통과
  (`IMPLEMENTATION_BASE=9634dfe326fcc7e3bd47dd0c71315d8767e7b888`).
- `git diff --check`: 통과.
- `dotnet build Assembly-CSharp.csproj --no-restore`: Unity generated
  `Temp/obj` assets file 부재로 시작하지 못했다.
- 일반 `dotnet build Assembly-CSharp.csproj`: 새 `.cs`가 아직 포함되지 않은
  stale Unity generated `.csproj`라 `TerritoryContainmentIndex`를 찾지 못해 실패했다.
- Unity 6000.0.69f1 batchmode compile: 이미 열린 ProjectIO Unity Editor가 project
  lock을 보유해 실행하지 못했다. 열린 Editor의 ScriptAssemblies timestamp도 아직
  갱신되지 않아 Editor test와 새 파일 포함 컴파일은 미검증이다.
- Editor test, 실제 Host/Client 이동, Profiler 수집은 열린 Unity Editor에서 실행
  대기 중이었다. 작업자가 Unity compile 및 관련 tests 완료를 확인했다.

## 남은 위험

- index candidate filter가 EPS 인접 bucket/수평 edge를 누락하면 false negative가
  발생할 수 있으므로 reference equivalence와 fallback을 필수로 검증한다.
- 실제 Host/Client runtime 및 Profiler 수치는 Unity 실행 환경에서 별도로 확인해야 한다.
- 새 Runtime·Editor test 파일을 포함한 Unity compile과 NUnit test를 실행하기 전에
  열린 Editor가 asset refresh/compile을 완료해야 한다.
