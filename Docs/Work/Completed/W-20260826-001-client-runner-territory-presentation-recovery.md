# W-20260826-001 Client Runner Territory presentation recovery

Status: Complete

## 동기화 기준

- Base Commit: 0554fc2a30ef9af3c20175594a5866e222ea9294
- 공용 Upstream: `origin/rebuild-development-environment`
- CheckStart: `READY_TO_CHECK_CONFLICTS` (2026-08-26)

## 담당자

Codex `/root`

## 기능

Territory Legacy polygon 완료 결과의 Fusion 복제 복구와 Client Input Authority Runner presentation 동등성

## 목표

- State Authority가 계속 유일하게 Legacy polygon, triangle, consumer event를 결정한다.
- Client가 하나의 완료 결과 RPC를 놓치거나 Begin/Data/Complete가 중복·재실행되어도 최신의 검증된 full polygon snapshot으로 수렴하고 revision 고착 없이 다음 Trail과 확장을 표시한다.
- `expansionCalculationPending`과 owner Trail prediction을 정상 완료, 거부, 중단, malformed transfer와 recovery의 모든 종료 경로에서 해제한다.
- 전역 Territory NetworkObject가 AOI 반경 밖의 Client에게도 지속 상태와 복구 RPC를 전달할 수 있게 GameWorld interest 설정을 검증·수정한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/Features/PlayerRunner.md`
- `Docs/Work/Completed/W-20260823-002-background-authoritative-territory-expansion.md`
- `Docs/Work/Completed/W-20260822-002-chunk-territory-replication-recovery.md` (shadow contract와의 분리 확인)
- `manage-feature-work`
- `build-chunk-territory`
- `photon-fusion-feature`

## 예상 수정 코드

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
  - authoritative advertised/persistent presentation revision과 pending lifecycle을 State Authority만 갱신한다.
  - Client mismatch 감지, bounded outstanding recovery request, State Authority의 targeted 최신 full snapshot 재전송, Host-local 중복 적용 배제를 추가한다.
  - Begin/Data/Complete 수신 실패가 owner prediction을 영구 차단하지 않게 terminal/recovery cleanup을 연결한다.
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryExpansionResultReplica.cs`
  - full snapshot의 최신 revision fast-forward, 이미 적용한 revision replay no-op, 현재 inbound transfer를 보존하는 idempotent Begin/Data/Complete와 원자적 malformed/gap 거부 계약을 구현한다.
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryExpansionReplication.cs`
  - 정상 bounded outbound, snapshot resend/recovery와 per-peer outstanding state를 receiver contract에 맞춰 확장하고 reset/teardown cleanup을 제공한다.
- `Assets/02_Scripts/Territory Refactor/Tests/Editor/TerritoryBackgroundExpansionWorkerTests.cs`
  - 1→2 정상 적용, gap 뒤 최신 full result recovery, duplicate/replay 무해성, out-of-order·손상 payload 원자 거부, 실패 뒤 정상 result, pending/owner prediction 종료 경로를 집중 검증한다.
- `Docs/Features/Territory.md`
  - Legacy authoritative presentation의 persistent revision·recovery·AOI 계약과 실제 검증 결과를 갱신한다.

## 예약 Scene·Prefab·Data Asset

- `Assets/01_Scenes/GameWorld.unity`
  - Scene의 `Territory` NetworkObject만 예약한다. 현 값 `ObjectInterest: 1`은 Fusion editor source에서 AreaOfInterest로 확인되며, Territory는 원점에 있고 플레이어 AOI 반경은 128, stage world boundary는 1000이다.
  - Territory를 global/always-interested로 설정할 필요가 구현 중 확정되면 이 오브젝트의 interest 모드만 변경한다. 하나의 scene object에 대한 소량 지속-state/결과 RPC bandwidth와 전역 Territory query·mesh 가시성 일관성이 근거이며, 다른 NetworkObject·AOI grid·반경은 변경하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- Chunk shadow는 presentation 또는 consumer 기준으로 전환하지 않는다. C013 Legacy polygon 결과가 계속 gameplay와 consumer의 유일한 authoritative 원본이다.
- expansion packet은 `sourceRevision` 기반의 polygon patch가 아니라 `(revision, full vertices float bits, full triangle indices)` snapshot이다. receiver는 현재 revision보다 뒤처진 경우 검증된 더 최신 full result를 원자 적용할 수 있다.
- 이미 적용한 revision의 동일 결과는 no-op이며, 현재 inbound transfer와 같은 identity의 repeated Begin/Data/Complete는 progress를 손상하지 않는다. identity가 다른 stale replay는 현재 transfer를 reset하지 않는다.
- sequence gap, conflicting future transfer, 길이·finite vertex·triangle index가 잘못된 payload는 공개 상태를 바꾸지 않고 명확히 거부한다. mismatch가 advertised authoritative revision과 지속되면 bounded recovery request로 최신 full snapshot을 targeted Reliable 전송한다.
- authoritative revision과 pending signal은 RPC 단독 지속 상태가 아니라 Networked 상태로 관찰 가능해야 한다. recovery request/response는 bounded Reliable transport이며 State Authority가 요청 peer를 검증한다.
- recovery/timeout/abort/apply failure는 inbound temporary state와 Client pending을 해제한다. State Authority만 Territory·mesh·consumer event를 authoritative하게 적용하고 Host-local은 Proxy 수신 경로로 다시 적용하거나 consumer event를 두 번 발생시키지 않는다.
- Fusion package 2.0.6 Stable build 1034는 변경하지 않는다. resimulation 중 RPC 재전달 관련 위험은 receiver idempotency와 persistent recovery로 완화하며, package upgrade 필요성은 별도 작업의 결정 근거로만 기록한다.

## 네트워크·Peer 동등성

- Input Authority: Host-local Runner와 Client Input Authority Runner가 `PlayerRunner.OnPositionChanged`로 local Trail prediction을 표시한다. prediction은 pending 동안 숨길 수 있으나 terminal, reject, abort, timeout 또는 recovery 시작/완료 후 영구 차단되지 않는다.
- State Authority: Host만 confirmed Trail, background Legacy polygon calculation, triangle, authoritative revision/pending 및 consumer event를 한 번 결정한다. Client는 Territory gameplay state를 직접 변경하지 않는다.
- 반환 경로: Networked advertised revision/pending이 지속 수렴 신호이고, Begin/Data/Complete full snapshot과 mismatch-triggered targeted resend가 Client presentation 복구 경로다. 거부·손상은 로그와 pending 해제로 Client가 다음 Trail을 시작할 수 있게 한다.
- Host-local은 authoritative 완료를 한 번만 적용하고 Client는 같은 float vertex bit와 triangle index를 terminal에서 한 번 적용한다. Client RPC replay/재시뮬레이션이 Host mutation·consumer event를 만들지 않는다.
- AOI: `GameWorld` Territory가 global/always-interest가 되면 AOI 밖 이동 뒤에도 이 지속 상태와 recovery request/response가 가능한지 확인한다. 두-player fixed session이라 Late Join/reconnect 확장은 만들지 않지만 Spawn/scene readiness, AOI leave/re-enter, teardown은 reset한다.
- 실제 runtime 증거가 없으면 Host와 Client 결과를 분리해 미검증으로 기록하고, Host 실행 → Client 접속 → Client Runner 첫/연속 확장 → 의도적 result gap/replay → AOI 밖 이동/복귀 → vertex bit·triangle 비교 → Host consumer event 단일 발생을 수동 절차로 남긴다.

## 다른 활성 작업과 겹치는 부분

`Docs/Work/Active/README.md` 외 Active 작업 파일이 없다. CheckStart가 보고한 Active 충돌도 없다.

## 범위 밖

- Fusion package 업그레이드, ProjectSettings, AOI grid 반경/셀 크기 또는 다른 NetworkObject interest 변경
- Chunk shadow를 Legacy polygon presentation, gameplay authority 또는 consumer로 cutover
- Territory polygon 확장 알고리즘, Trail sampling/자기 교차/Lifeline 규칙, Resource/Grid/Fog/Monster consumer 구현 변경
- Scene 외 GamePresentation, GameRoot, Core prefab, Player Runner prefab, asmdef, Material, Shader, Compute Shader 변경
- Late Join, reconnect, 다수 peer 보안/호환성 체계의 일반 지원

## 완료 조건

- 정상 1→2 연속 full result가 동일 vertex float bit와 triangle index로 적용된다.
- Client가 revision 하나를 놓친 뒤 검증된 최신 full result로 수렴하고 다음 확장도 표시한다.
- duplicate/replayed Begin/Data/Complete가 적용 revision 또는 유효 inbound transfer를 손상하지 않는다.
- out-of-order sequence와 malformed payload는 부분 mesh·revision을 공개하지 않고 거부하며, 이후 정상 result는 적용된다.
- pending과 owner Trail prediction은 완료·거부·중단·timeout·recovery에서 해제된다.
- Host-local과 Client Input Authority Runner 각각 첫 확장과 연속 확장에서 Trail, terminal mesh, 이후 Trail을 표시한다.
- Host/Client 최종 polygon vertex bit와 triangle index가 일치하고 authoritative consumer event는 Host에서 한 번만 실행된다.
- Territory AOI leave/re-enter 또는 global interest 변경의 Host/Client 영향과 bandwidth 근거를 기록한다.
- 집중 EditMode 테스트, Unity/Fusion Weaver 포함 프로젝트 compile, `git diff --check`, `git status`를 수행한다. 실제 Host·Client 실행을 못 하면 정확한 수동 절차와 미검증 항목을 기록한다.

## 롤백

- 이 작업의 구현 commit을 되돌리면 C013 background Legacy polygon과 기존 결과 packet 경로로 복귀한다.
- GameWorld의 Territory interest 변경은 해당 NetworkObject의 직전 serialized `ObjectInterest` 값으로만 복구한다. Chunk shadow, 다른 AOI 설정, Fusion package와 consumer 계약은 롤백 대상이 아니다.

## 실제 변경

- `TerritoryExpansionResultReplica`가 현재 client revision보다 최신인 검증 full snapshot을
  fast-forward로 원자 적용하고, applied replay no-op, duplicate Begin/Data idempotency,
  malformed/out-of-order의 inbound 보존을 제공한다.
- `TerritoryExpansionReplication`에 State Authority의 targeted recovery transfer queue를
  추가했다. normal과 recovery는 같은 tick당 최대 2 data packet 예산을 공유한다.
- `TerritorySystem`에 Networked advertised revision/pending, Client의 2초 bounded retry,
  State Authority의 requester-validated targeted latest full snapshot resend와 pending
  cleanup을 추가했다. Host consumer event 경로는 변경하지 않았다.
- `GameWorld.unity`의 Scene Territory NetworkObject만 `ObjectInterest: 1`(AOI)에서
  `ObjectInterest: 0`(global)으로 변경했다.
- `TerritoryBackgroundExpansionWorkerTests`에 fast-forward, replay, duplicate Begin/Data,
  out-of-order/malformed 뒤 정상 완료 검증을 추가했다.
- `Docs/Features/Territory.md`에 persistent presentation recovery와 global interest 계약을
  기록했다.

## 검증 결과

사전 진단:

- `Assets/Photon/Fusion/build_info.txt`: Fusion 2.0.6 Stable build 1034.
- `Player.log`와 `Player-prev.log`: Client에서 `Territory expansion receive begin failed: Expansion result revision is stale or out of order.`가 반복된다.
- 현 `TerritoryExpansionResultReplica.TryBegin`은 `sourceRevision == CurrentRevision` 및 연속 `revision`만 허용하고, `TryAppend`/`TryComplete` 오류는 inbound를 reset한다.
- 현 `TerritorySystem`은 full presentation을 Reliable Begin/Data/Complete RPC로만 보내며, persistent advertised presentation revision이나 expansion full-snapshot recovery가 없다.
- `GameWorld` Territory NetworkObject는 `ObjectInterest: 1` (AreaOfInterest), 객체 위치는 원점이다. Stage AOI 반경 128과 world boundary 1000의 차이로 전역 state 누락 가능성이 있다.

구현·집중 테스트·compile·Host/Client runtime은 예약 진행 뒤 수행한다.

구현 검증:

- `dotnet build ProjectIO.slnx`: 오류 0개, 기존 경고 25개.
- Unity 6000.0.69f1 batch import/compile: 정상 종료(return code 0), 변경한 script와
  `GameWorld.unity`를 import했다.
- Unity batch `-runTests`는 종료 0이지만 test result XML을 만들지 않아 Test Runner 실행
  증거로 인정하지 않는다. 추가한 EditMode 집중 테스트와 실제 Host/Client runtime은 아래
  수동 절차로 미검증이다.
- `git diff --check`: 통과.

## 남은 위험

- Fusion 2.0.6의 RPC resimulation 동작은 package 변경 없이 runtime에서 재현·검증해야 한다.
- full polygon snapshot의 worst-case payload/전송 시간과 global interest의 실제 bandwidth는 Unity/Fusion Profiler에서 측정해야 한다.
- 최신 full snapshot fast-forward와 Networked advertised revision의 정확한 Fusion Weaver serialization/OnChangedRender 사용법이 현재 package API와 맞는지는 구현 전 package source와 기존 Networked pattern을 다시 대조한다.
- 수동 Host/Client 절차: Host로 `GameWorld`를 시작하고 Client를 접속한다. Client Input
  Authority Runner로 첫 확장과 연속 확장을 수행해 Trail/terminal mesh/다음 Trail을
  확인한다. packet 하나를 의도적으로 누락·replay해 advertised revision recovery를
  확인하고 AOI 반경 128 밖으로 이동·복귀한다. Host와 Client의 final vertex float bit 및
  triangle index를 비교하고 Host의 consumer event가 한 번인지 확인한다. Unity Test Runner
  창에서 `TerritoryBackgroundExpansionWorkerTests`를 실행해 result XML을 남긴다.
