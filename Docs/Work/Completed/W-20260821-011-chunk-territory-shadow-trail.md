# W-20260821-011 Chunk Territory Shadow Trail 통합

Status: Completed

## 동기화 기준

- Base Commit: 3ee8d2ab905fc26ac961958fee17d3c26272c9c3
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Territory, Player Runner Trail, 장기 Chunk Territory 전환.

## 목표

State Authority의 현재 Legacy 확장 경로와 함께 첫 마일스톤의
`TerritoryTrailSession`을 shadow로 기록한다. 성공 재진입에서는 Legacy 경로와
fixed sample 순서가 일치하는지 비교하고, 자기 교차·Lifeline·외부 강제 이동
중단/재개에서는 Commit/Abort와 stale session 정리가 계약대로 이루어지는지
진단한다.

Shadow 경로는 관찰과 검증만 담당한다. Territory polygon, 확장 판정, RPC,
LineRenderer와 모든 consumer 결과는 계속 Legacy 경로만 사용한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/Features/PlayerRunner.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/build-chunk-territory/references/work-session-protocol.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Domain/TerritoryExpansionSession.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailShadowRecorder.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailShadowRecorder.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailShadowComparison.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailShadowComparison.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailShadowComparer.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailShadowComparer.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailShadowComparerTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailShadowComparerTests.cs.meta`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- `Docs/Features/Territory.md`
- 이 작업 문서

## 예약 Scene·Prefab·Data Asset

없음. `GameWorld.unity`, `GamePresentation.unity`, `GameRoot.unity`, `Core.prefab`,
`Player Runner.prefab`, `Territory.prefab`, Material, Shader, Compute Shader,
ScriptableObject와 Fusion 설정은 수정하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- C001-C004 fixed/Chunk/traversal/session 계약은 변경하지 않는다.
- Legacy `TerritoryExpansionSession`에는 현재 player path를 복사해 비교할 수 있는
  읽기 전용 seam만 추가한다. 기존 path 저장, 계산용 단순화, 교차 판정과 확장
  결과는 변경하지 않는다.
- 새 comparer는 Legacy 경로를 fixed로 양자화한 순서와 shadow sample 순서를
  비교하는 순수 계약으로 한정한다.
- Bootstrapper와 공개 Territory event는 변경하지 않는다.

## 네트워크·Peer 동등성

- 입력 원점은 기존 Player Runner 이동과 `OnPositionChanged`이며 새 입력이나
  Client RPC를 추가하지 않는다.
- 기존처럼 `TerritorySystem`의 State Authority만 shadow session을 시작하고
  sample을 기록하며 Commit/Abort와 비교 결과를 결정한다. Proxy는 shadow 상태를
  만들거나 진단을 중복 실행하지 않는다.
- Shadow 결과는 게임 상태가 아닌 Host 진단 정보다. Networked state, RPC,
  Spawn, AOI와 플레이어 결과 응답을 추가하지 않으며 지속 상태로 취급하지 않는다.
- Host 로컬 Runner와 Client Input Authority Runner 모두 기존 입력, 선 표시,
  성공 확장, 자기 교차·Lifeline 피드백을 그대로 사용한다. Shadow mismatch가
  플레이를 거부하거나 결과를 바꾸지 않는다.
- Host가 서버와 로컬 Client 역할을 함께 수행해도 State Authority 경로에서 한 번만
  기록한다. 기존 path RPC가 Proxy에서 shadow recorder를 실행하지 않게 한다.
- Late Join은 현재 Legacy Territory 복원만 사용한다. 진행 중 shadow 진단 session은
  복원하지 않으며 disconnect, Stop, Lifeline과 자기 교차 시 로컬 payload를
  Abort/소각한다.
- 자동 테스트는 comparer의 일치, 첫 mismatch, count mismatch와 Abort stale
  session을 검증한다. 런타임은 Host 로컬과 Client Runner 각각 정상 재진입,
  자기 교차/Lifeline, 강제 이동 pause/resume에서 Legacy 결과 무변경과 Host
  shadow 진단을 확인한다. 실제 다중 Peer 실행을 못 하면 두 절차를 미검증으로
  명시한다.

## 다른 활성 작업과 겹치는 부분

`CheckStart`와 `Docs/Work/Active/` 확인 결과 README 외 활성 예약이 없어 겹침이
없다.

## 범위 밖

- owner-predicted/confirmed Trail 표시와 fragment RPC
- Networked Trail 상태, AOI, Late Join Trail snapshot
- Chunk Territory 저장, fill, revision과 확장 commit
- GPU mask/SDF와 CPU fallback
- Territory consumer migration, authoritative cutover와 Legacy 삭제
- Scene, Prefab, Bootstrapper와 직렬화 참조 변경
- Legacy path sampling 간격, 계산용 단순화, 교차 판정과 영역 모양 변경

## 완료 조건

- State Authority의 Legacy path point마다 같은 순서의 fixed shadow sample이
  기록되고 성공 재진입 시 Commit과 비교 결과가 생성된다.
- 비교는 양자화된 좌표, point 수와 첫 mismatch를 결정론적으로 보고한다.
- 같은 Chunk 재방문과 다중 Chunk 이동은 fragment 순서를 유지한다.
- 자기 교차와 Lifeline reset은 active shadow payload를 Abort하고 다음 session에서
  stale data가 섞이지 않는다.
- 외부 강제 이동 pause 중에는 기존과 마찬가지로 sample을 추가하지 않고, resume
  강제점 이후 비교가 일치한다.
- Shadow mismatch와 내부 실패는 진단만 남기고 Territory, RPC, 표시, Kill,
  Lifeline과 consumer 결과를 변경하지 않는다.
- 순수 EditMode 테스트, 프로젝트 컴파일, Host·Client 수동 절차 또는 정확한
  미검증 기록, `.meta` pairing과 `git diff --check`가 완료된다.
- Scene·Prefab·Networked/RPC serialization diff와 Legacy/Shadow 중복 부작용이 없다.

## 실제 변경

- fixed Legacy point와 shadow sample의 point 수·첫 mismatch를 비교하는 순수
  `TerritoryTrailShadowComparer`와 result를 추가했다.
- Editor/Development Build의 State Authority 전용 `TerritoryTrailShadowRecorder`가
  단조 SessionId, sample sequence, Commit/Abort와 마지막 진단 결과를 관리하도록
  구현했다. 비개발 빌드에서는 shadow session을 시작하지 않는다.
- `TerritoryExpansionSession`에 현재 Legacy player path의 읽기 전용 Vector2 copy
  seam을 추가했다. path 저장·단순화·교차·확장 로직은 변경하지 않았다.
- `TerritorySystem`이 Legacy point 추가 뒤 같은 점을 shadow에 기록하고, 정상
  재진입의 확장 직전에 Commit/compare한다. Stop, 자기 교차, Lifeline reset과
  teardown은 active shadow를 Abort한다.
- 기존 RPC가 실행되는 Proxy에서는 `HasStateAuthority` 검사로 shadow를 시작하거나
  중복 기록하지 않는다.
- Scene·Prefab·Networked state·RPC·Spawn·LineRenderer와 consumer를 변경하지
  않았다.

## 검증 결과

- Unity 6000.0.69f1 격리 프로젝트 EditMode 21/21 통과. 기존 15개와 신규
  comparer 3개, recorder Commit/Abort/실패 격리 3개를 포함한다.
- 실제 수정된 `TerritoryTrailShadowRecorder`, `TerritoryExpansionSession`,
  `TerritorySystem`을 프로젝트 assembly와 함께 통합 컴파일해 0 error를 확인했다.
- 신규 comparer 좌표 mismatch와 count mismatch가 각각 첫 다른 index와 shared
  prefix 길이를 보고함을 확인했다.
- recorder 정상 Commit이 fragment를 남기고, Abort 후 payload/비교 결과를
  소각하며 다음 session이 더 큰 SessionId를 사용함을 확인했다.
- 작업자가 주 Unity Editor compile과 안내된 Host 로컬/Client Input Authority
  정상 재진입, 자기 교차·Lifeline·SandTomb pause/resume 절차 완료를 보고했다.
- 신규 `.meta` pairing과 GUID, 예약 범위, 신규 RPC/Networked/Spawn diff와
  `git diff --check` 통과. Scene·Prefab·직렬화 Asset diff 없음.

## 남은 위험

- 이 마일스톤은 State Authority의 shadow 관찰만 추가하므로 현재 선 동기화 지연,
  Territory 확장 hitch와 모양 보정은 아직 개선하지 않는다.
- Legacy float가 fixed로 양자화되면서 축당 최대 1/512 world-unit 차이가 발생한다.
  이는 C001 계약상 정상이며 그보다 큰 순서·좌표 차이만 mismatch로 취급한다.
- 실제 Client Runner 위치 이벤트가 Host State Authority에 도달하는 타이밍은
  런타임 Host·Client 테스트에서 별도로 확인해야 한다.
- 현재 성공 비교는 Legacy path와 같은 입력점을 shadow에 병행 기록하는 일치
  검증이다. 네트워크 transport, 화면 예측과 Chunk Territory commit의 정확성을
  증명하지 않는다.
