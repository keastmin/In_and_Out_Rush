# W-20260827-001 Runner dual pistols

Status: Completed

## 동기화 기준

- Base Commit: 3eac86b1be7673f2ac26d8300c140d6107b4bc27
- 공용 Upstream: `origin/rebuild-development-environment`
- CheckStart: `READY_TO_CHECK_CONFLICTS` (`AHEAD=0`, `BEHIND=0`, 2026-08-27)

## 담당자

Codex `/root`

## 기능

Player Runner의 공용 네트워크 무기 계약과 첫 구현인 쌍권총

## 목표

- Input Authority의 좌클릭 유지, R 재장전, 커서 조준 의도를 기존 Fusion Input 경로로 State Authority에 전달한다.
- State Authority가 탄창, 재장전, 발사 간격, 투사체 Spawn과 WorldMonster 피해를 한 번만 결정한다.
- 피해 1, 초당 4발, 사거리 10m, 16발 탄창, 재장전 2.5초, 무제한 예비탄, 명중 편차 없음의 쌍권총을 구현한다.
- Host 로컬과 Client Input Authority Runner가 같은 입력 가능 여부, 복제 탄약·재장전 상태, 성공 사격과 실패 상태를 관찰한다.
- 사용자 후속 HUD·모델 구현이 읽을 수 있는 상태 snapshot과 좌우 교대 사격 presentation event 및 연결 절차를 제공한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/PlayerRunner.md`
- `manage-feature-work`
- `photon-fusion-feature`

## 예상 수정 코드

- `Assets/02_Scripts/NetworkInputSystem.cs`
- `Assets/02_Scripts/Network/Fusion Test/NetworkInputData.cs`
- `Assets/02_Scripts/Player/Player Runner/IRunnerWeapon.cs`
- `Assets/02_Scripts/Player/Player Runner/RunnerProjectileWeapon.cs`
- `Assets/02_Scripts/Player/Player Runner/RunnerProjectile.cs`
- `Assets/02_Scripts/Player/Player Runner/Network/PlayerRunner.cs`
- `Assets/02_Scripts/Features/RunnerWeapons.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Logic.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Logic/ProjectIO.RunnerWeapons.asmdef`
- `Assets/02_Scripts/Features/RunnerWeapons/Logic/ProjectIO.RunnerWeapons.asmdef.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Logic/RunnerWeaponHand.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Logic/RunnerWeaponHand.cs.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Logic/RunnerWeaponStatus.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Logic/RunnerWeaponStatus.cs.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Logic/RunnerWeaponRules.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Logic/RunnerWeaponRules.cs.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion/RunnerWeaponNetworkBehaviour.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion/RunnerWeaponNetworkBehaviour.cs.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion/DualPistolWeapon.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion/DualPistolWeapon.cs.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Tests.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Tests/ProjectIO.RunnerWeapons.Tests.asmdef`
- `Assets/02_Scripts/Features/RunnerWeapons/Tests/ProjectIO.RunnerWeapons.Tests.asmdef.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Tests/RunnerWeaponRulesTests.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Tests/RunnerWeaponRulesTests.cs.meta`
- `Docs/Features/PlayerRunner.md`
- `Docs/Work/Active/W-20260827-001-runner-dual-pistols.md`

## 예약 Scene·Prefab·Data Asset

- `Assets/03_Prefabs/Player/Player Runner.prefab`
  - 기존 투사체 무기 component를 `DualPistolWeapon`으로 전환하고 NetworkObject의 NetworkedBehaviours에 정확히 한 번 등록한다.
  - 기존 공용 Muzzle은 fallback으로 유지하고 `Left Pistol Anchor`, `Right Pistol Anchor`를 실제 교대 Spawn 위치로 연결한다. Runner Projectile 참조를 유지하며 권총 모델, HUD, VFX, SFX는 추가하지 않는다.
- `Assets/03_Prefabs/Player/Runner Projectile.prefab`
  - 10m authoritative 이동 거리 제한과 기존 Fusion Spawn/NetworkTransform 구성을 검증한다.

Scene, ScriptableObject, ProjectSettings, Package는 변경하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- `IRunnerWeapon`은 State Authority 발사와 재장전 명령, 읽기 전용 `RunnerWeaponStatus`, `StatusChanged`, `ShotPresented`를 제공한다.
- `RunnerWeaponNetworkBehaviour`가 Networked 탄약, 재장전 timer, 발사 cooldown, 사격 sequence를 소유하고 Late Join에 복원한다.
- `RunnerProjectileWeapon`은 선택된 hand의 Muzzle·projectile Spawn과 평면 조준 정규화를 제공하는 기반으로 축소하고, 활성 Player Runner prefab은 좌우 Muzzle을 선택하는 `DualPistolWeapon`만 사용한다.
- `PlayerRunner`는 현재 무기 상태 원본을 공개하고 R 입력을 발사보다 먼저 처리한다. 슬라이드 중 발사만 차단하며 재장전과 달리기 사격은 허용한다.
- Project Map의 책임·라우팅은 바꾸지 않으며 StageBootstrapper와 Stage UI를 변경하지 않는다.

## 네트워크·Peer 동등성

- 입력 원점: Host-local 및 Client Input Authority의 `NetworkInputSystem`이 좌클릭 유지, R key-down, 환경 Raycast 또는 수평면 fallback으로 얻은 커서 조준점을 `NetworkInputData`에 기록한다.
- 검증·변경 owner: 해당 PlayerRunner의 State Authority만 유효한 조준 방향, 사망·슬라이드·cooldown·reload·탄약 조건을 검증하고 탄약/sequence 변경, reload 시작·완료, `Runner.Spawn`과 WorldMonster 피해를 수행한다.
- 반환 경로: Networked 탄약, reload timer, shot sequence와 projectile NetworkObject lifetime이 지속 성공 결과다. 빈 탄창은 자동 reload 상태로, reload/cooldown/slide 거부는 복제 status와 projectile 미생성으로 관찰한다. Input Authority의 HUD와 모델은 authority guard 없이 snapshot/event를 읽는다.
- Host-local과 Client는 같은 Input→State Authority→복제 status/projectile 계약을 사용한다. Host가 server와 local client 역할로 Spawn, 탄약 소비, presentation event를 두 번 실행하지 않게 한다.
- 사격 sequence 홀짝으로 Left/Right를 계속 교대하며 부분 재장전 뒤에도 sequence를 초기화하지 않는다. presentation event는 모든 관찰 peer에서 로컬 VFX/SFX/반동용으로 소비하고 게임 상태를 변경하지 않는다.
- Late Join은 현재 탄약·reload timer·shot sequence와 살아 있는 projectile을 Fusion 상태에서 복원한다. Despawn은 로컬 event 구독과 임시 presentation cache를 정리한다. AOI 설정은 변경하지 않는다.
- 실제 runtime 증거가 없으면 Host와 Client를 분리해 미검증으로 기록한다. 수동 절차는 Host 시작 → Client 참가 → 양쪽 Runner의 연사/부분 R reload/빈 탄창 자동 reload/달리기 사격/슬라이드 발사 거부 → Late Join 중 탄약·reload 복원 → WorldMonster 피해와 TrackMonster 무시 → Despawn 정리 순서다.

## 다른 활성 작업과 겹치는 부분

없음. `Docs/Work/Active/README.md` 외 Active 작업 파일이 없고 CheckStart가 보고한 충돌도 없다.

## 범위 밖

- 권총 모델, 애니메이션, HUD layout, VFX, SFX Asset 제작 또는 Scene/UI Prefab 연결
- 치명타, 확률적 miss, 유한 예비탄, 개별 권총별 독립 탄창
- TrackMonster 피해, 관통, knockback, 다른 무기 구현과 무기 교체 UI
- 스킬·아이템 사용 중 발사/재장전 상호작용
- Fusion package, AOI, StageBootstrapper, Scene, ProjectSettings 변경

## 완료 조건

- 16발이 0.25초 기본 간격으로 소비되고 좌우 hand와 실제 Left/Right Muzzle Spawn 위치가 엄격히 교대한다.
- R은 부분 탄창에서 발사보다 우선해 2.5초 reload를 시작하고, 빈 탄창 발사 시 자동 reload하며 완료 뒤 16발로 채운다.
- 공격속도와 reload 속도 scaler가 duration에 적용되고 reload 중 발사, full magazine reload, slide 중 발사가 상태를 잘못 변경하지 않는다.
- 달리기 중에는 발사 패널티가 없고 계속 누른 발사 입력은 reload 완료 뒤 다음 허용 tick부터 재개된다.
- 빈 방향 투사체가 10m에서 Despawn하고, 첫 WorldMonster에 피해 1을 한 번 적용한 뒤 Despawn하며 TrackMonster에는 피해를 주지 않는다.
- Host와 Client가 입력, ammo/reload HUD source, projectile 결과와 presentation hand를 동등하게 관찰하고 Host-local 중복 실행이 없다.
- Late Join, PlayerRunner/Projectile Despawn과 delayed Spawn 준비에서 stale event 또는 잘못된 authoritative mutation이 없다.
- 집중 EditMode 테스트, Unity/Fusion Weaver 포함 compile, Prefab 직렬화 참조, `git diff --check`, `git status`를 검증한다. 실제 Host·Client 실행을 못 하면 정확한 수동 절차와 미검증 항목을 남긴다.

## 실제 변경

- 순수 규칙 assembly에 탄약 소비, 좌우 교대, 수동·자동 재장전 조건, 공격속도·재장전 속도 계산을 추가하고 EditMode 테스트를 작성했다.
- `RunnerWeaponNetworkBehaviour`가 탄약, cooldown, reload timer·진행률, 사격 sequence·방향을 State Authority에서 관리하고 `RunnerWeaponStatus`, `StatusChanged`, `ShotPresented`를 공개하도록 구현했다.
- `RunnerProjectileWeapon`을 hand-aware authoritative Spawn 기반으로 전환하고 `DualPistolWeapon`이 Left/Right Muzzle을 선택하도록 구현했다. Player Runner prefab은 기존 공용 Muzzle을 fallback으로 유지하면서 `Left Pistol Anchor`, `Right Pistol Anchor`를 실제 교대 Spawn 위치로 연결하고 새 NetworkBehaviour를 정확히 한 번 등록한다.
- R key-down, 좌클릭 유지와 환경 Raycast·수평면 fallback 조준점을 Fusion Input에 추가했다. `PlayerRunner`는 R을 발사보다 먼저 처리하고 슬라이드 중 발사만 차단한다.
- Runner Projectile은 마지막 이동 tick을 한 번만 예약하고 다음 tick에 위치를 정확히 10m로 보정한 뒤 Despawn한다. 부동소수점 오차로 미세 속도를 반복 계산하며 정지하는 경로를 제거했고, 범위 안 첫 `WorldMonster`에만 한 번 피해를 준다.
- PlayerRunner 기능 문서에 HUD snapshot/event, 재장전 진행률, 좌우 모델 Presenter와 손 Anchor 연결 절차를 기록했다.

## 검증 결과

- `CheckStart`: `READY_TO_CHECK_CONFLICTS` (`AHEAD=0`, `BEHIND=0`)
- Active 충돌: 없음
- 예약 Commit·Push·원격 검증: `3a52c457523b2dfbfa93b13822041562aba96c28`, `READY_TO_IMPLEMENT`, `AHEAD=0`, `BEHIND=0`
- 순수 규칙 임시 .NET harness: PASS. 16발 소비·좌우 교대, 빈 탄창 자동 재장전 조건, 부분·full magazine 재장전 조건, sequence 유지, 공격속도·재장전 속도 배율을 실행했다. 임시 harness는 검증 뒤 삭제했다.
- 생성된 `Assembly-CSharp.csproj`에 새 소스만 임시 Include한 C# 전체 build: 오류 0, 기존 코드 warning 13. 임시 Include는 검증 직후 원복했다.
- 사거리 정지 수정 후 Unity가 갱신한 실제 `ProjectIO.RunnerWeapons.csproj` 참조를 사용한 `dotnet build Assembly-CSharp.csproj --no-restore`: 오류 0, 기존 코드 warning 13.
- Left/Right Muzzle 전달과 Prefab 연결 후 같은 전체 build 재실행: 오류 0, warning 16 (`Assembly-CSharp` 기존 warning 13과 Fusion Editor warning 3).
- `dotnet build ProjectIO.RunnerWeapons.Tests.csproj --no-restore`: 경고 0, 오류 0. Test assembly 컴파일은 통과했으며 Unity Test Runner 실행은 별도 미검증이다.
- Player Runner prefab 정적 검증: `DualPistolWeapon` component, `_weaponBehaviour`, `NetworkedBehaviours`, 공용 fallback·Left·Right Muzzle, Runner Projectile, 16/4/2.5/10 설정이 각각 정확히 한 번 존재한다.
- 새 Asset·폴더 meta 검증: 누락 없음, 새 GUID 중복 없음.
- 초기 Unity batchmode compile은 당시 동일 프로젝트를 연 PID 11460의 `Temp/UnityLockfile` 때문에 실행하지 못했다. 이후 Unity PID 45320의 강제 동기 recompile에서 `Fusion.CodeGen.ILWeaverBindings`를 포함한 Tundra build와 assembly reload가 성공했다. Unity Test Runner는 미실행이다.
- Host·Client, Late Join runtime: 미실행·미검증이다.
- `git diff --check`: 통과. Unity가 새 asmdef를 인식하며 자동 갱신한 예약 외 `ProjectIO.slnx` 변경은 이번 수정에서 건드리거나 포함하지 않고 별도 보존한다.

### 미검증 수동 절차

1. Unity Test Runner에서 EditMode `ProjectIO.RunnerWeapons.Tests`를 실행한다. Unity/Fusion compile과 assembly reload는 성공 확인됐다.
2. Host Runner에서 좌클릭을 유지해 초당 4발, 총 16발 소비와 Left/Right `ShotPresented` 및 실제 Pistol Anchor Spawn 위치 교대를 확인한다.
3. 부분 탄창에서 R을 눌러 2.5초 뒤 16발 복원, full magazine R 거부, 빈 탄창 발사 시 자동 재장전, 좌클릭 유지 시 완료 다음 허용 tick 발사 재개를 확인한다.
4. 달리기 연사에 패널티가 없는지, 슬라이드 중 새 발사만 차단되고 진행 중 재장전은 계속되는지 확인한다.
5. 별도 Client Input Authority Runner에서 2~4를 반복하고 Host와 Client의 ammo/reload snapshot, projectile 수, 좌우 presentation 결과가 같은지 확인한다.
6. 빈 방향 투사체가 10m 직전에서 멈춰 수명 timer를 기다리지 않고 즉시 사라지는지, `WorldMonster`에는 기본 피해 1 후 즉시 사라지는지, `TrackMonster`에는 피해 없이 통과하는지 확인한다.
7. 부분 탄창과 재장전 중 각각 Late Join해 현재 ammo·reload 진행률이 복원되는지 확인한다. Runner Despawn 뒤 HUD·Presenter event가 더 호출되지 않는지도 확인한다.

## 남은 위험

- 현재 Left/Right Muzzle은 사용자가 만든 Pistol Anchor 자체를 가리킨다. 모델을 배치한 뒤 총열 끝과 Anchor 원점이 다르면 별도 총구 child Transform을 만들고 참조를 교체해야 한다.
- 실제 Host·Client runtime과 Late Join 증거는 Unity 다중 Peer 환경에서 별도로 수집해야 한다.
