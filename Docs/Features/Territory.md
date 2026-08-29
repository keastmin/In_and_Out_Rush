# Territory

Status: Polygon authoritative, immutable spatial query index, background expansion and network Trail active

Last reviewed: 2026-08-29

## 책임과 계약

Territory는 완료된 Polygon 논리 상태와 내부 판정·확장 규칙을 소유한다. Territory.Vertices는 private list의 read-only snapshot이며, ReplaceVertices만 새 snapshot과 immutable-per-rebuild TerritorySpatialIndex를 함께 준비해 원자 교체한다. authoritative 상태는 여전히 완료된 전체 Polygon snapshot과 revision이다.

IsPointInPolygon과 IsPointOnBoundary의 공개 계약, TryCalculateExpansion, OnTerritoryExpandedEvent, Mesh publish 시점은 유지한다. 경계는 inside, epsilon은 0.0001f, 음수 좌표는 mathematical floor, concave polygon 의미를 보존한다.

## 공간 인덱스

Assets/02_Scripts/Features/Territory/Logic/의 TerritorySpatialIndex는 8 world-unit sparse chunk hash를 사용하고, edge가 있는 boundary chunk 내부에 adaptive quadtree를 만든다. point query는 bounds reject 후 Y row의 정렬 chunk와 quadtree 후보를 조회해 boundary/ray-crossing exact test를 수행한다. segment query는 supercover/DDA로 실제 통과 chunk를 열거하고 quadtree 후보를 반환한다. 기존 TerritoryContainmentIndex와 TerritoryBoundsIndex 경로 및 full-scan runtime fallback은 이 교체에서 제거한다.

Polygon edge 후보 검색은 확장 boundary 교차와 Polygon simple 검사에 재사용되고,
ear-clipping은 Chunk row 기반 vertex 후보만 검사한다. 동적 Trail 자기 교차는 Quadtree를
매 append마다 재구축하지 않고 같은 supercover/DDA traversal로 실제 통과 Chunk의 segment만
exact 검사한다. Trail renderer의 fixed 좌표·shared endpoint·ordered sequence 계약은 유지한다.

계측은 `TerritorySpatialIndex`의 `QueryCount`, `CandidateEdgeInspectionCount`,
`LastCandidateEdgeCount`, `ChunkCount`, `NodeCount`로 query 후보 축소와 build 규모를 확인한다.

## 현재 소유권

| 책임 | 현재 소유자 |
|---|---|
| 완료 Polygon snapshot과 query facade | `Territory` |
| Chunk·Quadtree edge query | `TerritorySpatialIndex`, `TerritorySpatialChunk`, `TerritorySpatialQuadtree` |
| 확장 boundary 조립과 후보 선택 | `TerritoryExpansionCalculator` |
| Polygon 검증과 triangulation | `TerritoryPolygonTriangulator` |
| 확장 경로와 자기 교차 | `TerritoryExpansionSession`, `TerritoryTrailSegmentIndex` |
| background 계산 | `TerritoryBackgroundExpansionWorker`과 WorkItem/Result |
| 확장 결과 복제·복구 | `TerritoryExpansionReplication`, Result Packetizer/Replica |
| confirmed Trail 기록·복제 | `TerritoryTrailShadowRecorder`, `TerritoryTrailReplicationStream` |
| 영역과 Trail 표현 | `TerritoryVisible`, `TerritoryTrailChunkRenderer` |

## 확장·복제 흐름

State Authority가 검증과 확장 성공을 결정하고 background worker가 계산한다. 완료 결과는 source revision을 확인한 뒤 Polygon snapshot, spatial index, Mesh, revision 및 event를 한 번에 적용한다. 실패·취소·stale 결과는 이전 완료 상태를 유지한다.

Fusion은 기존 전체 vertex/triangle snapshot을 Reliable Begin/Data/Complete로 복제하고 advertised revision·pending·recovery 계약을 유지한다. visual/Fusion RPC, AOI, Trail prediction/confirmation은 변경하지 않으며 새 인덱스는 네트워크 상태가 아니다.

