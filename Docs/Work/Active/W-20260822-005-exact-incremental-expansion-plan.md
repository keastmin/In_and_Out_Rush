# W-20260822-005 정밀 증분 영역 확장 계획 기반

Status: Reserved

## 동기화 기준

- Base Commit: 5a57efdf6fcc921b915e736bf52b7a2492e6150c
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Territory의 정밀 Chunk 경계 색인과 러너 Trail 증분 확장 계획.

## 목표

GPU 사용 여부나 현재 Legacy 전역 polygon 구현을 출발점으로 삼지 않고, 제품 목표에
직접 필요한 확장 계산 기반을 만든다. C006 immutable Chunk snapshot의 방향성 Boundary
segment를 revision마다 한 번만 순서화·색인하고, 이미 C003/C004로 Chunk 분할된 러너
Trail fragment를 이동 중에 순서대로 증분 처리한다.

정상 재진입 terminal에서는 기존 영역 전체와 긴 Trail 전체를 다시 교차 검사하거나
복사하지 않고, 정확한 진출·재진입 접점, 사용할 기존 경계 arc 범위와 새 Trail 범위를
참조하는 immutable expansion plan을 확정한다. 이 단계는 계획을 실제 Chunk coverage,
mesh 또는 gameplay 상태로 적용하지 않는 순수 기반이며 Legacy가 계속 권위다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- `Docs/Work/Completed/W-20260822-004-gpu-chunk-mask-presentation.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/build-chunk-territory/references/work-session-protocol.md`

