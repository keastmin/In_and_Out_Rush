# Territory

Status: Legacy Polygon authoritative, background expansion and network Trail active

Last reviewed: 2026-08-28

## 책임

Territory는 플레이 영역의 논리 상태와 내부 판정, Runner가 그리는 확장 Trail,
State Authority의 확장 결과, Host·Client 표현 동기화를 관리한다. Grid, Fog,
Resource, Monster, Sacred Zone, Sanctuary 등은 이 기능의 공개 상태와 확장 이벤트를
소비한다.

현재 논리 영역의 유일한 authoritative source는 `Territory.Vertices` Polygon이다.
Chunk 좌표는 Trail 저장·표시를 나누는 보조 단위일 뿐, 논리 영역 상태나 영역 판정의
authoritative source가 아니다.

## 현재 소유권

| 책임 | 현재 소유자 |
|---|---|
| 논리 영역과 확장 규칙 | `Territory`, `TerritorySystem` |
| 빠른 내부 판정 | `TerritoryBoundsIndex`, `TerritoryContainmentIndex` |
| 확장 경로와 자기 교차 판정 | `TerritoryExpansionSession`, `TerritoryTrailSegmentIndex` |
| 백그라운드 Polygon 계산 | `TerritoryBackgroundExpansionWorker`과 관련 WorkItem/Result |
| 확장 결과 복제·복구 | `TerritoryExpansionReplication`, Result Packetizer/Replica |
| confirmed Trail 기록·복제 | `TerritoryTrailShadowRecorder`, `TerritoryTrailReplicationStream` |
| Trail 순수 데이터 | `FixedTerritoryPoint`, `TerritoryTrailSession`, Packet/Receiver/Comparer |
| 영역과 Trail 비주얼 | `TerritoryVisible`, `TerritoryTrailChunkRenderer` |

`TerritoryChunkCoordinate`와 `TerritorySegmentChunkTraversal`은 fixed Trail을
8 world-unit 단위로 분할하는 데 사용한다. 현재 Chunk snapshot, Chunk store,
Compact store, Chunk delta 복제 또는 Chunk 영역 비주얼은 존재하지 않는다.

## 주요 진입점과 공개 연결부

- `TerritorySystem.Territory`: 현재 완료된 논리 Polygon
- `TerritorySystem.TerritoryVisible`: 현재 완료된 영역 Mesh 표현
- `TerritorySystem.IsExpanding`, `ExpansionLineWidth`: Runner와 아이템 표현용 상태
- `TerritorySystem.OnTerritoryExpandedEvent`: 완료 결과가 원자 적용된 뒤 발생하는 이벤트
- `Territory.IsPointInPolygon(Vector2)`: authoritative 내부·경계 판정
- `Territory.IsPointOnBoundary(Vector2)`: Polygon 경계 판정
- `Territory.ReplaceVertices(IReadOnlyList<Vector2>)`: Polygon과 query index를 함께 교체하는 정상 seam
- `Territory.TryCalculateExpansion(...)`: 백그라운드 계산에서 사용하는 순수 입력 복사 seam
- `StageBootstrapper.TerritorySystem`, `StageBootstrapper.TerritoryVisible`: Scene 연결부

## 확장과 Trail 흐름

1. `TerritorySystem`이 초기 원형 Polygon을 만들고 `Territory`와
   `TerritoryVisible`에 적용한다.
2. Runner가 영역 밖으로 나가면 Input Authority owner는 local predicted Trail을 즉시
   표시한다. State Authority는 위치를 검증하고 confirmed fixed Trail을 기록한다.
3. confirmed Trail은 bounded Reliable packet으로 Proxy에 전송된다. 움직이는 끝점은
   Unreliable live-head 갱신으로 보완하며, `TerritoryTrailChunkRenderer`가 Chunk별
   LineRenderer segment로 표현한다.
4. 현재 segment가 이전 Trail과 교차하면 `TerritoryTrailSegmentIndex`가 자기 교차를
   판정한다. 이 인덱스는 8 world-unit AABB Chunk 후보를 사용하며 논리 영역 인덱스와는
   별개다.
5. Runner가 영역으로 재진입하면 State Authority가 source Polygon과 계산용 Trail
   snapshot을 `TerritoryBackgroundExpansionWorker`에 한 건 예약한다. 계산 중에는
   마지막 완료 Polygon이 판정과 표현의 기준이며 다음 확장을 시작하지 않는다.
6. worker는 Polygon 검증과 triangulation까지 완료한다. main thread가 source revision을
   다시 확인한 뒤 `ReplaceVertices`, Mesh 교체와
   `OnTerritoryExpandedEvent`를 한 번에 수행한다.
7. 확장 실패·취소·teardown은 마지막 완료 Polygon을 보존한다.

계산용 경로는 짧은 이동을 모두 저장하지 않고 거리와 방향 변화 기준으로 점을 선택한다.
표시·복제용 fixed Trail과 계산용 Polygon 경로는 같은 수명주기를 공유하지만 동일한
자료구조는 아니다.

외부 강제 이동 중에는 State Authority가 Trail을 suspend하고 Reliable lifecycle을
보낸다. owner prediction은 마지막 confirmed point로 reconcile하며 강제 이동 구간을
확장선으로 추가하지 않는다. 재개하면 현재 위치에서 다시 이어진다.

## 네트워크 계약

- State Authority만 확장 성공 여부와 authoritative Polygon revision을 결정한다.
- `AdvertisedTerritoryExpansionRevision`과
  `AdvertisedTerritoryExpansionPending`이 완료 revision과 계산 중 상태를 지속
  Networked 상태로 알린다.
- 정상 확장 결과는 이전 Polygon patch가 아니라 전체 vertex float bit와 triangle index
  snapshot이다. Reliable packet은 data packet당 최대 48 word이고 simulation tick당
  정상·복구 합계 최대 2 data packet을 보낸다.
