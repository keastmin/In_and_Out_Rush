# W-20260823-002 단순 백그라운드 권위 영역 확장 교체

Status: Complete

## 동기화 기준

- Reservation Commit: 1e4469621ed4ba7f28c6876b503640b4ac4eda55
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex `/root`

## 기능

Territory 영역 확장 계산의 전면 교체, 백그라운드 실행과 두 Peer 완료 결과 동기화

## 목표

- 이전 C008-C010 compact 확장 계산 경로를 실제 gameplay 확장에서 제거한다.
- State Authority가 재진입 시 현재 Territory와 확정 Trail snapshot을 하나의 background
  CPU 작업에 넘긴다.
- background 작업에서 확장 polygon 생성, 검증, triangulation과 bounded network packet
  준비까지 끝낸다.
- 계산 중 simulation, 입력, 이동, Render와 Fusion tick을 계속 실행하고 마지막 완료
  Territory를 판정·표시 상태로 유지한다.
- 완료 결과만 main thread에서 한 번 Mesh와 Territory에 적용하고 같은 vertex/triangle을
  Proxy에 전송한다.
- 입력 Trail point를 vertex budget이나 prototype tolerance로 단순화하지 않는다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/Features/PlayerRunner.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- `manage-feature-work`
- `replace-existing-feature`
- `build-chunk-territory`
- `photon-fusion-feature`