## 예상 수정 코드

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryBoundaryLoopIndex.cs` 신규 및 `.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionPlan.cs` 신규 및 `.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionSession.cs` 신규 및 `.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionMetrics.cs` 신규 및 `.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryBoundaryLoopIndexTests.cs` 신규 및 `.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkExpansionSessionTests.cs` 신규 및 `.meta`
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- 이 작업 문서

기존 공개 Chunk 타입의 변경이 불가피하면 구현 전에 이 예약의 정확한 파일 범위를
갱신하고 작업자의 다음 진행 요청 및 원격 예약 검증을 거친다.

## 예약 Scene·Prefab·Data Asset

없음. Scene, Prefab, Material, Shader, Compute Shader, ScriptableObject, asmdef, Package,
ProjectSettings와 Inspector 참조를 변경하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- C001-C007 좌표, Chunk ownership, Trail 순서·수명과 고정 2인 복제 계약은 변경하지
  않는다.
- 새 C008은 GPU/CPU 표시 계약이 아니라 `정밀 증분 확장 계획` 계약으로 정의한다.
- Boundary loop index는 C006의 global segment sequence와 fixed local endpoint를
  검증해 하나의 닫힌 방향성 loop와 Chunk별 후보 lookup을 만든다. index 생성은
  revision당 한 번 허용하지만 Trail append/terminal에서 전체 Boundary scan은 금지한다.
- Trail 좌표는 C001 fixed 정밀도 이후 추가 tolerance, vertex budget, 곡선 단순화나
  점 이동을 적용하지 않는다. 연속 중복점과 정확히 같은 직선 위 중간점만 모양을
  바꾸지 않는 정규화로 허용한다.
- expansion session은 ordered fragment를 append할 때 접촉 후보, Trail 누적 면적과
  영향 Chunk를 갱신한다. terminal은 보관 Trail 전체를 다시 순회하지 않고 두 경계
  arc 후보의 면적을 prefix 정보로 평가한다.
- plan은 기존 Boundary sequence 범위, 새 fixed Trail 범위, 진출·재진입 접점,
  선택된 후보와 진단 work count를 읽기 전용으로 제공한다. snapshot mutation,
  triangulation, mesh, renderer, RPC 또는 consumer event를 만들지 않는다.
- fixed 교차 계산 overflow, 경계 sequence gap, 열린/복수 loop, Trail gap,
  경계 overlap, 진출·재진입이 정확히 한 쌍이 아닌 입력과 expanded-area가 증가하지
  않는 후보는 실패시키며 기존 상태를 변경하지 않는다.

## 네트워크·Peer 동등성

이 slice는 Fusion 상태, RPC payload, Authority와 현재 Trail 표시를 변경하지 않는다.
Input Authority owner prediction, State Authority confirmed Trail과 Proxy 표시에는 영향이
없으며 Legacy polygon이 계속 게임 결과를 결정한다.

순수 planner는 같은 C006 snapshot revision과 C003/C004 ordered fragment에 대해 Peer
역할과 무관하게 byte-equivalent fixed plan을 만들어야 한다. 실제 State Authority
적용, Client 결과 반환, 예측·확정 전환과 동시 표시는 후속 통합 milestone에서 별도
예약하고 Host·Client runtime으로 검증한다.

## 다른 활성 작업과 겹치는 부분

`CheckStart` 결과 원격과 동기화됐고 `Docs/Work/Active/`에는 안내용 README 외 예약이
없어 겹침이 없다.

## 범위 밖

- `TerritorySystem`, `Territory`, `TerritoryVisible` runtime 연결 또는 Legacy 동작 변경
- plan을 changed `Empty`/`Full`/`Boundary` coverage로 materialize하거나 C006 store에 Commit
- Job System/Burst adapter, background scheduling과 frame별 budget
- mesh 생성, exact Chunk renderer, GPU/CPU mask와 Shader
- Fusion RPC/packet/revision 변경과 Host·Client predicted expansion 공개
- containment, Grid, Fog, Resource, Monster 등 consumer migration
- Legacy polygon/vertex RPC/mesh 삭제와 authoritative cutover
- Late Join, reconnect, AOI recovery와 다수 Peer 보안 확장

## 완료 조건

- C006 Boundary sequence를 exact global fixed loop로 복원하고 sequence gap, endpoint
  불연속, 열린/복수 loop와 overflow를 원자적으로 거부한다.
- 음수 Chunk와 Chunk 모서리·경계 위 endpoint에서도 Chunk lookup이 같은 접점을
  중복 없이 찾는다.
- ordered Trail fragment를 한 번만 append해 진출·재진입 접점과 영향 Chunk를
  누적하고 Abort/reset은 모든 pending plan 데이터를 소각한다.
- 직선, 대각선, 곡선 표본, concave 기존 영역과 긴 다중-Chunk Trail에서 선택된
  expansion plan의 fixed 경로가 입력 Trail을 이동·삭제·근사하지 않는다.
- 두 후보 중 기존 면적보다 실제 확장되는 현재 게임 규칙의 큰 면적 후보를 고르며,
  invalid/self-intersecting/overlap/추가 boundary crossing은 plan을 공개하지 않는다.
- 1000×1000 world 범위, 큰 Boundary loop와 장거리 Trail stress에서 index build 외
  append가 현재 fragment의 local Boundary 후보 수에 비례하고 terminal의 전체
  Boundary/Trail rescan count가 0임을 deterministic work metrics로 검증한다.
- 동일 snapshot/fragment 입력을 Host 역할과 Client 역할에 해당하는 별도 session에
  적용했을 때 plan과 실패 reason이 동일하다.
- 신규 순수 테스트와 기존 ChunkDomain 회귀, project compile, `git diff --check`가
  통과하고 Scene·Prefab·설정·Shader·asmdef diff가 없다.

## 실제 변경

예약 단계. 구현 후 기록한다.

## 검증 결과

예약 단계. 구현 후 기록한다.

## 남은 위험

- 이번 slice는 계산 plan만 만들므로 실제 확장 frame 비용을 아직 제거하지 않는다.
  후속 changed-coverage materialization과 scheduling이 완료돼야 runtime 개선이 생긴다.
- 임의로 무한한 경로와 변경 면적을 계산 시간 0으로 만들 수는 없다. 후속 단계는
  이번 증분 plan을 이용해 이동 중에 비용을 분산하고 변경 범위만 materialize해야 한다.
- 기존 C006 snapshot은 Full Chunk를 개별 sparse entry로 보관한다. 매우 넓은 내부
  영역 압축이 필요하다는 profile 근거가 생기면 별도 계약과 milestone으로 다룬다.
