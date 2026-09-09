# Player Runner

Status: Current

Last reviewed: 2026-08-28

## 책임

Runner의 이동, 체력·스태미나, 전투, 회피, 버프, 순간이동과 네트워크 입력을 관리한다.

## 주요 진입점

- `Assets/02_Scripts/Player/Player Runner/Network/PlayerRunner.cs`
- `PlayerRunnerMovement`, Combat·Slide·Tumble·Buff handler
- `NetworkInputSystem`, `NetworkInputData`
- `RunnerWeaponNetworkBehaviour`, `ShotgunWeapon`, `AssaultRifleWeapon`, `DualPistolWeapon`, `RunnerProjectile`

## 상태와 권한

Networked 플레이어 상태는 Fusion Authority 규칙을 따른다. 로컬 Camera·HUD 표현은 네트워크 상태와 분리한다.

무기는 Input Authority의 좌클릭 유지, R 재장전, 커서 조준점과 기존 이동 입력을 Fusion Input으로 전달한다. State Authority만 실제 달리기 여부를 판정하고 탄약, 발사 간격, 재장전, 명중 RNG, Projectile Spawn과 WorldMonster 피해를 변경한다. 탄약, 재장전 timer, 사격 sequence와 활성 무기의 추가 상태는 Networked 상태이므로 Host와 Client 및 Late Join이 같은 결과를 읽는다. 현재 Player Runner prefab의 기본 무기는 `ShotgunWeapon`이다.

## 주요 연결

Territory 보호 판정, Monster·Projectile 피해, Laboratory 상호작용, Runner UI, Cinemachine, Ping.

`PlayerRunner.Render`의 기존 `OnPositionChanged`는 Territory Trail의 입력 seam이다.
Input Authority peer는 이 이벤트로 local fixed Trail을 즉시 예측 표시하고,
State Authority는 같은 이동을 검증해 confirmed stream을 만든다. PlayerRunner가
Territory 결과나 Trail network state를 직접 소유하지는 않는다.

## 돌격소총 계약

돌격소총 코드는 보존하지만 현재 Player Runner prefab의 활성 무기는 아니다.

- 기본 피해 6, 초당 8발, 사거리 10m, 30발 탄창, 3초 재장전이며 예비탄은 무제한이다.
- 좌클릭을 유지하면 cooldown마다 공용 `Muzzle`에서 Projectile 한 개를 발사한다. R은 부분 탄창에서도 재장전을 시작하고, 0발에서 발사를 시도하면 자동 재장전한다.
- 공격속도와 재장전 시간은 각각 `WeaponAttackSpeedScaler`, `WeaponReloadSpeedScaler`를 적용한다. 최종 피해는 `6 × WeaponDamage × WeaponDamageScaler`이다.
- 정지와 일반 이동 중에는 커서 조준 방향을 그대로 사용한다. LeftShift, 이동 방향과 스태미나 조건을 모두 만족한 실제 달리기 중에는 State Authority의 Networked RNG에서 균등 표본 두 개를 소비하고, 평균한 삼각 분포로 좌우 최대 8도의 수평 탄퍼짐을 적용한다.
- 명중률 66% 설계 의도는 확률적 hit 판정이 아니라 탄퍼짐으로 표현하므로 실제 명중률은 대상 크기와 거리에 따라 달라진다.
- `ShotPresented`는 `RunnerWeaponHand.Primary`와 실제 탄퍼짐이 적용된 방향을 전달한다. 슬라이드 중에는 발사만 차단하고 재장전은 계속된다.
- Projectile은 최대 10m에서 Despawn하고 첫 WorldMonster에만 피해를 한 번 적용하며 TrackMonster는 통과한다.

## 산탄총 계약

- 기본 탄창 5발, 초당 1.25회, 최대 사거리 7m, 전체 원뿔각 24도이며 예비탄은 무제한이다. 공격속도 scaler를 적용한 기본 사격 간격은 0.8초다.
- 한 발은 State Authority가 공용 `Muzzle`에서 단일 원뿔 Physics query로 판정한다. WorldMonster별 Collider를 한 번으로 합치고 첫 가시 Collider가 해당 대상일 때만 적중 후보로 두므로 다른 대상과 장애물을 관통하지 않는다. TrackMonster는 판정 대상이 아니다.
- 거리 `0~3m / 3~5m / 5~7m`의 기본 피해는 `36 / 18 / 9`다. 최종 피해는 `거리 피해 × WeaponDamage × WeaponDamageScaler`다.
- 정지·일반 이동은 후보마다 100% 적중한다. 실제 달리기 중에는 안정적인 NetworkId 순서와 Networked RNG로 후보마다 독립 50% hit/MISS를 판정한다.
- 사격마다 Networked shot sequence와 pellet seed를 갱신한다. 모든 Peer의 `ShotgunShotPresented`는 같은 중심 방향과 원뿔 안의 결정적 8개 연출용 펠릿 방향을 제공하며 실제 피해에는 관여하지 않는다. 기본 presentation은 공용 Muzzle에서 각 방향으로 7m를 그리는 황백색 LineRenderer tracer를 0.1초 동안 페이드해 게임 화면에서 발사를 확인할 수 있게 한다.
- State Authority의 `TargetResultPresented` RPC 결과는 해당 Runner의 Input Authority에 target ID/위치, hit 또는 MISS, 적용 피해, 넉백 여부와 shot sequence를 정확히 한 번 제공한다. 실제 MISS 텍스트, 총기 모델, VFX와 SFX 연결은 별도 presentation 작업이다.
- R 또는 빈 탄창 발사는 연속 적립 장전을 시작한다. Reload Speed Scaler가 적용된 기본 1초마다 한 발을 채우고, 한 발 이상이면 발사 입력이 현재 shell 장전을 취소하고 즉시 발사할 수 있다. 완전 장전 R은 무시하며 이동·달리기·슬라이드는 장전을 중단하지 않는다.
- 슬라이드 중 발사는 기존 PlayerRunner 입력 경로에서 차단된다. 3m 이내 성공 적중은 넉백 허용 WorldMonster에 0.2초 동안 1m 넉백을 요청한다.
- 탄약, 현재 shell timer와 진행률, reload 상태, sequence, 명중 RNG와 pellet seed는 Networked 상태다. Late Join은 현재 상태만 복원하고 과거 `ShotgunShotPresented`나 target result를 재생하지 않는다.