## 예상 수정 코드

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory/Territory.cs`
- `Assets/02_Scripts/Territory/TerritoryVisible.cs`
- 신규 `Assets/02_Scripts/Territory Refactor/Domain/TerritoryBackgroundExpansionWorkItem.cs`
- 신규 `Assets/02_Scripts/Territory Refactor/Domain/TerritoryBackgroundExpansionResult.cs`
- 신규 `Assets/02_Scripts/Territory Refactor/Domain/TerritoryBackgroundExpansionWorker.cs`
- 신규 `Assets/02_Scripts/Territory Refactor/Domain/TerritoryExpansionPresentationData.cs`
- 신규 `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryExpansionResultPacket.cs`
- 신규 `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryExpansionResultPacketizer.cs`
- 신규 `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryExpansionResultReplica.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryExpansionReplication.cs`
- 신규 worker/calculator/packet 집중 테스트와 대응 `.meta`
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- 이 작업 문서

## 예약 Scene·Prefab·Data Asset

없음. Inspector 연결을 추가하지 않고 기존 TerritorySystem 연결 안에서 교체한다.

## 공용 계약 또는 Bootstrapper 변경

- active 확장 계산 원본은 compact snapshot이 아니라 완료된 Legacy Territory vertex
  snapshot과 authoritative confirmed Trail이다.
- work item은 source revision, source polygon과 Trail의 독립 snapshot이다.
- worker는 한 번에 한 revision만 처리한다. polygon 결과, triangle index와 packet이 모두
  완성되기 전에는 결과를 공개하지 않는다.
- 계산 중에는 새 확장 Trail을 시작하지 않지만 Runner 이동과 나머지 gameplay는 막지 않는다.
  완료 적용 뒤 현재 위치부터 다음 확장을 시작할 수 있다.
- Unity Mesh API, event와 Fusion RPC는 main thread에서만 실행한다.

## 네트워크·Peer 동등성

- Input Authority는 기존처럼 로컬 Trail을 즉시 표시하고 State Authority가 confirmed Trail과
  확장 결과를 결정한다.
- Host-local Runner와 Client Runner 모두 같은 State Authority background schedule 경로를
  사용하며 Host가 중복 계산·적용하지 않는다.
- 완료 vertex는 float bit를 그대로 word로 저장하고 triangle index와 함께 packet당 고정
  상한, tick당 고정 packet 상한으로 Proxy에 전송한다.
- Proxy는 begin/data/terminal 순서, revision, 크기, finite vertex와 triangle index를
  검증하고 terminal에서만 동일 결과를 원자 적용한다. Proxy 재삼각분할은 없다.
- 현재 2인 단일 스테이지 범위만 다루며 Late Join, reconnect, AOI와 보안 확장은 추가하지
  않는다.

## 교체와 롤백

- `TerritorySystem`에서 compact expansion begin/drain/schedule/poll/publish 호출을 모두
  제거하고 새 background worker만 active mutation을 수행한다.
- C006-C012 chunk/trail 진단·기록 코드는 삭제하지 않지만 영역 확장 결과를 만들거나 적용하지
  않는다.
- 동기 `Territory.TryExpand` 호출과 새 worker를 동시에 실행하지 않는다.
- 롤백은 이 작업 commit을 되돌려 기존 예약 기준 코드로 복귀하는 것이며 Scene·Prefab 복구는
  필요 없다.

## 다른 활성 작업과 겹치는 부분

기존 W-20260823-002를 같은 담당자가 전면 갱신한다. 다른 Active 예약은 없다.

## 범위 밖

- GPU, Burst, Job System과 CPU/GPU fallback
- 영역 query와 모든 consumer의 chunk-native 전환
- Mesh upload 자체의 비동기화(Unity main-thread 제약)
- Trail 기록·페이지 renderer와 confirmed Trail network 계약 변경
- Scene, Prefab, Inspector, ScriptableObject, asmdef, Package와 ProjectSettings
- Late Join, reconnect, AOI, 다수 Peer와 보안 확장

## 완료 조건

- 재진입 frame에는 snapshot 준비와 background schedule만 하고 polygon 검증·삼각분할을
  동기 실행하지 않는다.
- 계산 중 Runner 이동, rendering과 Fusion simulation이 계속되며 이전 Territory가 유지된다.
- worker가 source/path snapshot만 읽고 확장 polygon, triangle과 bounded packet을 준비한다.
- 256 vertex prototype 제한과 tolerance 기반 polygon 단순화를 사용하지 않는다.
- 성공 결과는 main thread에서 revision당 한 번만 적용되고 Host/Client는 같은 float
  vertex bit와 triangle index를 사용한다.
- 실패는 이전 Territory를 보존하고 worker를 영구 fault시키지 않아 다음 정상 확장을 다시
  예약할 수 있다.
- active runtime에서 compact expansion 오류·schedule 경고가 더 이상 발생하지 않는다.
- 집중 테스트, Assembly-CSharp compile, Host-local/Client Runner 양방향 확장과 계산 중
  이동 체감, Profiler를 검증한다.
- `git diff --check`와 `git status`로 예약 밖 변경이 없음을 확인한다.

## 실제 변경

- 이전 W-002 compact 연결 미커밋 구현은 작업자의 명시적 승인으로 모두 제거했다.
- `TerritorySystem` active 확장을 단일 State Authority background worker로 교체했다.
- worker가 source/path snapshot으로 polygon 확장, 검증, triangulation과 packetization을
  완료하고 main thread가 revision 확인 뒤 Mesh/Territory/event를 한 번 적용한다.
- 기존 prototype 256 vertex budget과 tolerance 기반 RDP 단순화 경로를 제거했다.
- 완료 vertex float bit와 triangle index를 48-word packet, tick당 최대 2 data packet으로
  Proxy에 보내고 terminal에서만 동일 결과를 적용한다.
- compact C008-C011 runtime 호출과 동기 확장/vertex RPC는 active 확장 경로에서 제거했다.
- Scene, Prefab과 Inspector 변경은 없다.

## 검증 결과

- `VerifyReservation`이 reservation commit
  `1e4469621ed4ba7f28c6876b503640b4ac4eda55`에서 통과했고 다른 Active 예약은 없다.
- Unity가 재생성한 `Assembly-CSharp`와 `Assembly-CSharp-Editor` build는 모두 오류 0개다.
  기존 runtime warning 13개와 Editor warning 8개가 남는다.
- 300개 이상 유효 Trail 꺾임의 background 계산, 256 초과 결과 vertex, 최대 48-word
  packet과 float/triangle exact round-trip 테스트가 통과했다.
- invalid 작업 뒤 같은 worker에서 다음 정상 확장을 처리하는 복구 테스트가 통과했다.
- tick당 최대 2 data packet outbound budget 테스트를 포함한 신규 테스트 3/3이 통과했다.
- `git diff --check`는 통과했다.
- 작업자가 Host-local/Client Runner 양방향 정상 확장, 계산 중 플레이 연속성, 양쪽 Peer
  결과 모양, 연속 확장과 compact 오류 미발생을 수동 확인했다.
- 정량 Profiler 수치는 측정하지 않았으며 후속 성능 회귀에서 필요할 때 확인한다.

## 남은 위험

- Unity Mesh upload와 consumer event는 main thread 제약이므로 최종 적용 순간의 실제 비용은
  runtime Profiler로 확인해야 한다.
- polygon 검증과 ear clipping 총 계산 시간은 vertex 수에 따라 증가하지만 background에서
  실행되어 게임 frame을 막지 않는다.
- 계산 중 새 영역 확장을 추가로 시작하지 않으므로 매우 긴 계산 중의 추가 획득 경로는
  기록하지 않는다. Runner 이동과 다른 gameplay는 계속된다.
