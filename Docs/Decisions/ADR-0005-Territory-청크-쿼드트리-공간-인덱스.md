# ADR-0005: Territory 청크·쿼드트리 공간 인덱스

- 상태: 채택
- 날짜: 2026-08-29
- 범위: Territory 내부 판정, 확장 교차 후보, Polygon 검증/삼각분할 후보, Trail segment 검색

## 결정

authoritative 상태는 완료된 Polygon의 전체 정점/삼각형 snapshot과 revision으로 유지한다. `Territory`는 외부에서 수정할 수 없는 정점 snapshot을 보유하고, `ReplaceVertices`에서 다음 snapshot과 `TerritorySpatialIndex`를 모두 완성한 뒤 원자적으로 교체한다. 실패·취소·stale 결과는 이전 snapshot과 인덱스를 유지한다.

`TerritorySpatialIndex`는 8 world-unit 단위 sparse chunk hash를 최상위 분할로 사용한다. 경계 edge가 포함된 chunk만 만들고, 각 chunk 안에서는 adaptive quadtree로 edge AABB 후보를 줄인다. point query는 bounds reject 후 해당 row의 정렬된 boundary chunk와 quadtree 후보를 조회하고, boundary/ray-crossing은 원본 float geometry로 정확히 판정한다. segment query는 supercover/DDA로 실제 통과 chunk를 열거한 뒤 후보를 exact 교차 검사한다.

Polygon query 경로는 확장 boundary 교차와 self-intersection 검사에 재사용하고,
ear-clipping은 같은 Chunk row 원칙의 vertex 후보 인덱스를 사용한다. 동적으로 append되는
Trail segment는 Quadtree를 매번 재구축하지 않고 같은 supercover/DDA Chunk traversal 뒤
Chunk별 segment 후보를 exact 검사한다. 근사 raster occupancy는 authoritative 판정에
사용하지 않는다.

## 선택 이유와 대안

sparse chunk는 무한 월드·음수 좌표를 deterministic floor로 처리하면서 빈 공간 구조를 만들지 않는다. boundary chunk 내부에만 quadtree를 만들어 uniform grid의 빈 cell 비용을 줄이고, DDA로 긴 사선의 실제 통과 chunk만 방문한다. 순수 uniform grid는 빈 cell과 후보 중복이 커지고, 전역 quadtree는 무한 범위 관리가 복잡하다. raster authority는 경계 오차로 Polygon 의미를 바꾸므로 채택하지 않았다.

## 불변식

1. State Authority만 확장 성공과 authoritative Polygon revision을 결정한다.
2. Proxy에는 기존처럼 전체 vertex/triangle snapshot을 Reliable Begin/Data/Complete로 복제한다.
3. snapshot, spatial index, mesh, revision, expanded event는 같은 완료 결과를 가리킨다.
4. 경계는 inside이며 epsilon은 `0.0001f`, 음수 chunk 좌표는 mathematical floor다.
5. 새 경로와 legacy 경로가 mutation, event, mesh 또는 RPC를 중복 실행하지 않는다.

## 롤백

문제 발생 시 `ReplaceVertices` 원자 교체 지점에서 이전 Polygon snapshot/index 경로로 되돌린다. 네트워크 snapshot, revision, RPC, visual/Fusion 연결은 변경하지 않았으므로 인덱스 구현을 비활성화하거나 이전 query 구현을 복원해 authoritative 결과를 보존할 수 있다. 회귀 시 boundary·concave·음수 좌표·대각선·randomized geometry와 Host/Client 확장 흐름을 비교한다.
