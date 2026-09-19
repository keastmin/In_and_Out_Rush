# W-20260920-001 트랙 몬스터 이동·공격 타워 피해 경로 교체

Status: Reserved

## 동기화 기준

- Base Commit: 4214f74f3ea849cec657f335e76b230e5e49daf3
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

- Codex (현재 작업)

## 기능

- Monster와 Projectile, Track와 Round, Tower와 Laboratory

## 목표

- 트랙 몬스터의 State Authority 이동과 경로 전환을 Rigidbody/NetworkRigidbody3D에서 Transform/NetworkTransform으로 교체한다.
- `Track Monster` 계열의 Rigidbody와 NetworkRigidbody3D를 제거하고 Body Collider를 활성 Trigger로 바꾼다. 공유 `Monster.prefab`과 월드 몬스터의 물리 이동은 유지한다.
- 공격 타워가 부모의 `ITowerDamagedMonster`를 통해 대상 선정과 피해를 수행하도록 바꾸고, 자식 Collider의 네트워크 오브젝트·투사체·범위 피해 연결을 복구한다.

## 읽을 문서와 Skill

- `AGENTS.md`, `Docs/PROJECT_MAP.md`
- `Docs/Features/MonstersAndProjectiles.md`, `Docs/Features/TrackAndRounds.md`, `Docs/Features/TowerAndLaboratory.md`
- `manage-feature-work`, `replace-existing-feature`, `photon-fusion-feature`

## 예상 수정 코드

- `Assets/02_Scripts/Monster/TrackMonster.cs`
- `Assets/02_Scripts/Features/Monster/Public.meta`
- `Assets/02_Scripts/Features/Monster/Public/ITowerDamagedMonster.cs`
- `Assets/02_Scripts/Features/Monster/Public/ITowerDamagedMonster.cs.meta`
- `Assets/02_Scripts/Tower/TowerTargeting.cs`
- `Assets/02_Scripts/Tower/Property Effect/TowerPropertyEffectApplier.cs`
- `Assets/02_Scripts/Tower/Towers/Attack Tower/AttackTower.cs`
- `Assets/02_Scripts/Tower/Towers/Attack Tower/BladeTower.cs`
- `Assets/02_Scripts/Tower/Towers/Attack Tower/SentryGunTower.cs`
- `Assets/02_Scripts/Tower/Towers/Center Tower/CenterTower.cs`
- `Assets/02_Scripts/Tower/Missile.cs`
- 계약·검증 문서: `Docs/Features/MonstersAndProjectiles.md`, `Docs/Features/TrackAndRounds.md`, `Docs/Features/TowerAndLaboratory.md`

## 예약 Scene·Prefab·Data Asset

- `Assets/03_Prefabs/Field/Track Monster.prefab`
- `Assets/03_Prefabs/Field/Track Monster Normal Small.prefab`
- `Assets/03_Prefabs/Field/Track Monster Normal Medium.prefab`
- `Assets/03_Prefabs/Field/Track Monster Normal Large.prefab`
- `Assets/03_Prefabs/Field/Track Monster Elite Walker.prefab`
- `Assets/03_Prefabs/Field/Track Monster Elite Predater.prefab`
- `Assets/03_Prefabs/Field/Track Monster Boss Invader.prefab`
- 하위 6종은 기본 `Track Monster.prefab`을 상속한다. 기본 프리팹 변경으로 모두 반영되면 하위 파일의 수정은 생략하고, 각각 최종 상속 결과를 확인한다.
- Scene·Data Asset 수정 예상 없음. `Assets/03_Prefabs/Field/Monster.prefab`은 월드 몬스터가 공유하므로 수정하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- 신규 공개 계약 `ITowerDamagedMonster`: 공격 타워의 우선순위 조회와 피해 적용 대상. `TrackMonster`가 구현하고 타워 소비자가 Collider의 부모에서 찾는다.
- Bootstrapper 변경 없음. `Monster`와 `IDamageable`의 월드 몬스터 경로는 유지한다.

## 네트워크·Peer 동등성

- 플레이어 입력 원점 변경 없음. 기존 건설 요청은 Player Builder의 Input Authority 경로를 유지한다. 타워의 자동 탐지·발사는 State Authority에서만 실행한다.
- State Authority가 대상 선정, 체력 변경, 트랙 이동·경로 전환, Spawn/Despawn을 결정한다. 비권위 Peer는 복제된 NetworkTransform, Monster 체력, 공격 결과와 효과를 관찰한다.
- Host 로컬과 Client 피어는 같은 Collider/인터페이스 대상에 대해 공격 결과를 확인한다. 신규 미리보기·HUD·거부 응답은 없으며, 기존 건설 실패 피드백은 변경하지 않는다.
- Host 중복 발사·피해, Client 직접 상태 변경, 타겟 Despawn 시 참조 정리, Stage 3 경로 전환, Late Join 위치·체력 복원, Trigger가 이동을 막지 않는지 확인한다.
- 가능한 범위에서 컴파일·Prefab 참조·집중 검증을 수행한다. 두 Peer 런타임을 실행하지 못하면 Host/Client별 타워 공격·트랙 전환·Late Join 수동 확인 절차와 미검증 항목을 결과에 기록한다.

## 다른 활성 작업과 겹치는 부분

- 확인 시 `Docs/Work/Active/`에 다른 예약 없음.

## 범위 밖

- 월드 몬스터의 Rigidbody 이동, Runner 무기 피해 계약, 공격 타워 건설·비용, 타워 공격 수치와 웨이브 구성 변경.

## 완료 조건

- 모든 `Track Monster` 계열 최종 Prefab에서 Rigidbody·NetworkRigidbody3D가 없고 NetworkTransform과 활성 Trigger Collider가 유효하다.
- 트랙 이동 속도, 영역 밖·라운드 강화 배율, Stage 3 전환·반복, 정착·내재화 결과가 유지된다.
- 모든 공격 타워와 센터타워의 기본 공격이 자식 Collider에서 부모 `ITowerDamagedMonster`를 찾고 State Authority에서 한 번만 피해를 준다. 탄환 시각 효과, 미사일 폭발, 블레이드 타격과 속성 효과의 대상 참조가 유효하다.
- 관련 Prefab 상속·GUID/fileID, 참조 검색, 집중 테스트·컴파일, `git diff --check`와 작업 트리 상태를 확인한다.
- 롤백은 이 작업의 Transform 이동·NetworkTransform 및 타워 계약 변경을 함께 되돌리고, 트랙 프리팹의 기존 Rigidbody·NetworkRigidbody3D 직렬화 구성을 복원한다.

## 실제 변경

- 예약 단계. 구현 전.

## 검증 결과

- 예약 단계. 구현 전.

## 남은 위험

- Unity/Fusion 런타임 Host·Client와 Late Join 검증은 구현 후 수행 가능 여부를 확인한다.
