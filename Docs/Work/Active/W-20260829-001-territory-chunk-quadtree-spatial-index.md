# W-20260829-001 Territory Chunk·Quadtree 공간 인덱스 교체

Status: Reserved

## 동기화 기준

- Base Commit: 5a845968c66cb0701f01ee385250ae9180cf904f
- 공용 Upstream: `origin/rebuild-development-environment`

## 담당자

Codex 메인 오케스트레이터. Architecture, Territory 정확성 계약, Fusion Authority 경계와
최종 통합은 메인 에이전트가 판단한다. 하위 에이전트는 서로 겹치지 않는 사용처 조사,
확정된 구조의 소규모 구현과 집중 검증만 담당한다.

## 기능

Territory 논리 영역 판정, Polygon 확장 계산, Trail 자기 교차 판정의 공간 검색 경로.

## 목표

- 현재 1차원 Y bucket과 full Polygon scan fallback을 전역 8 world-unit Chunk hash 및
  각 boundary Chunk 내부의 적응형 Quadtree로 교체한다.
- `Vector3`/`Vector2` 지점 판정은 전체 정점 수가 아니라 해당 Chunk row와 Quadtree가
  반환한 경계 후보 수에 비례하도록 한다.
- 확장 Trail과 Territory boundary 교차 검색, 후보 Polygon의 self-intersection 검증,
  triangulation 후보 검색이 전체 segment·vertex 이중 순회를 피하도록 같은 공간 분할
  원칙을 적용한다.
- 긴 사선 Trail의 자기 교차 후보 Chunk를 AABB 직사각형 전체가 아니라 실제 segment가
  통과하는 Chunk로 제한한다.
- 논리 상태와 표현을 분리한다. `Territory`는 private Polygon snapshot과 revision별
  immutable spatial index를 소유하고, `TerritoryVisible`은 완료된 mesh 표현만 유지한다.
- 기존 `Territory.IsPointInPolygon`, 확장 완료 event, 전체 Polygon 결과 복제와 Mesh
  publish 시점은 호환 seam으로 유지해 모든 기존 소비자를 한 번에 안전하게 전환한다.

### 설계 계약

- Chunk와 Quadtree는 배타적 대안이 아니다. Chunk hash가 무한 월드의 top-level sparse
  partition을 맡고, Quadtree는 edge가 들어 있는 각 Chunk 내부 후보를 적응적으로 줄인다.
- authoritative gameplay revision과 네트워크 snapshot은 계속 완료된 Polygon이다.
  Spatial index는 같은 Polygon에서 원자적으로 재구축되는 논리 query representation이며
  별도 네트워크 상태나 별도 성공 결과를 만들지 않는다.
- boundary는 기존과 같이 inside로 간주하고 `0.0001f` epsilon, 음수 좌표 mathematical
  floor, concave Polygon 의미를 보존한다. 근사 raster occupancy로 판정 의미를 바꾸지 않는다.
- point containment는 bounds reject 후 현재 Chunk와 같은 Chunk row의 boundary Chunk를
  정렬 검색하고, Quadtree edge 후보에만 boundary/ray-crossing exact test를 수행한다.
- segment query는 supercover/DDA traversal로 실제 통과 Chunk만 방문한 뒤 각 Chunk의
  Quadtree에서 AABB/segment 후보를 얻고 원래 float geometry로 최종 판정한다.
- Polygon 교체는 vertex snapshot과 spatial index를 먼저 완성한 후 한 번에 publish한다.
  실패·취소·stale background 결과는 이전 snapshot과 index를 함께 보존한다.
- 새로운 query/calculation 경로가 성공 결과, event, mesh 또는 RPC를 중복 publish하지
  않으며, 기존 index와 full-scan fallback은 전환 완료 후 제거한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `manage-feature-work`
- `build-chunk-territory`
- `replace-existing-feature`
- `photon-fusion-feature`

## 예상 수정 코드

기존 파일:

- `Assets/02_Scripts/Territory/Territory.cs`
- `Assets/02_Scripts/Territory/Territory.cs.meta`
- `Assets/02_Scripts/Territory/TerritoryContainmentIndex.cs`
- `Assets/02_Scripts/Territory/TerritoryContainmentIndex.cs.meta`
- `Assets/02_Scripts/Territory Refactor/Domain/TerritoryBoundsIndex.cs`
- `Assets/02_Scripts/Territory Refactor/Domain/TerritoryBoundsIndex.cs.meta`
- `Assets/02_Scripts/Territory Refactor/Trail/TerritoryTrailSegmentIndex.cs`
- `Assets/02_Scripts/Territory Refactor/Tests/Editor/TerritoryContainmentIndexTests.cs`
- `Assets/02_Scripts/Territory Refactor/Tests/Editor/TerritoryContainmentIndexTests.cs.meta`
- `Assets/02_Scripts/Territory Refactor/Tests/Editor/TerritoryBackgroundExpansionWorkerTests.cs`

새 논리 구현과 테스트(각 `.cs.meta` 포함):

- `Assets/02_Scripts/Features/Territory/Logic/TerritorySpatialEdge.cs`
- `Assets/02_Scripts/Features/Territory/Logic/TerritorySegmentTraversal.cs`
- `Assets/02_Scripts/Features/Territory/Logic/TerritorySpatialQuadtree.cs`
- `Assets/02_Scripts/Features/Territory/Logic/TerritorySpatialChunk.cs`
- `Assets/02_Scripts/Features/Territory/Logic/TerritorySpatialIndex.cs`
- `Assets/02_Scripts/Features/Territory/Logic/TerritoryExpansionCalculator.cs`
- `Assets/02_Scripts/Features/Territory/Logic/TerritoryPolygonTriangulator.cs`
- `Assets/02_Scripts/Territory Refactor/Tests/Editor/TerritorySpatialIndexTests.cs`
- `Assets/02_Scripts/Territory Refactor/Tests/Editor/TerritoryExpansionSpatialIndexTests.cs`
- `Assets/02_Scripts/Territory Refactor/Tests/Editor/TerritoryTrailSegmentIndexTests.cs`
- 새 폴더 `Assets/02_Scripts/Features/Territory/`, `Logic/`의 `.meta`

문서:

- `Docs/Decisions/ADR-0005-Territory-청크-쿼드트리-공간-인덱스.md`
- `Docs/Features/Territory.md`
- `Docs/PROJECT_MAP.md`
- 이 작업 문서

## 예약 Scene·Prefab·Data Asset

없음. 기존 `TerritorySystem`, `TerritoryVisible`, `GameWorld` NetworkObject와 serialized
field를 유지한다. 조사 중 Asset 수정이 필요해지면 변경 전에 예약 범위를 갱신한다.

## 공용 계약 또는 Bootstrapper 변경

- `Territory.IsPointInPolygon(Vector2)`, `Territory.IsPointOnBoundary(Vector2)`,
  `Territory.TryCalculateExpansion(...)`, `Territory.ReplaceVertices(...)`의 호출 계약은 유지한다.
- `Territory.Vertices`는 외부 직접 mutation을 막는 read-only 공개 snapshot으로 바꾸되,
  현재 소비자의 Count/index/enumeration 사용은 유지한다.
- `TerritorySystem.OnTerritoryExpandedEvent`와 `StageBootstrapper` 연결은 변경하지 않는다.
- 실제 소비자 파일은 API 호환으로 수정하지 않는 것을 우선한다. 컴파일상 변경이 필요한
  소비자가 발견되면 해당 파일을 건드리기 전에 예약을 갱신한다.

## 네트워크·Peer 동등성

- Input Authority owner가 기존처럼 Runner 이동 의도와 predicted Trail을 생성한다.
- State Authority만 위치를 검증하고 자기 교차, exit/re-entry, 확장 성공과 Polygon revision을
  결정한다. 새 spatial index는 이 authoritative 계산 내부에서만 사용한다.
