# W-20260821-010 Chunk Territory 도메인 기반

Status: Completed

## 동기화 기준

- Base Commit: 33fd08d21a13bd993fc9b8eaa9f9fff12cd5d94b
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Territory, Player Runner Trail, 장기 Chunk Territory 전환.

## 목표

거대한 단일 Legacy 폴리곤을 최종적으로 대체할 장기 전환의 첫 마일스톤으로,
프레임·GPU·Fusion에 의존하지 않는 고정소수점 Chunk 좌표와 순서 보존 Trail
세션 도메인을 만든다.

이번 작업은 Runner 경로를 청크 경계에서 형상 변화 없이 분할하고, 같은 청크를
여러 번 방문해도 `SessionId + Sequence + FragmentSequence`로 원래 이동 순서를
복원하며, Abort 뒤의 stale fragment를 거부할 수 있는 순수 계약과 테스트까지만
완료한다. 현재 Legacy Territory, Fusion RPC, Scene 표시와 게임 결과는 변경하지
않는다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/build-chunk-territory/references/work-session-protocol.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/ProjectIO.Territory.ChunkDomain.asmdef`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/FixedTerritoryPoint.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkCoordinate.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailSample.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailFragment.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailSessionStatus.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailSession.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritorySegmentChunkTraversal.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/ProjectIO.Territory.ChunkDomain.Tests.asmdef`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritorySegmentChunkTraversalTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailSessionTests.cs`
- 위 신규 폴더·파일과 일치하는 Unity `.meta` 파일
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- `Docs/Features/Territory.md`
- 이 작업 문서

## 예약 Scene·Prefab·Data Asset

없음. `GameWorld.unity`, `GamePresentation.unity`, `GameRoot.unity`,
`Core.prefab`, `Player Runner.prefab`, `Territory.prefab`, Material,
Compute Shader, ScriptableObject와 Fusion Project 설정은 수정하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- 새 순수 도메인 계약은 2D 고정소수점 좌표, Chunk 좌표, Trail session/sample/
  fragment/status와 결정론적 선분-Chunk traversal로 한정한다.
- 좌표 원점, Chunk 경계 포함 규칙, 음수 좌표, 모서리 동시 통과 tie-break,
  sequence 연속성, Abort stale-data 거부를 문서와 테스트로 고정한다.
- 기존 `TerritorySystem`, `Territory`, `TerritoryVisible`,
  `TerritoryExpansionSession`, RPC, Bootstrapper, 공개 이벤트와 모든 소비자 계약은
  변경하지 않는다.
- 새 asmdef는 `Assembly-CSharp` 또는 Unity/Fusion 런타임 타입에 의존하지 않는
  순수 assembly로 만들고, 테스트 assembly만 이를 참조한다.

## 네트워크·Peer 동등성

- 이번 마일스톤은 Fusion 상태와 RPC에 연결하지 않으며 런타임 플레이 결과를
  변경하지 않는다. 현재 State Authority 기반 Legacy 확장이 계속 유일한
  authoritative 경로다.
- 새 `SessionId`, sample sequence, fragment sequence, status 계약은 후속 네트워크
  마일스톤에서 Input Authority의 예측 Trail과 State Authority 확정 Trail을
  동일 순서로 연결하기 위한 기반이지만, 이번에는 전송이나 표현에 사용하지 않는다.
- Host 로컬과 Client Input Authority의 입력, 선 표시, 성공, 자기 교차 실패,
  Lifeline, Late Join과 정리 동작은 현재 경로 그대로 유지되어야 한다.
- 새 코드가 런타임에 연결되지 않았음을 정적 참조 검색하고, 프로젝트 컴파일과
  순수 EditMode 테스트로 Legacy 경로 무변경을 확인한다. 실제 Peer 동등성 개선은
  후속 runtime integration 마일스톤에서 Host·Client를 각각 검증한다.

## 다른 활성 작업과 겹치는 부분

`CheckStart`와 `Docs/Work/Active/` 확인 결과 README 외 활성 예약이 없어 겹침이
없다.

## 범위 밖

- 기존 Territory authoritative cutover 또는 Legacy 코드 삭제
- `TerritorySystem`과 PlayerRunner runtime 연결
- Fusion RPC, Networked state, AOI, Late Join snapshot 구현
- 예측/확정 Trail Presentation 또는 LineRenderer 교체
- GPU Compute Shader, Chunk mask/SDF, Material과 Render Profile
- Territory Chunk 저장, 영역 fill/commit/revision과 consumer migration
- Scene, Prefab, Bootstrapper, Fog, Grid, Resource, Monster 변경

## 완료 조건

- 같은 입력은 플랫폼과 프레임에 무관한 동일 고정소수점 결과를 만든다.
- 선분이 한 프레임에 여러 Chunk와 정확한 모서리를 통과해도 방문 순서가
  결정론적이며 분할 선분 사이에 틈이나 중복 진행이 없다.
- 음수 좌표와 Chunk 경계 위 좌표의 소유 Chunk가 문서 계약과 일치한다.
- 같은 Chunk 재방문이 별도 fragment로 남고 전체 sequence로 원 경로를 복원한다.
- sample/fragment sequence gap과 Abort 뒤 stale fragment가 명시적으로 거부된다.
- 순수 EditMode 테스트, 프로젝트 컴파일, `.meta` pairing, asmdef parse,
  `git diff --check`가 통과한다.
- Legacy runtime 코드와 Scene·Prefab·네트워크 직렬화 diff가 없다.
- 장기 roadmap과 handoff가 다음 하나의 bounded milestone만 준비하고 구현하지 않는다.

## 실제 변경

- `ProjectIO.Territory.ChunkDomain` 순수 asmdef와 fixed point, Chunk coordinate,
  segment traversal, Trail sample/fragment/session/status를 추가했다.
- half-open Chunk 소유권, 음수 floor, exact-corner 동시 step과 fixed 분할점
  연속성을 구현했다.
- sample/fragment sequence 연속성, 같은 Chunk 재방문 순서, Abort payload 소각과
  stale session 거부를 구현했다.
- Unity EditMode 테스트 assembly와 결정론·경계·재방문·gap·Abort 테스트를
  추가했다.
- `Docs/LongRunning/Territory/`에 계약, milestone, roadmap, handoff와 test matrix를
  작성하고 Territory 기능 문서에 아직 연결되지 않은 신규 기반을 기록했다.
- Legacy runtime, Fusion, Scene, Prefab, Material, Shader와 consumer는 변경하지
  않았다.

## 검증 결과

- Unity 6000.0.69f1 별도 최소 프로젝트에서 신규 runtime/test asmdef를 import하고
  EditMode 테스트 15/15 통과.
- 주 프로젝트 `Assembly-CSharp.csproj` 빌드 0 error 통과. 표시된 warning 16건은
  기존 Fusion/Legacy analyzer warning이다.
- runtime asmdef에 UnityEngine, Fusion, Assembly-CSharp 참조가 없고
  `noEngineReferences: true`임을 확인.
- 신규 Asset 11개와 폴더 2개의 `.meta`가 모두 짝을 이루며 GUID 중복 0건.
- runtime/test asmdef JSON parse 통과.
- Legacy runtime과 Scene·Prefab·네트워크 직렬화 Asset diff 없음.
- `git diff --check` 통과.

## 남은 위험

- 고정소수점 정밀도와 Chunk 크기는 이후 네트워크 압축·GPU mask·소비자 query의
  공용 계약이 되므로 이 마일스톤에서 테스트와 문서로 고정한 뒤 변경 시 별도
  migration이 필요하다.
- 순수 도메인 기반만으로는 현재 보고된 Trail 지연이나 확장 hitch가 개선되지
  않는다. 가시적 개선은 후속 Fusion Trail integration, Chunk state, GPU
  presentation 마일스톤에서 순차적으로 제공한다.
- 주 Unity Editor가 열려 있어 신규 assembly 검증은 같은 Unity 버전의 별도 최소
  프로젝트에서 수행했다. 작업자가 주 Editor에 focus해 import한 뒤 Test Runner로
  동일 15개 테스트를 재실행할 수 있다.
