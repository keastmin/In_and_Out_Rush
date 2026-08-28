# W-20260828-004 Runner shotgun

Status: Completed

## 동기화 기준

- Base Commit: 5825b4f82b6d975c545fe16a709f3d92148c736b
- 공용 Upstream: `origin/rebuild-development-environment`
- CheckStart: `READY_TO_CHECK_CONFLICTS` (`AHEAD=0`, `BEHIND=0`, 2026-08-28)

## 담당자

Codex `/root`

## 기능

Player Runner 기본 네트워크 산탄총과 WorldMonster 근접 넉백

## 목표

- Player Runner prefab의 기본 무기를 산탄총으로 전환하되 기존 돌격소총과 쌍권총 코드는 보존한다.
- 한 발을 State Authority의 단일 24도 원뿔 판정으로 처리해 최대 7m 안의 복수 WorldMonster에 거리별 피해를 적용한다.
- 달리기 중 대상별 독립 50% MISS, 결정적 8펠릿 연출 데이터, 소유자 대상별 결과 이벤트를 Host와 Client에 동등하게 제공한다.
- 5발 탄창에 1초마다 1발을 채우는 사격 중단 가능 적립 장전과 3m 이내 이동형 WorldMonster의 1m 넉백을 구현한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/PlayerRunner.md`
- `Docs/Features/MonstersAndProjectiles.md`
- `manage-feature-work`
- `photon-fusion-feature`

## 예상 수정 코드

- `Assets/02_Scripts/Features/RunnerWeapons/Logic/RunnerWeaponRules.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Logic/ShotgunRules.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Logic/ShotgunRules.cs.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion/RunnerWeaponNetworkBehaviour.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion/ShotgunWeapon.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion/ShotgunWeapon.cs.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion/ShotgunShotPresentation.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion/ShotgunShotPresentation.cs.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion/ShotgunTargetResultPresentation.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion/ShotgunTargetResultPresentation.cs.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Tests/RunnerWeaponRulesTests.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Tests/ShotgunRulesTests.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Tests/ShotgunRulesTests.cs.meta`
- `Assets/02_Scripts/Player/Player Runner/RunnerProjectileWeapon.cs`
- `Assets/02_Scripts/Monster/Monster.cs`
- `Assets/02_Scripts/Monster/WorldMonster.cs`
- `Assets/02_Scripts/Monster/SandTomb.cs`
- `Assets/02_Scripts/Monster/ShooterWorldMonster.cs`
- `Assets/02_Scripts/Monster/Centipede.cs`
- `Docs/Features/PlayerRunner.md`
- `Docs/Features/MonstersAndProjectiles.md`
- `Docs/Work/Active/W-20260828-004-runner-shotgun.md`

## 예약 Scene·Prefab·Data Asset

- `Assets/03_Prefabs/Player/Player Runner.prefab`
  - 기존 무기 component fileID, PlayerRunner `_weaponBehaviour`, NetworkObject `NetworkedBehaviours`와 공용 `Muzzle` 참조를 유지한 채 script를 `ShotgunWeapon`으로 전환한다.
  - 5발, 초당 1.25회, shell당 1초, 3/5/7m, 피해 36/18/9, 전체 원뿔 24도, 달리기 명중률 50%, 8펠릿, 1m/0.2초 넉백과 7m/0.1초 기본 tracer를 설정한다.

Scene, ScriptableObject, ProjectSettings, Package, Monster prefab과 Runner Projectile prefab은 변경하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- `IRunnerWeapon`, `NetworkInputData`, `NetworkInputSystem`과 Bootstrapper는 변경하지 않는다.
- `RunnerWeaponNetworkBehaviour`에 기존 무기의 전체 탄창 장전을 기본값으로 유지하는 적립 장전·사격 중단 hook과 무기별 shot presentation hook을 추가한다.
- `ShotgunWeapon`은 sequence, seed, 중심 방향과 8개 펠릿 방향을 담은 `ShotgunShotPresentation` event를 모든 Peer에 제공한다.
- `ShotgunWeapon`은 target NetworkId/위치, hit 또는 MISS, 피해, 넉백 여부와 sequence를 담은 `ShotgunTargetResultPresentation` event를 요청 Runner의 Input Authority에 제공한다.
- `WorldMonster.TryApplyKnockback`을 State Authority 전용 공용 진입점으로 추가한다. 기본 이동형은 허용하고 `SandTomb`, `ShooterWorldMonster`, `Centipede`는 면역으로 override한다.
- Project Map의 기능 라우팅은 변경하지 않는다.

## 네트워크·Peer 동등성

- 입력 원점: Host-local과 Client Input Authority의 기존 `NetworkInputSystem`이 좌클릭 유지, R key-down, 커서 조준점, LeftShift와 이동 입력을 `NetworkInputData`로 전달한다.
- 검증·변경 owner: 해당 PlayerRunner의 State Authority가 실제 달리기와 슬라이드 상태를 결정한다. 무기 State Authority만 cooldown, shell 장전, 탄약, shot sequence, RNG, 원뿔/가시성/MISS 판정, WorldMonster 피해와 넉백을 한 번 수행한다.
- 지속 반환 경로: Networked 탄약, 현재 shell timer와 진행률, reload 상태, shot sequence, 명중 RNG와 pellet seed가 Host·Client HUD 및 Late Join의 원본이다. WorldMonster 위치는 기존 NetworkRigidbody로 복제한다.
- 일회성 반환 경로: 복제 shot sequence와 pellet seed로 모든 Peer가 같은 `ShotgunShotPresentation`과 8개 기본 tracer를 만들고, State Authority RPC는 해당 Input Authority에 대상별 hit/MISS·피해·넉백 결과를 전달한다. Host-local도 같은 결과 계약을 정확히 한 번 사용한다.
- 권위 없는 Client는 탄약, RNG, 피해, 넉백을 직접 변경하지 않는다. 정지·일반 이동은 대상별 100%, 실제 달리기는 안정 대상 순서의 Networked RNG로 대상별 독립 50%를 판정한다.
- 한 발은 Collider를 WorldMonster별로 dedupe하고 공용 Muzzle 기준 전체 24도·최대 7m 안에서 3/5/7m 피해 36/18/9를 적용한다. 첫 가시 Collider가 대상이 아니면 제외하여 비관통을 보장하고 TrackMonster는 무시한다.
- 3m 이내 성공 적중은 이동형 WorldMonster를 0.2초 동안 1m 밀며 기존 바위·자원·Territory·Sanctuary 경계 판정에서 중단한다. 새 넉백은 진행 중 값을 교체하고 면역형은 피해만 받는다.
- Late Join은 현재 ammo, shell reload timer/progress, sequence, RNG, pellet seed와 이미 반영된 Monster 체력·위치를 복원하며 과거 일회성 shot/result event는 재생하지 않는다.
- Runner·Monster Despawn, disconnect와 Scene unload에서 weapon event, presentation cache, target result와 knockback timer가 남지 않게 한다. AOI와 Additive Scene 준비 계약은 변경하지 않는다.
- 실제 Host·Client를 실행하지 못하면 Host 시작 → Client 참가 → 양쪽 정지/이동/달리기 다중 대상 사격 → MISS/result event → 적립 장전/사격 중단 → 슬라이드 거부 → 비관통/거리 피해 → 이동형/면역형/경계 넉백 → Late Join → Despawn 순서의 수동 절차와 미검증 상태를 기록한다.

## 다른 활성 작업과 겹치는 부분

`CheckStart`와 `Docs/Work/Active/` 확인 결과 `README.md` 외 Active 작업 파일이 없어 겹침이 없다.

## 범위 밖

- 무기 선택·구매·런타임 교체와 예비탄 제한
- 치명타, 스킬·아이템 상호작용과 TrackMonster 피해
- MISS 텍스트, 총기 모델, 애니메이션, 정식 tracer/muzzle VFX와 SFX. 발사 확인용 0.1초 기본 LineRenderer tracer는 포함한다.
- HUD layout, Scene, Bootstrapper, ProjectSettings, Package와 AOI 변경
- 기존 돌격소총·쌍권총 코드 제거

## 완료 조건

- 기본 산탄총이 5발을 0.8초 기본 간격으로 소비하고, 1초마다 1발을 채우며, 탄약이 있으면 사격으로 장전을 중단하고 공격·재장전 속도 scaler를 적용한다.
- 한 발이 최대 7m·전체 24도 원뿔에서 WorldMonster를 한 번씩 판정하고, 3/5/7m 경계의 최종 피해가 `36/18/9 × WeaponDamage × WeaponDamageScaler`이며 가시성 차단과 TrackMonster 무시를 지킨다.
- 정지·일반 이동은 100%, 달리기는 대상별 독립 50%이며 Host·Client가 같은 8펠릿 presentation·0.1초 기본 tracer와 요청자 hit/MISS 결과를 받는다.
- 3m 이내 이동형은 1m/0.2초 넉백되고 경계에서 중단하며 SandTomb, Rafflesia, Gigantia는 밀리지 않는다.
- Host-local 중복 실행이 없고 권위 없는 Client mutation이 없으며 Late Join, Despawn과 delayed readiness에서 stale 상태·event가 없다.
- 기존 돌격소총과 쌍권총 회귀, 집중 EditMode 테스트, Unity/Fusion Weaver compile, prefab GUID·fileID·수치, `.meta` pairing, `git diff --check`와 예약 외 변경 부재를 검증한다.
- 실제 Host·Client 실행을 못 하면 정확한 수동 절차와 미검증 항목을 남긴다.
- 롤백은 Player Runner prefab의 동일 component를 `AssaultRifleWeapon` GUID와 기존 30/8/3/6/10/8도 설정·Runner Projectile 참조로 되돌리고 신규 Shotgun 코드와 Monster 넉백 변경을 제거하는 방식으로 수행한다.

## 실제 변경

- `RunnerWeaponNetworkBehaviour`에 기존 무기의 전체 탄창 장전을 기본값으로 유지하는 incremental reload, reload fire-interrupt와 weapon-specific presentation hook을 추가했다. `RunnerProjectileWeapon`은 새 shot sequence 인자를 받되 기존 AR·쌍권총 발사 동작은 바꾸지 않았다.
- `ShotgunRules`와 테스트를 추가해 3/5/7m 피해 경계, ±12도 원뿔, 달리기 hit/MISS, seed·sequence 기반 펠릿 각도와 5발 적립 장전 규칙을 순수 Logic으로 분리했다.
- `ShotgunWeapon`은 State Authority에서 7m overlap, WorldMonster Collider dedupe, 24도 원뿔, 첫 Collider 가시성, NetworkId 안정 순서와 대상별 RNG를 판정하고 거리 피해·근거리 넉백을 한 번 적용한다. TrackMonster는 `WorldMonster` 후보가 아니므로 제외된다.
- `ShotgunWeapon`은 Networked hit RNG와 pellet seed를 소유한다. 복제 sequence/seed를 소비하는 `ShotgunShotPresented`와 State Authority→Input Authority reliable target result RPC의 `TargetResultPresented`를 추가했다. Despawn에서 두 event와 로컬 후보 cache를 정리한다.
- 사용자 확인 요청에 따라 `ShotgunShotPresented`의 8개 방향을 사용하는 7m 황백색 LineRenderer tracer를 추가했다. 모든 Peer에서 0.1초 동안 페이드하며 별도 Asset이나 network state 없이 동작하고 Despawn에서 coroutine, line object와 runtime material을 정리한다.
- `Monster`에 권위 전용 Networked knockback timer·velocity와 AI보다 먼저 실행하는 이동 tick을 추가했다. `WorldMonster.TryApplyKnockback`은 기존 `IsMovementPathBlocked`를 사용하며 SandTomb, `ShooterWorldMonster`, `Centipede`는 면역 override다.
- Player Runner prefab의 fileID `-3321396743853381275`, `_weaponBehaviour`, NetworkObject behaviour 등록과 Muzzle fileID `5455947531722268542`를 유지하고 script GUID와 산탄총 수치만 전환했다. Runner Projectile prefab과 Scene은 변경하지 않았다.
- `PlayerRunner.md`와 `MonstersAndProjectiles.md`에 산탄총, 적립 장전, presentation/result, Late Join과 권위 넉백·면역 계약을 기록했다.

## 검증 결과

- 예약 commit `8f1569e978422531bacc55fece029c75492eec83`을 upstream에서 `VerifyReservation`으로 확인해 `READY_TO_IMPLEMENT`를 받은 뒤 구현했다.
- 최종 handoff 직전 `dotnet build ProjectIO.RunnerWeapons.Tests.csproj --no-restore --nologo`와 `dotnet build Assembly-CSharp.csproj --no-restore --nologo`를 다시 실행해 두 빌드 모두 경고 0, 오류 0을 확인했다.
- 새 `ShotgunRules.cs`를 임시 source include한 `dotnet build ProjectIO.RunnerWeapons.csproj --no-restore --nologo`: 경고 0, 오류 0.
- 새 `ShotgunRulesTests.cs`를 임시 source include한 `dotnet build ProjectIO.RunnerWeapons.Tests.csproj --no-restore --nologo`: 경고 0, 오류 0. NUnit source와 기존 회귀 테스트는 컴파일됐지만 이 Unity 생성 csproj에는 standalone test adapter가 없어 `dotnet test`로 실제 case 실행 결과를 얻지 못했다.
- 새 Shotgun adapter 3개를 임시 source include한 `dotnet build Assembly-CSharp.csproj --no-restore --nologo`: 오류 0. 기존 코드의 미사용/deprecated/Unity message signature 경고 13개만 확인됐다. 임시 csproj include는 검증 직후 모두 원복했으며 csproj는 Git 비추적 생성 파일이다.
- 기본 tracer 추가 후 Unity가 갱신한 `Assembly-CSharp.csproj`로 같은 전체 build를 다시 실행해 오류 0, 기존 경고 13개를 확인했고 `git diff --check`도 통과했다. Prefab에는 enabled, 황백색, 0.1초, 시작 0.035m/끝 0.008m 굵기가 직렬화됐다.
- 최신 tracer source는 열린 Unity Editor의 `Library/ScriptAssemblies/Assembly-CSharp.dll`보다 새로워 Editor import·Fusion Weaver 반영은 아직 수동 Assets Refresh 후 확인해야 한다. CLI `dotnet build` 컴파일과 Unity runtime import 검증을 구분한다.
- Prefab 정적 검증에서 Shotgun script GUID는 1회, 기존 weapon component fileID는 component·GameObject·PlayerRunner·NetworkObject 연결의 4회이며 Muzzle과 `5 / 1.25 / 1 / 36·18·9 / 3·5·7 / 24 / 50 / 8 / 1 / 0.2` 값이 확인됐다.
- 새 script 5개와 `.meta` 5개 pairing, 예약 범위 안 변경만 존재함과 `git diff --check` 통과를 확인했다.
- 열린 Unity Editor가 자동 csproj refresh를 수행하지 않았고 `computer-use`의 세션 CLI `orca-ide`도 없어 Unity Test Runner, Asset import, Fusion IL Weaver와 실제 Host·Client 실행은 수행하지 않았다.

## 남은 위험

- Unity에서 Assets Refresh 후 RunnerWeapons EditMode test를 실행해 3/5/7m, ±12도, 대상별 50%, 8펠릿, 적립 장전·사격 중단과 AR/쌍권총 회귀 case가 실제 통과하는지 확인해야 한다.
- 게임 화면에서 Host와 Client 각각 사격할 때 공용 Muzzle에서 8개 tracer가 7m까지 한 번만 나타나고 약 0.1초에 페이드하며, 사격 sequence가 없는 Late Join에는 과거 tracer가 나타나지 않고 Despawn 후 child object가 남지 않는지 확인해야 한다.
- Unity compile과 Fusion IL Weaver 완료 후 Player Runner prefab에 Missing Script가 없고 NetworkedBehaviour 등록·Muzzle 참조가 유지되는지 확인해야 한다.
- Host 시작 → Client 참가 → 양쪽 Runner로 정지/이동/달리기 다중 대상 사격 → 대상별 MISS/result event → R·빈 탄창 적립 장전과 1발 뒤 사격 중단 → 슬라이드 발사 거부 → 일렬 대상 비관통과 3/5/7m 피해 → 이동형 1m/0.2초 넉백 → SandTomb/Rafflesia/Gigantia 면역 → 바위·자원·Territory·Sanctuary 경계 중단 → 빈 공간 7m pellet presentation → Host 중복 event/피해 없음 순서로 확인해야 한다.
- 부분 장전 도중 새 Client를 참가시켜 ammo, 현재 shell 진행률, reload, sequence, RNG와 pellet seed의 현재 상태만 복원되고 과거 event가 재생되지 않는지 확인한 뒤 Runner/Monster Despawn 후 stale event·knockback이 없는지 확인해야 한다.
- 별도 fresh Critic context를 사용할 수 없어 Gauntlet 독립 검토는 수행하지 못했다. 현재 self-review와 정적·컴파일 증거만 기록했으며 Unity Physics/Fusion Peer 검증 전에는 런타임 완료로 간주하지 않는다.
- Unity가 자동 재정렬한 `ProjectIO.slnx` 변경이 예약 밖에 별도로 나타났으며 산탄총 구현에 포함하거나 stage하지 않는다.