- 성공 결과는 기존 전체 vertex/triangle Reliable Begin/Data/Complete와 advertised revision으로
  Host·Client에 돌아간다. 실패·취소·stale 결과는 기존 마지막 완료 Polygon을 유지한다.
- Host 로컬과 Client Input Authority의 predicted/confirmed Trail, 성공, 거부·실패 feedback
  계약을 변경하지 않으며 Host가 index를 두 번 적용하거나 event를 중복 발생시키지 않는다.
- 기존 normal/recovery packet budget, source revision 검증, pending 상태, teardown cancellation,
  제품의 2인 시작 동시 참가 전제를 유지한다. 새 Networked field/RPC/AOI 계약은 추가하지 않는다.
- 자동 검증은 순수 geometry 동등성, background worker, packet/replica 회귀와 Unity/Fusion compile을
  수행한다. 실제 Host·Client는 Host owner와 Client owner 각각 exit→Trail→re-entry 성공 1회,
  자기 교차 실패 1회, event/mesh/revision 단일 적용, teardown 뒤 stale 결과 미적용을 확인한다.
  환경에서 두 Peer를 실행하지 못하면 이 런타임 항목을 명시적으로 미검증으로 남긴다.

## 다른 활성 작업과 겹치는 부분

없음. `CheckStart` 시 Active에는 안내용 `Docs/Work/Active/README.md`만 존재했다.

## 범위 밖

- Territory visual shader/색상/LineRenderer 디자인 변경
- Polygon 전체 snapshot을 Chunk delta 네트워크 프로토콜로 교체
- 일반 Late Join·reconnect 지원 추가
- Grid cell 크기에 맞춘 근사 raster Territory로 gameplay 의미 변경
- 몬스터 AI 규칙, spawn 확률, safe-zone sampling 규칙 변경
- Scene·Prefab 재배치와 기존 Territory 디렉터리의 대량 이동

## 완료 조건

- 기존 공개 query와 모든 소비자가 새 Chunk·Quadtree index를 사용하고 old Y-bucket/bounds
  index 및 full-scan runtime fallback이 제거된다.
- boundary/concave/음수 좌표/긴 사선/수천 정점/randomized point가 reference 결과와 동등하다.
- query warm-up 후 managed allocation이 없고, 대형 Polygon에서 inspected candidate 수가 전체
  정점 순회보다 유의미하게 작음을 계측 테스트로 확인한다.
- expansion boundary 교차와 candidate self-intersection/triangulation이 spatial 후보 검색을
  사용하며 기존 성공·실패 geometry 회귀 테스트를 통과한다.
- Trail 자기 교차 index가 실제 통과 Chunk를 사용하고 shared endpoint 예외와 기존 kill/lifeline
  결과가 유지된다.
- Polygon snapshot, spatial index, mesh, revision, event가 같은 완료 결과를 바라보며 old/new
  경로의 중복 실행이 없다.
- 집중 Territory 테스트, Unity/Fusion compile, `git diff --check`, 정확한 변경 파일 검사를
  묶어서 완료한다.
- 자동화할 수 없는 Host·Client 실행 항목과 성능 측정 한계를 정확히 기록한다.

## 실제 변경

예약 단계. 구현 후 기록한다.

## 검증 결과

예약 단계. 구현 후 기록한다.

## 남은 위험

- Polygon vertex 수가 매우 클 때 결과 snapshot 전송량과 전체 Mesh 교체 비용은 이번 query
  index 교체와 별개의 병목으로 남을 수 있다.
- adaptive index build 비용과 메모리는 boundary 길이·분포에 영향을 받으므로 후보 수,
  Chunk 수, node 수를 계측하고 비정상 입력에 deterministic failure를 둔다.
- 정확한 경계 epsilon과 Chunk 경계의 supercover 누락은 false negative 위험이 있으므로
  Chunk 경계·대각선·음수 좌표 회귀 테스트를 우선한다.