- Proxy는 Begin/Data/Complete 전부를 검증한 뒤에만 결과를 원자 적용한다. sequence gap,
  충돌하는 future transfer와 malformed payload는 공개 상태를 바꾸지 않는다.
- Proxy가 advertised revision보다 뒤처지면 targeted Reliable recovery를 요청하고,
  outstanding 요청이 2초 동안 수렴하지 않으면 재시도한다.
- 제품 세션은 스테이지 시작부터 함께하는 최대 2인 전제다. 일반 Late Join·reconnect
  지원은 현재 계약이 아니다.
- `GameWorld`의 Territory NetworkObject는 AOI 밖에서도 revision이 고착되지 않도록
  global interest를 유지한다.

## 내부 판정

`Territory.IsPointInPolygon`은 먼저 `TerritoryBoundsIndex`로 전체 AABB를
reject한다. 그 다음 `TerritoryContainmentIndex`가 point Y에 해당하는 8 world-unit
uniform bucket의 edge만 사용해 경계와 ray-crossing을 검사한다.

인덱스는 `ReplaceVertices`와 정상 Polygon 적용 seam에서 rebuild한다. 음수 좌표에는
mathematical floor를 사용하고 edge Y 범위에는 `0.0001f` padding을 둔다. edge reference
총량이 평균 16개를 넘거나 입력이 유효하지 않으면 기존 full Polygon scan으로
fallback한다.

Profiler marker와 `QueryCount`, `CandidateEdgeInspectionCount`,
`LastRebuildEdgeReferenceCount`, `ReferenceFallbackCount` 계측으로 후보 축소와
fallback을 확인할 수 있다.

## 주요 소비자

- `InfiniteGrid`: cell 내부 판정, 확장 이벤트, Territory Mesh bounds
- `ResourceSpawnSystem`: 영역 밖 배치 판정과 확장 후 Spawn 계획
- `WorldMonsterSpawnSystem`, `TrackMonsterSpawnSystem`, `Monster`: Territory 상태와 확장 반응
- `SacredZoneSystem`: 확장 후 정화 진행도 재계산
- `StageBootstrapper`: 확장 후 장애물 제거, Sanctuary를 영역 밖에 배치
- `FogOfWarSystem`: `TerritoryVisible` 표현 연결
- Player Runner와 관련 아이템 표현: 확장 상태와 선 너비, 강제 이동 suspend/resume

소비자를 새 논리 영역 구조로 옮길 때는 direct query, event, Mesh bounds와 네트워크
결과가 같은 시점의 상태를 보는지 함께 확인한다.

## 관련 코드와 Asset

- `Assets/02_Scripts/Territory/`
- `Assets/02_Scripts/Territory Refactor/Domain/`
- `Assets/02_Scripts/Territory Refactor/Trail/`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/`의 active Trail 자료구조
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/`
- `GameWorld.unity`, `GamePresentation.unity`, `GameRoot.unity`

## 변경 시 확인

- `Vertices` 갱신이 `ReplaceVertices` 또는 동등한 단일 mutation seam을 통과하는가
- query index와 영역 Mesh가 같은 완료 Polygon revision을 가리키는가
- 재진입 frame에서 Polygon 계산·triangulation을 동기로 실행하지 않는가
- 계산 중 이동·렌더링·Fusion tick이 계속되고 한 번에 하나의 확장만 publish되는가
- Host owner와 Client owner가 predicted Trail, confirmed Trail, 성공·거부·실패
  피드백에서 동등한 체감을 갖는가
- old/new 경로가 event, Mesh, network result 또는 consumer mutation을 중복 실행하지 않는가
- packet budget, revision 검증, recovery retry, teardown 뒤 결과 폐기가 유지되는가
- Trail Chunk 분할의 fixed 좌표, 음수 floor, shared endpoint와 ordered sequence가 유지되는가
- Scene·Prefab을 바꾸면 Territory NetworkObject, LineRenderer와
  `StageBootstrapper` 직렬화 참조가 유효한가

집중 테스트는 `TerritoryContainmentIndexTests`,
`TerritoryBackgroundExpansionWorkerTests`와
`ProjectIO.Territory.ChunkDomain.Tests`의 남아 있는 Trail 테스트다. Fusion RPC나
Networked 필드를 바꾸면 Unity 컴파일뿐 아니라 Fusion Weaver, Host·Client 실행,
실패·복구·teardown도 확인한다.

## 현재 기술 부채와 미래 작업 경계

- `Territory.Vertices`가 public mutable `List<Vector2>`이므로 정상 seam 밖의 직접
  변경은 bounds·containment index를 낡게 만들 수 있다.
- 논리 영역과 비주얼은 `TerritorySystem`에서 함께 publish되며 도메인 계약으로 완전히
  분리되지 않았다.
- Polygon 확장은 source boundary와 Trail 길이에 따라 총 계산량과 전체 결과 전송량이
  증가하고, 완료 시 전체 Mesh를 교체한다.
- `TerritoryTrailSegmentIndex`는 사선 segment의 실제 통과 cell이 아니라 AABB에
  포함되는 모든 8-unit Chunk에 등록하므로 긴 사선에서 후보가 늘 수 있다.
- 현재 내부 판정 인덱스는 Y bucket 후보 축소이며 논리 cell occupancy, Chunk store,
  adaptive quadtree를 제공하지 않는다.

향후 Chunk·Grid·Quadtree 기반 논리 영역과 확장 구조는 별도 설계 요청과 예약에서
정한다. 이 문서는 아직 승인되지 않은 저장 구조, 정밀도, 전환 순서 또는 rollback을
미리 고정하지 않는다.
