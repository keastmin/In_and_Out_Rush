# Handoff

## Completed outcome

C008 plan의 exact Trail과 실제로 교체되는 기존 Boundary arc만 이용해 추가 영역을
materialize하는 C009 순수 Domain 기반을 구현했다. 긴 edge도 Chunk part 하나씩,
scanline과 Boundary 제외도 결정적 primitive work로 나뉘며
`TryStep(maxWorkUnits)`가 호출 budget을 넘지 않는다.

Boundary Chunk는 새 Trail과 captured-region 교체 arc의 fixed local segment 및 제거
source sequence를 보존한다. Boundary가 없는 넓은 내부는 개별 Full Chunk 객체가 아니라
행별 inclusive `(Y, MinX..MaxX)` run으로 압축한다. source snapshot의 Full Chunk와
전체 Boundary를 순회·복사하거나 전역 sequence를 재번호화하지 않는다.

완료 전 candidate는 공개되지 않고 Abort, stale revision, malformed arc와 overflow는
candidate를 소각한다. 같은 입력은 budget 분할과 Host·Client 역할에 무관한 동일 결과를
만든다. 이번 slice는 C006 store, Legacy runtime, Fusion, renderer를 변경하지 않았다.

## Changed files

- `TerritoryChunkExpansionMaterializationSession`
- `TerritoryChunkExpansionMaterialization`, `TerritoryChunkExpansionMaterializationMetrics`
- `TerritoryChunkBoundaryEdit`, `TerritoryChunkFillRun`
- 위 materialization과 run 신규 테스트 2개
- Territory 기능 문서, C009, milestone/handoff/roadmap/test 문서와 Active 작업 문서

## Decisions used

- authoritative geometry 계산은 C001 fixed CPU domain 한 경로만 소유한다. GPU/CPU
  fallback은 만들지 않는다.
- 추가 영역은 Trail과 교체 arc만 읽는다. 유지되는 source Boundary 전체와 Full Chunk
  map은 materialization 입력 처리에서 순회하지 않는다.
- 긴 선분의 Chunk 분할 자체도 작업 예산에 포함해 한 `TryStep`에서 전개하지 않는다.
- Boundary Chunk는 exact segment를 유지하고 내부만 row run으로 압축한다. 해상도,
  tolerance, vertex budget과 근사 단순화는 없다.
- algorithmic metrics에는 step 호출 횟수나 budget 크기를 넣지 않아 budget 분할이
  달라도 동일 결과와 metrics를 만든다.
- compact 결과를 C006 개별 Full entry와 전역 sequence로 다시 펼치는 적용은 금지하고
  후속 persistent store가 run과 Boundary edit를 직접 보존하도록 한다.

## Verification evidence

- 신규 source와 현재 ChunkDomain source/test 전체 직접 compile: warning 0, error 0
- 신규 assertion 10개 포함 현재 ChunkDomain 회귀 직접 실행: 60/60 통과
- exact Trail/교체 arc, same-sequence 접점, 음수·대각선과 local segment 연속성 통과
- budget 1/10000 최종 결과·metrics 동일성과 호출별 work budget 상한 통과
- 완료 전 비공개, Abort/stale revision 소각과 session 재사용 뒤 이전 결과 불변 통과
- 1000×1000 stress에서 source Boundary/Full scan과 renumber metrics 각각 0,
  Full 저장량이 행 run 수에 비례함을 확인
- Unity import/compile과 안내된 Territory EditMode 전체 검증을 작업자가 완료
- 신규 source를 포함한 `dotnet build ProjectIO.slnx --no-restore` 오류 0개, 기존
  warning 21개

## Serialized or manual setup

Scene·Prefab·Inspector 연결 변경은 없다. Unity가 신규 script와 `.meta`를 import한 뒤
아래만 확인한다.

1. Console compile error가 없는지 확인한다.
2. EditMode `ProjectIO.Territory.Tests` 전체를 실행한다.
3. 신규 `TerritoryChunkFillRunTests`와
   `TerritoryChunkExpansionMaterializationSessionTests`가 모두 통과하는지 확인한다.

이번 slice는 runtime에 연결되지 않으므로 Host·Client 플레이, Territory 모양과 표시가
바뀌면 회귀다. Scene 설정이나 GPU 토글은 추가하지 않았다.

## Known risks and failures

- compact 결과를 authoritative store에 적용하지 않았으므로 실제 gameplay frame 비용과
  Legacy 모양 보정은 아직 바뀌지 않는다.
- C006은 여전히 개별 Full sparse entry와 전역 연속 Boundary sequence를 사용한다.
  C009 결과를 이 구조로 전개하면 장기 목표를 훼손하므로 후속 store 계약 변경이
  필요하다.
- bounded work는 frame spike 상한을 만들지만 전체 계산 시간을 0으로 만들지 않는다.
  후속 Job/Burst adapter와 runtime profile로 완료 지연을 검증해야 한다.
- scanline active edge의 ordered set 연산은 edge당 로그 비용이다. 비정상적으로 복잡한
  한 행의 경계가 profile 병목이면 동일 C009 결과를 유지한 Burst-friendly 배열
  scheduler로 옮긴다.

## Remaining legacy consumers

authoritative expansion, compact store apply, containment, mesh, Grid, Fog, Resource,
Monster, vertex replication과 최종 presentation 모두 Legacy 경로다.

## Next bounded milestone

W-006의 Unity 검증과 최종 Commit·Push가 끝난 뒤 별도 예약으로 C009 Boundary edit와 Full
run을 직접 보존하는 persistent Chunk store apply를 설계한다. C006 전역 sequence와
개별 Full entry로 재전개하지 않으며, Job/Burst frame scheduling은 저장 계약이 고정된
뒤 같은 결과를 실행하는 adapter로 분리한다. runtime 권위 전환, Fusion과 renderer는
그 뒤 milestone이다.

## Exact starting files

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionMaterialization.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionMaterializationSession.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkBoundaryEdit.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkFillRun.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkSnapshot.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkStore.cs`
- `Docs/LongRunning/Territory/CONTRACTS.md`의 C006/C009