- 정상·복구 전송은 data packet당 최대 48 word, simulation tick당 합계 최대 2 data packet을 유지한다.
- Proxy는 Begin/Data/Complete와 source/revision/sequence를 모두 검증한 뒤에만 결과를 적용한다.
- advertised revision보다 뒤처진 Proxy는 targeted Reliable recovery를 요청하고 outstanding
  요청이 2초 동안 수렴하지 않으면 재시도한다.
- 제품 세션은 스테이지 시작부터 함께하는 최대 2인 전제이며 일반 Late Join·reconnect는
  이번 계약에 포함하지 않는다.
- Territory NetworkObject의 global interest, State Authority 단일 mutation,
  Host/Client owner의 predicted·confirmed Trail 체감은 유지한다.

## 확장과 Trail 수명주기

1. Runner가 밖으로 나가면 owner가 predicted Trail을 표시하고 State Authority가 confirmed
   fixed Trail을 기록한다.
2. 새 segment는 DDA가 반환한 실제 통과 Chunk에만 등록되며 이전 Trail과 exact 교차한다.
3. 재진입 시 source Polygon revision과 계산용 Trail snapshot 한 건만 background worker에 예약한다.
4. worker는 spatial boundary 검색, 두 확장 후보 검증과 triangulation, result packetization을 수행한다.
5. main thread는 source revision을 재검증하고 Mesh 적용 성공 뒤 Polygon snapshot/index를 교체한
   다음 확장 event를 한 번 발생시킨다.
6. 실패·취소·teardown·stale 결과는 마지막 완료 Polygon과 index를 유지한다.

외부 강제 이동 중에는 State Authority가 Trail을 suspend하고 Reliable lifecycle을 보내며,
owner prediction은 마지막 confirmed point로 reconcile한다. 강제 이동 구간은 확장선에 포함하지 않는다.

## 주요 진입점과 소비자

- 진입점: TerritorySystem.Territory, TerritorySystem.TerritoryVisible, Territory.IsPointInPolygon, Territory.IsPointOnBoundary, Territory.ReplaceVertices
- 소비자: InfiniteGrid와 obstacle spawner, ResourceSpawnSystem/ResourceZone,
  WorldMonsterSpawnSystem/TrackMonsterSpawnSystem/Monster, SacredZoneSystem, SanctuaryView,
  StageBootstrapper, FogOfWarSystem, Player Runner와 관련 아이템 표현
- 코드: Assets/02_Scripts/Territory/, Assets/02_Scripts/Territory Refactor/, Assets/02_Scripts/Features/Territory/Logic/

Scene·Prefab의 `TerritorySystem`, `TerritoryVisible`, LineRenderer 직렬화 참조는 변경하지 않는다.

## 검증 방법

boundary·concave·음수 좌표·대각선·긴 segment·randomized point를 reference geometry와 비교하고, index candidate/노드/쿼리 계측과 warm-up 후 allocation을 확인한다. 확장 성공·자기 교차 실패·triangulation, Trail shared endpoint, packet/revision 회귀를 집중 테스트한다. Unity/Fusion compile 및 Host·Client에서 exit→Trail→re-entry, 실패, 단일 event/Mesh/revision 적용, teardown stale 결과 폐기를 확인한다.

## 범위 밖과 rollback

Polygon 전체 snapshot을 Chunk delta 네트워크로 바꾸거나 raster authority, 일반 Late Join/reconnect, visual shader와 몬스터 규칙은 범위 밖이다. 문제 시 ReplaceVertices 교체 경계에서 이전 query 구현을 복원하거나 새 index를 비활성화해 Polygon authoritative 결과와 네트워크 계약을 보존한다.

남는 비용은 전체 Polygon snapshot 전송과 완료 Mesh 전체 교체, revision 적용 시 index rebuild다.
경계가 매우 길거나 한 Chunk에 edge가 집중되면 Quadtree leaf 후보와 build 메모리가 증가할 수
있으므로 위 계측과 Profiler marker(`TerritorySpatialIndex.Rebuild`, `Contains`,
`SegmentCandidates`)로 실제 Stage 데이터를 확인한다.
