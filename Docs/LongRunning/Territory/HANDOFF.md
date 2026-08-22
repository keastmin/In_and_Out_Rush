# Handoff

## Completed outcome

C006 immutable snapshot의 방향성 Boundary를 revision당 한 번 exact fixed loop와
Chunk-local 후보로 색인하는 C008 기반을 구현했다. C003/C004 ordered Trail fragment는
Runner 이동 중 한 번만 append되며 진출·재진입 접점, 외부 Trail, 면적,
self-intersection 후보와 영향 Trail Chunk를 증분 누적한다.

재진입 terminal은 전체 Boundary나 보관 Trail을 다시 순회·복사하지 않는다. Boundary
prefix 면적으로 두 arc를 평가하고 기존 영역보다 커지는 현재 게임 규칙의 큰 후보를
immutable plan으로 공개한다. fixed 좌표를 이동·근사하지 않으며 연속 중복점과 exact
collinear 중간점만 모양 보존 정규화로 제거한다.

이번 slice는 순수 Domain 기반이다. Legacy polygon, C006 store, mesh, Fusion과 모든
consumer runtime은 변경하지 않았다.

## Changed files

- `TerritoryBoundaryLoopIndex`, `TerritoryChunkExpansionSession`
- `TerritoryChunkExpansionPlan`, `TerritoryChunkExpansionMetrics`
- 위 index/session 신규 테스트 2개
- Territory 기능 문서, C008, milestone/handoff/roadmap/test 문서와 Active 작업 문서

## Decisions used

- GPU는 요구사항이나 fallback 계약이 아니다. authoritative geometry는 C001 exact
  fixed CPU domain이 소유하고 GPU는 후속 exact 표시 profile이 필요할 때만 검토한다.
- Boundary index build는 revision당 한 번 허용하되 fragment append와 terminal 전체
  Boundary scan은 금지한다.
- Trail은 이동 중 local Boundary 후보와 1-world-unit spatial cell의 기존 Trail
  후보만 검사한다. 전체 Trail 검사는 terminal로 미루지 않는다.
- plan은 external Trail만 보관한다. 첫 fragment는 마지막 내부점 또는 경계 anchor를
  포함해야 하며 재진입 뒤 inside tail은 확장 경로나 self-intersection에 포함하지 않는다.
- 두 후보 중 기존 면적보다 커지는 후보만 허용하고 큰 면적 후보를 결정적으로 고른다.
- 실패와 Abort는 pending geometry를 소각하며 source snapshot은 절대 변경하지 않는다.

## Verification evidence

- 신규 source와 현재 ChunkDomain source/test 전체 직접 compile: warning 0, error 0
- 현재 회귀와 신규 assertion 직접 실행: 56/56 통과, 신규 16개
- sequence/폐합/음수 Chunk/global overflow와 exact fixed area 통과
- 직선·대각선·concave·Boundary corner·shared Chunk edge Trail 모양 보존 통과
- gap, overlap, self-intersection, 추가 crossing과 Abort 실패 원자성 통과
- 동일 입력의 별도 Host-role/Client-role session plan과 metrics 동일성 통과
- 1000×1000 world, 400개 초과 Boundary segment, 장거리 multi-Chunk Trail stress 통과
- stress terminal Boundary/Trail full scan metrics 각각 0
- Unity import가 stale W-004 mask source 참조를 제거하고 신규 W-005 source/test를
  project file에 포함한 뒤 `dotnet build ProjectIO.slnx` 오류 0개, 기존 warning
  25개로 통과했다.
- 작업자가 Unity compile과 안내된 Territory EditMode 검증 완료를 보고했다.

## Serialized or manual setup

Scene·Prefab·Inspector 연결 변경은 없다. Unity가 신규 script와 `.meta`를 import한 뒤
아래만 확인한다.

1. Unity import가 끝나 csproj에서 제거된 `TerritoryChunkMaskLayout`과
   `TerritoryChunkMaskTileRasterizer` 항목이 사라지고 신규 W-005 script가 포함됐는지
   확인한 뒤 Console compile error가 없는지 확인한다.
2. EditMode `ProjectIO.Territory.Tests` 전체를 실행한다.
3. 신규 `TerritoryBoundaryLoopIndexTests`와 `TerritoryChunkExpansionSessionTests`가 모두
   통과하는지 확인한다.

이번 slice는 runtime에 연결되지 않으므로 Host·Client 플레이 동작과 표시가 바뀌면
회귀다. 별도의 Scene 설정이나 GPU 토글은 만들지 않았다.

## Known risks and failures

- 아직 plan을 changed Chunk coverage로 materialize하거나 C006 store에 적용하지 않으므로
  실제 영역 확장 frame spike와 Legacy 모양 보정은 이번 결과만으로 개선되지 않는다.
- 무한한 변경 면적의 적용 시간을 0으로 만들 수는 없다. 후속 단계는 plan의 Boundary
  arc와 영향 범위를 이용해 변경 coverage 생성을 frame budget으로 분산해야 한다.
- 교차 접점은 fixed grid로 한 번 반올림된다. 추가 tolerance나 해상도 저하는 없으며
  오차 상한은 C001의 축당 1/512 world unit이다.
- 한 1-world-unit spatial cell 안에 비정상적으로 많은 Trail segment가 밀집하면 해당
  cell 후보 비용은 증가한다. 실제 profile로 밀집 경로 문제가 확인되면 더 작은
  index cell 또는 계층형 index를 별도 계약 없이 내부 최적화할 수 있다.
- C006 Full Chunk 개별 sparse entry 압축은 이번 범위 밖이다.

## Remaining legacy consumers

authoritative expansion, changed coverage materialization, containment, mesh, Grid, Fog,
Resource, Monster, vertex replication과 최종 presentation 모두 Legacy 경로다.

## Next bounded milestone

W-005는 Complete다. 다음 별도 예약은 C008 plan을 변경 영향 Chunk의 exact
`Empty`/`Full`/`Boundary` coverage로
materialize하고 frame budget 안에서 원자적으로 C006 store에 적용하는 순수/스케줄링
단계다. runtime 권위 전환, Fusion과 renderer는 그 뒤 분리한다.

## Exact starting files

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryBoundaryLoopIndex.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionPlan.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionSession.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionMetrics.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkStateBuilder.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkStore.cs`
- `Docs/LongRunning/Territory/CONTRACTS.md`의 C006/C008
