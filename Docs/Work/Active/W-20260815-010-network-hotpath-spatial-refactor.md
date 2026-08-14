# W-20260815-010 Network hot-path spatial refactor

Status: Reserved

## 동기화 기준

- Base Commit: 9361abf3be308cc047a7990ae6de5e9228c4a9b9
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Monster and Projectile, Territory, Stage initialization, Fusion AOI.

## 목표

프로파일러에서 확인된 네 CPU 병목을 현재 게임 규칙과 Fusion Authority를
유지하면서 줄인다.

- `WorldMonsterSpawnSystem.FixedUpdateNetwork`: 전체 Spawn record 순회 대신
  플레이어 주변 Chunk 후보와 현재 활성 record만 처리한다.
- `Monster.FixedUpdateNetwork`: 몬스터 tick 빈도를 낮추지 않고 Territory
  containment와 월드 장애물 path query의 반복 선형 탐색을 공간 캐시로 줄인다.
- `TerritorySystem.ExpandTerritoryFromCurrentPath`: Legacy polygon authority를
  유지하되 중복 polygon/mesh 처리와 확장 이벤트 소비자의 containment 비용을
  줄이고, 계산·표현·복제·소비자 구간을 별도 marker로 측정 가능하게 한다.
- Fusion `UpdateAreaOfInterest`: 매 tick `AddPlayerAreaOfInterest` 호출과 기존
  반경은 유지하고, 평면 게임 월드에 맞는 AOI cell 크기를 Host에서 한 번
  설정해 구가 포함하는 3D cell 수를 줄인다.

기존 취소 작업 `W-20260815-009`에는 구현이 남아 있지 않으므로 현재 코드와
새 profiler 결과를 기준으로 다시 설계한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/MonstersAndProjectiles.md`
- `Docs/Features/Territory.md`
- `Docs/Features/StageInitialization.md`
- `Docs/Work/Completed/W-20260815-007-host-peer-territory-performance-refactor.md`
- `Docs/Work/Completed/W-20260815-008-fixed-update-profiler-markers.md`
- `manage-feature-work`
- `photon-fusion-feature`
- `build-chunk-territory`
- `migrate-feature-slice` for the bounded Monster spatial-index extraction only

## 예상 수정 코드

- `Assets/02_Scripts/System/WorldMonsterSpawnSystem.cs`
- `Assets/02_Scripts/Monster/Monster.cs`
- `Assets/02_Scripts/Monster/WorldMonster.cs`
- `Assets/02_Scripts/Territory/Territory.cs`
- `Assets/02_Scripts/Territory/TerritoryVisible.cs`
- `Assets/02_Scripts/System/TerritorySystem.cs`
- `Assets/02_Scripts/Sanctuary/SanctuaryView.cs` only if the Territory vertex
  mutation contract must invalidate the same bounds cache.
- `Assets/02_Scripts/Stage/Network/StageBootstrapper.cs`
- New focused Chunk/obstacle query types under
  `Assets/02_Scripts/Features/Monster/Logic/` and
  `Assets/02_Scripts/Features/Monster/Adapters/Unity/`, with matching `.meta`
  files.
- New focused Territory bounds/result types only under
  `Assets/02_Scripts/Territory Refactor/Domain/`, with matching `.meta` files.
- Focused EditMode tests and their `.meta`/asmdef files only under
  `Assets/02_Scripts/Features/Monster/Tests/` if the extracted pure Chunk index
  can be tested without an `Assembly-CSharp` backward dependency.
- `Docs/Features/MonstersAndProjectiles.md`
- `Docs/Features/Territory.md`
- `Docs/Features/StageInitialization.md` only when the AOI initialization
  contract changes.
- This work document.

## 예약 Scene·Prefab·Data Asset

없음. Scene, Prefab, ScriptableObject, `NetworkProjectConfig.fusion`, Package,
ProjectSettings와 Photon/Fusion third-party source는 수정하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- `TerritorySystem`은 기존 serialized/network facade로 유지한다.
- `TerritorySystem.OnTerritoryExpandedEvent`, `Territory.Vertices`,
  `TryGetCurrentExpansionPath`, 기존 RPC 방향과 payload shape를 유지한다.
- State Authority만 Territory mutation, Monster Spawn/Despawn, AOI 설정과
  authoritative gameplay 결정을 수행한다.
- `StageBootstrapper.FixedUpdateNetwork`의 player별 AOI 등록은 Fusion 계약에
  따라 매 tick 유지한다. 기존 AOI 반경과 관찰 결과를 변경하지 않는다.
- Monster movement/attack/stun simulation cadence와 gameplay threshold는
  변경하지 않는다.

## Territory milestone

- 단일 milestone: Legacy polygon authority를 유지한 spatial containment 및
  expansion-result 처리 최적화.
- Chunk Territory cutover, revision/changed-chunk replication, consumer
  migration과 Legacy 삭제는 범위 밖이다.
- 새 경로가 같은 확장, event, mesh 또는 RPC를 중복 생성하지 않는다.
- rollback은 새 cache/index/result 경로만 제거해 기존 Legacy 호출로 복귀한다.

## 다른 활성 작업과 겹치는 부분

없음. `CheckStart` 기준 `Docs/Work/Active/`에는 README만 있었다.

## 범위 밖

- 네 profiler hot path 밖의 Monster, Resource, Grid, Fog, Sacred Zone, Tower,
  Projectile 또는 player gameplay 변경.
- Network tick rate 저하, Monster AI cadence 저하, AOI 반경 축소.
- Scene/Prefab 직렬화, NetworkObject interest mode, RPC/Networked property,
  Spawn ownership, Late Join 저장 계약 변경.
- Chunk Territory authoritative cutover 또는 Legacy Territory 삭제.

## 완료 조건

- World monster refresh 작업량이 전체 record 수가 아니라 주변 Chunk 후보와
  활성 record 수에 의해 제한되고, Spawn/Despawn/dormant movement 결과가
  기존 규칙을 보존한다.
- Monster tick은 매 Fusion tick 유지하면서 대부분의 safe-zone query가 cached
  bounds에서 조기 종료되고 obstacle query가 공간 broadphase 후보만 검사한다.
- Territory 확장은 한 번만 authoritative mutation/event/mesh/vertex sync를
  만들며 Host, Client, Late Join의 현재 결과와 rollback 경로를 보존한다.
- AOI는 기존 player별 128 반경을 매 tick 등록하면서 설정된 cell 수 감소를
  profiler 또는 AOI gizmo/statistics로 재확인할 수 있다.
- 관련 순수 로직 test, 프로젝트 compile, `git diff --check`, `git status`,
  Host/Client 수동 확인 절차와 profiler before/after marker를 기록한다.

## 실제 변경

예약 Push와 검증 후 작성.

## 검증 결과

예약 Push와 구현 후 작성.

## 남은 위험

- 현재 profiler marker는 하위 Fusion/physics/consumer 시간을 포함한다. 변경
  후에도 비중이 남으면 새 세부 marker로 직접 비용과 하위 비용을 분리한다.
- AOI cell 확대는 관찰 반경을 바꾸지 않지만 cell당 후보 object 수를 늘린다.
  실제 object 분포에서 before/after를 비교하고 악화되면 즉시 기존 기본값으로
  rollback한다.
- Legacy polygon의 공개 mutable vertex list 때문에 모든 현재 mutation seam이
  cache invalidation을 거치는지 정적 검색과 Host/Client 확장으로 확인해야 한다.
