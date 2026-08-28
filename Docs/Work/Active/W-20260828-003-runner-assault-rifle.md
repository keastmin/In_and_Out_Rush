# W-20260828-003 Runner assault rifle

Status: Reserved

## 동기화 기준

- Base Commit: 41ad52ffd4215bf1ffc8cd61dcb7e0db11b4de35
- 공용 Upstream: `origin/rebuild-development-environment`
- CheckStart: `READY_TO_CHECK_CONFLICTS` (`AHEAD=0`, `BEHIND=0`, 2026-08-28)

## 담당자

Codex `/root`

## 기능

Player Runner의 기본 네트워크 무기인 돌격소총과 달리기 탄퍼짐

## 목표

- 기존 쌍권총 코드를 보존하면서 Player Runner prefab의 기본 무기를 돌격소총으로 전환한다.
- 기본 피해 6, 초당 8발, 사거리 10m, 30발 탄창, 3초 재장전, 무제한 예비탄의 단일 Projectile 무기를 구현한다.
- 정지와 일반 이동은 커서 조준 방향 그대로 발사하고, 실제 달리기 중에는 좌우 최대 8도의 삼각 분포 수평 탄퍼짐을 적용한다.
- Input Authority가 전달한 기존 이동·무기 입력을 State Authority가 검증하여 탄약, 재장전, 난수 상태, Projectile Spawn과 피해를 한 번만 변경한다.
- 기존 HUD snapshot과 presentation event 계약을 유지하며 단일 총구를 `Primary`로 구분한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/PlayerRunner.md`
- `manage-feature-work`
- `photon-fusion-feature`

## 예상 수정 코드

- `Assets/02_Scripts/Features/RunnerWeapons/Logic/RunnerWeaponHand.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Logic/RunnerWeaponRules.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion/RunnerWeaponNetworkBehaviour.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion/DualPistolWeapon.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion/AssaultRifleWeapon.cs`
- `Assets/02_Scripts/Features/RunnerWeapons/Adapters/Fusion/AssaultRifleWeapon.cs.meta`
- `Assets/02_Scripts/Features/RunnerWeapons/Tests/RunnerWeaponRulesTests.cs`
- `Assets/02_Scripts/Player/Player Runner/IRunnerWeapon.cs`
- `Assets/02_Scripts/Player/Player Runner/RunnerProjectileWeapon.cs`
- `Assets/02_Scripts/Player/Player Runner/Network/PlayerRunner.cs`
- `Docs/Features/PlayerRunner.md`
- `Docs/Work/Active/W-20260828-003-runner-assault-rifle.md`

## 예약 Scene·Prefab·Data Asset

- `Assets/03_Prefabs/Player/Player Runner.prefab`
  - 활성 `DualPistolWeapon` component를 `AssaultRifleWeapon`으로 전환한다.
  - 기존 component fileID, PlayerRunner `_weaponBehaviour`, NetworkObject `NetworkedBehaviours`, Runner Projectile과 공용 `Muzzle` 참조를 유지한다.
  - 돌격소총 수치를 30발, 초당 8발, 재장전 3초, 피해 6, 사거리 10m, 달리기 최대 탄퍼짐 8도로 설정한다.
  - `Left Pistol Anchor`, `Right Pistol Anchor`와 쌍권총 코드는 보존한다.

Scene, ScriptableObject, ProjectSettings, Package와 Runner Projectile prefab은 변경하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- `IRunnerWeapon.TryFire`에 State Authority가 계산한 실제 달리기 여부를 추가한다.
- `RunnerWeaponHand`에 단일 총구용 `Primary`를 추가한다.
- `RunnerWeaponNetworkBehaviour`는 무기별 사격점 선택 hook을 제공하고, `RunnerProjectileWeapon`은 무기별 조준 방향 보정 hook을 제공한다.
- 쌍권총은 기존 Left/Right 교대와 무편차 방향을 명시적으로 유지한다.
- `AssaultRifleWeapon`은 Networked `NetworkRNG` 상태를 소유하고 달리기 중에만 삼각 분포 탄퍼짐을 계산한다.
- Bootstrapper, NetworkInputData, NetworkInputSystem, Stage UI와 Project Map은 변경하지 않는다.

## 네트워크·Peer 동등성

- 입력 원점: Host-local 및 Client Input Authority의 기존 `NetworkInputSystem`이 LeftShift, 이동 방향, 좌클릭 유지, R key-down과 커서 조준점을 `NetworkInputData`로 전달한다.
- 검증·변경 owner: 해당 `PlayerRunner`의 State Authority가 이동 방향·스태미나·슬라이드/텀블을 반영해 실제 달리기 여부를 계산한다. 무기 State Authority만 cooldown·reload·탄약 조건을 검증하고 탄약/sequence/RNG 상태 변경, Projectile Spawn과 WorldMonster 피해를 수행한다.
- 탄퍼짐: 유효한 달리기 사격마다 Networked `NetworkRNG`에서 균등 표본 두 개를 소비해 평균낸 수평 각도를 `[-8도, +8도]`에 매핑한다. 정지·일반 이동은 RNG를 소비하지 않고 커서 방향을 유지한다.
- 반환 경로: Networked 탄약, reload timer, shot sequence, RNG 상태와 `LastShotDirection`, Projectile NetworkObject lifetime이 지속 결과다. `Status`/`StatusChanged`와 `ShotPresented(Primary, direction)`는 authority guard 없이 읽어 Host와 Client에 같은 탄약·재장전·실제 발사 방향을 제공한다.
- Host-local과 Client는 같은 Input → State Authority → 복제 상태/Projectile 계약을 사용한다. Host의 server·local-client 이중 역할이 탄약 소비, RNG 진행, Spawn, 피해 또는 presentation을 중복 실행하지 않게 한다.
- Late Join은 현재 탄약·reload timer·shot sequence·RNG와 살아 있는 Projectile을 Fusion 상태에서 복원한다. Runner Despawn은 공용 무기 event와 presentation cache를 정리한다. AOI와 Scene 준비 계약은 변경하지 않는다.
- 실제 다중 Peer 실행 증거가 없으면 Host와 Client를 분리해 미검증으로 기록한다. 수동 절차는 Host 시작 → Client 참가 → 양쪽 Runner의 정지/일반 이동 무편차 사격 → 달리기 탄퍼짐 → 부분/빈 탄창 재장전 → 슬라이드 거부 → 피해·사거리 → Late Join 복원 → Despawn 정리 순서다.

## 다른 활성 작업과 겹치는 부분

없음. `Docs/Work/Active/README.md` 외 Active 작업 파일이 없고 CheckStart가 보고한 충돌도 없다.

## 범위 밖

- 무기 선택·장착 UI와 런타임 무기 교체
- 돌격소총 모델, 애니메이션, HUD layout, VFX, SFX와 MISS 표시
- 정확히 66%를 보장하는 충돌 판정, 치명타, 관통과 유한 예비탄
- 다른 무기 구현, 스킬·아이템 사용 중 추가 상호작용
- StageBootstrapper, Scene, ProjectSettings, Package, AOI 변경

## 완료 조건

- 기본 돌격소총이 30발을 0.125초 기본 간격으로 소비하고 부분/빈 탄창에서 3초 재장전하며 공격·재장전 속도 scaler를 적용한다.
- 정지와 일반 이동의 방향 편차는 0도이고, 실제 달리기 사격은 항상 좌우 8도 범위이며 두 균등 표본 평균의 삼각 분포를 사용한다.
- 돌격소총은 공용 `Muzzle`에서 `Primary` presentation으로 Spawn하고, 쌍권총을 다시 연결하면 기존 Left/Right 교대가 유지된다.
- 기본 최종 피해는 `6 × WeaponDamage × WeaponDamageScaler`이고 첫 WorldMonster에 한 번만 적용한다. TrackMonster는 통과하고 Projectile은 최대 10m에서 Despawn한다.
- Host와 Client가 입력, 탄약·재장전 상태, 탄퍼짐 방향, Projectile 결과를 동등하게 관찰하며 Host-local 중복 실행이 없다.
- Late Join, Runner/Projectile Despawn과 delayed Spawn 준비에서 stale event나 잘못된 authoritative mutation이 없다.
- 집중 EditMode 테스트, Unity/Fusion Weaver compile, prefab 직렬화 참조, `git diff --check`, `git status`를 검증한다. 실제 Host·Client 실행을 못 하면 정확한 수동 절차와 미검증 항목을 남긴다.
- 롤백은 Player Runner prefab의 동일 component를 `DualPistolWeapon`으로 되돌리고 기존 16/4/2.5/1/10 및 Left/Right Muzzle 참조를 복원하는 방식으로 수행한다.

## 실제 변경

예약 후 기록한다.

## 검증 결과

예약 후 기록한다.

## 남은 위험

- 66% 명중률은 사용자가 선택한 탄퍼짐 표현으로 대체되므로 실제 충돌 확률은 대상 크기와 거리에 따라 달라진다.
- 실제 Host·Client runtime과 Late Join 증거는 Unity 다중 Peer 환경에서 별도로 확인해야 한다.
