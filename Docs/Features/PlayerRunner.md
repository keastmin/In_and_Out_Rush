# Player Runner

Status: Current

Last reviewed: 2026-08-27

## 책임

Runner의 이동, 체력·스태미나, 전투, 회피, 버프, 순간이동과 네트워크 입력을 관리한다.

## 주요 진입점

- `Assets/02_Scripts/Player/Player Runner/Network/PlayerRunner.cs`
- `PlayerRunnerMovement`, Combat·Slide·Tumble·Buff handler
- `NetworkInputSystem`, `NetworkInputData`
- `RunnerWeaponNetworkBehaviour`, `DualPistolWeapon`, `RunnerProjectile`

## 상태와 권한

Networked 플레이어 상태는 Fusion Authority 규칙을 따른다. 로컬 Camera·HUD 표현은 네트워크 상태와 분리한다.

쌍권총은 Input Authority의 좌클릭 유지, R 재장전과 커서 조준점을 Fusion Input으로 전달한다. State Authority만 탄약, 발사 간격, 재장전, Projectile Spawn과 WorldMonster 피해를 변경한다. 탄약, 재장전 timer, 사격 sequence는 `DualPistolWeapon`의 Networked 상태이므로 Host와 Client 및 Late Join이 같은 결과를 읽는다.

## 주요 연결

Territory 보호 판정, Monster·Projectile 피해, Laboratory 상호작용, Runner UI, Cinemachine, Ping.

`PlayerRunner.Render`의 기존 `OnPositionChanged`는 Territory Trail의 입력 seam이다.
Input Authority peer는 이 이벤트로 local fixed Trail을 즉시 예측 표시하고,
State Authority는 같은 이동을 검증해 confirmed stream을 만든다. PlayerRunner가
Territory 결과나 Trail network state를 직접 소유하지는 않는다.

## 쌍권총 계약

- 기본 피해 1, 초당 4발, 사거리 10m, 16발 탄창, 2.5초 재장전이며 예비탄은 무제한이다.
- 좌클릭을 유지하면 cooldown마다 발사한다. R은 부분 탄창에서도 재장전을 시작하고, 0발에서 발사를 시도하면 자동 재장전한다.
- 공격속도와 재장전 시간은 각각 `WeaponAttackSpeedScaler`, `WeaponReloadSpeedScaler`를 적용한다.
- 달리기 중 발사 패널티는 없고 슬라이드 중에는 발사만 차단한다. 재장전은 이동·달리기·슬라이드 중에도 계속된다.
- 사격 sequence 홀짝으로 Left와 Right를 계속 교대한다. 부분 재장전은 sequence를 초기화하지 않는다.
- Projectile은 사격 sequence의 Left/Right에 맞춰 `Left Pistol Anchor`와 `Right Pistol Anchor`에서 교대로 Spawn하고 최대 10m에서 Despawn한다. 좌우 참조가 빠지면 기존 공용 `Muzzle`을 fallback으로 사용한다. 첫 WorldMonster에만 피해를 한 번 적용하며 TrackMonster는 통과한다.

## HUD·모델 연결

실제 권총 모델, 애니메이션, HUD, VFX와 SFX는 이 기능이 소유하지 않는다. 연결 코드는 로컬 Input Authority의 `PlayerRunner.Weapon`을 사용한다.

- HUD는 `IRunnerWeapon.Status`를 초기 표시하고 `StatusChanged`를 구독해 탄약과 재장전 시작·완료를 갱신한다. 재장전 바는 `Status.IsReloading` 동안 매 frame `Status.ReloadProgress01`을 읽는다.
- 탄약 표시는 `Status.Ammunition / Status.MagazineCapacity`를 사용한다. 현재 쌍권총 기본 표시는 `현재 탄약 / 16`이다.
- 두 권총 모델은 Player Runner prefab의 `Root` 아래 `Left Pistol Anchor`, `Right Pistol Anchor`에 배치한다. 이 Anchor가 논리 Projectile Spawn 위치이므로 실제 총구 위치에 맞춘다. 별도 총구 child Transform을 만들면 `DualPistolWeapon`의 Left/Right Muzzle 참조를 그 child로 교체한다.
- 모델 Presenter는 `ShotPresented(RunnerWeaponHand hand, Vector3 direction)`을 구독하고 전달된 손에만 반동, 총구 VFX와 SFX를 재생한다. 이 event에서는 게임 상태나 피해를 변경하지 않는다.
- 구독자는 PlayerRunner 또는 무기 Despawn 전에 event 구독을 해제해야 한다. 무기 component도 Despawn 시 남은 로컬 구독을 정리한다.

## 관련 Asset

- Player Runner prefab
- `GamePresentation.unity`의 Runner camera와 UI

## 변경 시 확인

- Input Authority와 State Authority 구분
- Host·Client 이동과 피해 결과
- Host·Client의 좌클릭/R 입력, 탄약·재장전 상태와 좌우 presentation 동등성
- 슬라이드 발사 거부, 달리기 사격, WorldMonster 피해와 TrackMonster 무시
- Late Join 탄약·reload timer 복원과 Host-local 중복 Spawn 방지
- Spawn 시 StageBootstrapper 준비 상태
- 사망·Despawn 후 이벤트와 registry 정리
- Host/Client Input Authority에서 owner Trail의 즉시 표시와 State Authority
  terminal 뒤 정리 동등성

## 기술 부채

주 PlayerRunner 클래스가 여러 handler를 조정하고 전역 Stage 참조도 사용한다. 흐름별로 공개 연결부를 줄여야 한다.
