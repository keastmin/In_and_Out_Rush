# W-20260821-008 Stalker 투사체 공격 전환

Status: Reserved

## 동기화 기준

- Base Commit: 9c623f0d04cc3c236405a97a6f49cabefa64a671
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Monster and Projectile

## 목표

Stalker가 기존 감지·추적과 2m 공격 진입 사거리를 유지하면서 직접 피해 대신 네트워크 `MonsterProjectile`을 발사하게 한다. 현재 초당 3회·피해 1의 공격 성능을 보존하고, 발사체는 8m/s 속도와 5초 수명을 사용한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/MonstersAndProjectiles.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Monster/Stalker.cs`
- `Assets/02_Scripts/Projectile/MonsterProjectile.cs`
- `Assets/03_Prefabs/Field/Stalker.prefab`
- `Docs/Features/MonstersAndProjectiles.md`
- `Docs/Work/Active/W-20260821-008-stalker-projectile-attack.md`

## 예약 Scene·Prefab·Data Asset

- `Assets/03_Prefabs/Field/Stalker.prefab`: 기존 미커밋 머티리얼·장애물 여유값 변경을 보존한 채 `Muzzle` 자식과 Stalker의 투사체 참조·수치만 추가한다.
- `Assets/03_Prefabs/Field/Monster Projectile.prefab` 및 Network Prefab table은 변경하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- `MonsterProjectile.Initialize`의 발사자 인수를 `ShooterWorldMonster`에서 공통 `Monster`로 일반화한다. 기존 ShooterWorldMonster 호출은 호환되어야 한다.
- Bootstrapper, Spawn table, Scene, Network Prefab table, Runner/Builder 입력 계약은 변경하지 않는다.

## 네트워크·Peer 동등성

- 입력 기원: 없음. Stalker AI는 State Authority의 `FixedUpdateNetwork`에서만 동작한다.
- 권위: State Authority가 공격 타이머를 갱신하고 투사체를 정확히 한 번 Spawn·초기화하며, 투사체 State Authority가 충돌 피해와 Despawn을 결정한다.
- 결과 전달: `MonsterProjectile` NetworkObject와 NetworkTransform 복제로 Host와 Client가 생성·이동·Despawn을 관찰한다. 별도 RPC나 지속 상태는 추가하지 않는다.
- Host와 Client: 둘 다 동일한 복제 투사체를 보며, Client는 투사체 Spawn·피해·Despawn을 직접 변경하지 않는다. Runner 입력·HUD·미리보기 변경은 없다.
- 중복·준비·Late Join·정리: Host의 단일 AI tick만 Spawn하도록 유지하고, 비행 중 Late Join은 현재 NetworkObject/Transform 복제를 관찰한다. 수명 만료, 충돌, 아이템 파괴, 부모 몬스터 자신의 Collider 무시는 기존 정리 경로를 유지한다.
- 런타임 증거: Unity 환경에서 Host와 Client를 실행하여 생성·이동·Runner 적중·장애물 적중·수명 만료·Late Join을 수동 확인한다. 이 환경에서 실행할 수 없으면 절차와 미검증 항목을 기록한다.

## 다른 활성 작업과 겹치는 부분

- `W-20260820-006-match-progression-transition-policy.md`는 TimeSystem·Match Progression만 예약하며, 이 작업의 Monster·Projectile·Prefab·기능 문서 경로와 겹치지 않는다.

## 범위 밖

- Stalker 감지 거리 5m, 공격 진입 사거리 2m, 추적 상태 전이
- 다른 Monster AI, ShooterWorldMonster 동작·수치, Runner 입력·UI
- `Monster Projectile.prefab`, Network Prefab table, Spawn table, Scene, Bootstrapper
- 기존 Stalker Prefab의 머티리얼·장애물 여유값 변경 내용

## 완료 조건

- Stalker가 2m 이내 Runner에게 직접 `TakeDamage` 호출 없이 초당 3회의 네트워크 투사체를 발사한다.
- 투사체는 1 피해, 8m/s, 5초 수명을 가지며 발사 Stalker에게 충돌하지 않는다.
- 기존 ShooterWorldMonster 발사와 투사체 레지스트리·아이템 파괴·충돌·Despawn 동작이 유지된다.
- Host와 Client에서 권위 있는 단일 Spawn과 동일한 복제 관찰을 확인하고, Late Join 수동 검증 절차를 기록한다.
- 집중 컴파일, `git diff --check`, 예약 범위의 Prefab 참조 검증을 통과한다.

## 실제 변경

예약 단계.

## 검증 결과

예약 단계.

## 남은 위험

- Unity Editor의 실제 Host·Client와 Late Join 실행 증거는 구현 후 별도 수동 검증이 필요하다.