## 쌍권총 계약

쌍권총 코드는 보존하지만 현재 Player Runner prefab의 활성 무기는 아니다.

- 기본 피해 1, 초당 4발, 사거리 10m, 16발 탄창, 2.5초 재장전이며 예비탄은 무제한이다.
- 좌클릭을 유지하면 cooldown마다 발사한다. R은 부분 탄창에서도 재장전을 시작하고, 0발에서 발사를 시도하면 자동 재장전한다.
- 공격속도와 재장전 시간은 각각 `WeaponAttackSpeedScaler`, `WeaponReloadSpeedScaler`를 적용한다.
- 달리기 중 발사 패널티는 없고 슬라이드 중에는 발사만 차단한다. 재장전은 이동·달리기·슬라이드 중에도 계속된다.
- 사격 sequence 홀짝으로 Left와 Right를 계속 교대한다. 부분 재장전은 sequence를 초기화하지 않는다.
- Projectile은 사격 sequence의 Left/Right에 맞춰 `Left Pistol Anchor`와 `Right Pistol Anchor`에서 교대로 Spawn하고 최대 10m에서 Despawn한다. 좌우 참조가 빠지면 기존 공용 `Muzzle`을 fallback으로 사용한다. 첫 WorldMonster에만 피해를 한 번 적용하며 TrackMonster는 통과한다.

## HUD·모델 연결

실제 무기 모델, 애니메이션, HUD, 정식 VFX와 SFX는 이 기능이 소유하지 않는다. 산탄총에는 별도 Asset이 필요 없는 짧은 기본 tracer만 포함하며, 연결 코드는 로컬 Input Authority의 `PlayerRunner.Weapon`을 사용한다.

- HUD는 `IRunnerWeapon.Status`를 초기 표시하고 `StatusChanged`를 구독해 탄약과 재장전 시작·완료를 갱신한다. 재장전 바는 `Status.IsReloading` 동안 매 frame `Status.ReloadProgress01`을 읽는다.
- 탄약 표시는 `Status.Ammunition / Status.MagazineCapacity`를 사용한다. 현재 기본 산탄총 표시는 `현재 탄약 / 5`이며 재장전 바는 현재 shell 한 발의 진행률을 표시한다.
- 돌격소총 Presenter는 `ShotPresented(RunnerWeaponHand.Primary, direction)`을 구독해 단일 총구 반동, VFX와 SFX를 재생한다. 현재 Player Runner prefab의 공용 `Muzzle`이 논리 Projectile Spawn 위치다.
- 두 권총 모델은 Player Runner prefab의 `Root` 아래 `Left Pistol Anchor`, `Right Pistol Anchor`에 배치한다. 이 Anchor가 논리 Projectile Spawn 위치이므로 실제 총구 위치에 맞춘다. 별도 총구 child Transform을 만들면 `DualPistolWeapon`의 Left/Right Muzzle 참조를 그 child로 교체한다.
- 모델 Presenter는 `ShotPresented(RunnerWeaponHand hand, Vector3 direction)`을 구독하고 전달된 손에만 반동, 총구 VFX와 SFX를 재생한다. 이 event에서는 게임 상태나 피해를 변경하지 않는다.
- 구독자는 PlayerRunner 또는 무기 Despawn 전에 event 구독을 해제해야 한다. 무기 component도 Despawn 시 남은 로컬 구독을 정리한다.

## 관련 Asset

- Player Runner prefab
- `GamePresentation.unity`의 Runner camera와 UI

## 변경 시 확인

- Input Authority와 State Authority 구분
- Host·Client 이동과 피해 결과
- Host·Client의 좌클릭/R 입력, 탄약·재장전 상태와 Primary/Left/Right presentation 동등성
- 산탄총의 3/5/7m 피해 경계, ±12도 원뿔, 다중 Collider dedupe와 첫 Collider 비관통
- 산탄총의 정지·이동 100%, 달리기 대상별 50% MISS, 결정적 8펠릿과 owner 결과 event
- Host·Client Game View에서 공용 Muzzle 기준 7m 황백색 tracer 8개가 약 0.1초 페이드하고 Late Join·Despawn에 남지 않는지
- 산탄총의 5발 적립 장전·사격 중단·슬라이드 발사 거부, 근거리 넉백과 면역형
- 돌격소총 정지·일반 이동 무편차와 달리기 최대 8도 탄퍼짐, 쌍권총 좌우 교대 회귀
- WorldMonster 피해와 TrackMonster 무시, 산탄총 7m와 Projectile 무기 10m 사거리
- Late Join 탄약·현재 shell timer·sequence·RNG·pellet seed 복원과 Host-local 중복 실행 방지
- Spawn 시 StageBootstrapper 준비 상태
- 사망·Despawn 후 이벤트와 registry 정리
- Host/Client Input Authority에서 owner Trail의 즉시 표시와 State Authority
  terminal 뒤 정리 동등성

## 기술 부채

주 PlayerRunner 클래스가 여러 handler를 조정하고 전역 Stage 참조도 사용한다. 흐름별로 공개 연결부를 줄여야 한다.
