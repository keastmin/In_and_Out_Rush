# W-20260820-006 Match Progression 상태 전이 Policy Slice

Status: Reserved

## 동기화 기준

- Base Commit: 3328ad9228acf65091ac1ebf7b21f747c6d7a881
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Match Progression, Track and Rounds

## 목표

`TimeSystem.FixedUpdateNetwork`에 들어 있는 현재 라운드 Phase와 경과 시간에 따른 다음 전이 결정 규칙 하나를 Unity·Fusion에 의존하지 않는 순수 `RoundProgressionTransitionPolicy`로 분리한다. 기존 `TimeSystem` 호출자 하나만 새 Policy에 연결하고, Host State Authority가 Networked 라운드 상태를 변경하는 경로와 기존 이벤트 순서·payload를 유지한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/TrackAndRounds.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/migrate-feature-slice/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Features/MatchProgression.meta`
- `Assets/02_Scripts/Features/MatchProgression/Logic.meta`
- `Assets/02_Scripts/Features/MatchProgression/Logic/ProjectIO.MatchProgression.asmdef`
- `Assets/02_Scripts/Features/MatchProgression/Logic/ProjectIO.MatchProgression.asmdef.meta`
- `Assets/02_Scripts/Features/MatchProgression/Logic/RoundProgressionPhase.cs`
- `Assets/02_Scripts/Features/MatchProgression/Logic/RoundProgressionPhase.cs.meta`
- `Assets/02_Scripts/Features/MatchProgression/Logic/RoundProgressionTransition.cs`
- `Assets/02_Scripts/Features/MatchProgression/Logic/RoundProgressionTransition.cs.meta`
- `Assets/02_Scripts/Features/MatchProgression/Logic/RoundProgressionTransitionPolicy.cs`
- `Assets/02_Scripts/Features/MatchProgression/Logic/RoundProgressionTransitionPolicy.cs.meta`
- `Assets/02_Scripts/Features/MatchProgression/Tests.meta`
- `Assets/02_Scripts/Features/MatchProgression/Tests/ProjectIO.MatchProgression.Tests.asmdef`
- `Assets/02_Scripts/Features/MatchProgression/Tests/ProjectIO.MatchProgression.Tests.asmdef.meta`
- `Assets/02_Scripts/Features/MatchProgression/Tests/RoundProgressionTransitionPolicyTests.cs`
- `Assets/02_Scripts/Features/MatchProgression/Tests/RoundProgressionTransitionPolicyTests.cs.meta`
- `Assets/02_Scripts/Time/Network/TimeSystem.cs`
- `Docs/Features/TrackAndRounds.md`
- `Docs/Work/Active/W-20260820-006-match-progression-transition-policy.md`

## 예약 Scene·Prefab·Data Asset

없음. 새 순수 스크립트·asmdef와 대응 `.meta`만 추가하며 Scene, Prefab, ScriptableObject, Network Prefab table은 변경하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- `Dev.Network.RoundPhase`, `TimeSystem`의 Networked property, public property와 이벤트 signature는 변경하지 않는다.
- 새 `RoundProgressionPhase`, `RoundProgressionTransition`, `RoundProgressionTransitionPolicy`는 Match Progression 순수 assembly 내부의 결정 계약이다.
- `TimeSystem`이 기존 `RoundPhase`를 순수 Phase로 변환해 Policy를 호출하고, 반환된 전이에 따라 기존 `StartRound` 또는 `EndRound`를 정확히 한 번 호출한다.
- `StageBootstrapper`와 초기화·이벤트 구독 연결은 변경하지 않는다.

## 다른 활성 작업과 겹치는 부분

- `W-20260820-004-construction-tower-build-usecase.md`는 Construction 코드, `TowerBuildManager`, 관련 기능 문서와 `ProjectIO.slnx`를 예약한다. 이번 작업은 해당 파일과 계약을 변경하지 않으며 `ProjectIO.slnx`도 예약하지 않는다.
- `W-20260820-005-host-client-gameplay-parity-rules.md`는 `AGENTS.md`, 기능 작업 Skill과 작업 템플릿만 예약한다. 이번 작업은 해당 공통 문서를 변경하지 않고, 현재 규칙에 맞춰 Host authoritative 실행과 Client 복제 관찰 경계를 검증한다.
- 코드, Asset, Bootstrapper, Network Spawn 경계의 직접 중복은 없다.

## Legacy와 새 진입점

- Legacy: `TimeSystem.FixedUpdateNetwork`가 Phase와 `PhaseElapsedTime`을 직접 비교해 `StartRound` 또는 `EndRound`를 선택한다.
- 새 경로: `TimeSystem.FixedUpdateNetwork`가 시간 누적과 State Authority 검사를 계속 소유하고, 전이 선택만 `RoundProgressionTransitionPolicy.Evaluate` 한 호출에 위임한다.
- 첫 Slice의 소비자는 `TimeSystem` 하나이며 다른 호출자나 병렬 실행 경로를 추가하지 않는다.

## 네트워크·이벤트 경계

- `Object.HasStateAuthority`와 `_isRunning` 검사는 기존 위치에서 Policy 호출 전에 유지한다.
- `ElapsedTime`, `RoundNumber`, `Phase`, `PhaseElapsedTime`, `IsBerserk`는 기존처럼 `TimeSystem`만 변경하며 새 순수 Policy는 Networked 상태를 소유하거나 변경하지 않는다.
- Maintenance 종료 시 `OnRoundStarting`, Combat 종료 시 `OnRoundEnded`, Berserk 조건 충족 시 `OnBerserkStarted`, 다음 Maintenance 진입 시 `OnMaintenanceStarting`의 기존 순서와 sender·context를 유지한다.
- Client는 기존 Networked property 복제와 이벤트 연결을 그대로 사용하며 새 authoritative 실행 경로를 만들지 않는다.
- Host 로컬 실행에서도 Fixed tick당 전이 결정과 기존 이벤트가 중복 실행되지 않는다.

## 롤백

- `TimeSystem.FixedUpdateNetwork`의 직접 Phase·시간 비교를 복원하면 소비자 전환만 즉시 롤백할 수 있다.
- 새 Match Progression 순수 assembly와 테스트는 소비자가 `TimeSystem` 하나뿐이므로 함께 제거할 수 있다.
- Network schema, RPC, 직렬화 Asset을 변경하지 않아 Scene·Prefab 또는 세션 데이터 마이그레이션은 필요하지 않다.

## 범위 밖

- `StageBootstrapper.cs`, `StageBootstrapper.KIM.cs`, `StageBootstrapper.YOU.cs`
- `TrackSystem`, Track 확장 규칙과 Grid·Obstacle·Tower 소비자
- `TrackMonsterSpawnSystem`, Monster Wave Spawn, wave table
- Stage UI, Timer UI와 다른 UI
- Scene, Prefab, ScriptableObject, ProjectSettings, Package
- 라운드 duration·default round count 값, Berserk 조건, 이벤트 signature·구독 구조 변경
- 다른 `TimeSystem` 소비자 연결 또는 Match Progression 전체 교체

## 완료 조건

- 순수 Policy가 Maintenance·Combat 상태와 Phase 경과 시간·해당 duration으로 `None`, `StartRound`, `EndRound` 중 하나만 결정한다.
- 경계 직전에는 전이가 없고 duration과 같은 tick부터 기존과 동일한 전이를 반환한다.
- `TimeSystem`만 Policy를 호출하며 기존 State Authority guard 이후 정확히 한 번 평가한다.
- 시간 누적, Networked 상태 변경, Round 증가, Berserk 판정과 이벤트 발생은 기존 `TimeSystem` 경로에 남는다.
- Host의 Maintenance→Combat과 Combat→Maintenance 상태·이벤트 순서가 기존과 동일하다.
- Client가 authoritative 전이를 실행하지 않고 복제 상태를 계속 관찰한다.
- `StageBootstrapper`, Track 확장, Monster Wave Spawn, UI, Scene·Prefab diff가 없다.
- 순수 Policy 집중 테스트, Unity assembly 컴파일, 관련 프로젝트 컴파일과 `git diff --check`를 통과한다.

## 실제 변경

예약 단계.

## 검증 결과

예약 단계.

## 남은 위험

- 순수 Phase와 기존 `Dev.Network.RoundPhase` 사이 변환이 추가되므로 양방향 상태 매핑 누락을 집중 테스트와 코드 검토로 확인한다.
- 실제 Host·Client에서 라운드 경계 tick, 이벤트 순서와 Client timer 복제를 수동 확인한다.
