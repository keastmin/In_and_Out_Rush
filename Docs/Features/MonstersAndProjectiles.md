# Monsters and Projectiles

Status: Current

Last reviewed: 2026-08-28

## 책임

월드·트랙 몬스터의 Spawn, 이동, 전투, 정착과 몬스터·플레이어 투사체 수명을 관리한다.

## 주요 진입점

- `Monster`, `WorldMonster`, `TrackMonster`
- `WorldMonsterSpawnSystem`, `TrackMonsterSpawnSystem`
- `MonsterProjectile`, `MonsterProjectileRegistry`, `Projectile`
- World·Track monster spawn table
- `Assets/02_Scripts/Features/Monster/Logic/WorldMonsterChunkIndex.cs`
- `Assets/02_Scripts/Features/Monster/Logic/WorldMonsterSpawnCandidatePolicy.cs`
- `Assets/02_Scripts/Features/Monster/UseCases/SelectWorldMonsterSpawnCandidatesUseCase.cs`
- `Assets/02_Scripts/Features/Monster/Adapters/Unity/WorldObstacleBoundsIndex.cs`

## 주요 연결

PlayerRunner, Track, Territory, Sacred Zone, Sanctuary, Tower, Stage round events, and `InfiniteGridObstacleSpawner` world obstacles.

## 관련 Asset

Monster와 Projectile prefab, `GameWorld.unity`의 spawn parent와 systems, `WorldObstacle` instances spawned by `InfiniteGridObstacleSpawner`.

## 변경 시 확인

- Host Spawn과 State Authority
- Client 표시와 Late Join
- World monster obstacle-list injection after authoritative obstacle spawn
- Patrol, slide, and chase paths avoiding the occupied bounds of spawned rocks
- World Monster patrol, Strider slide, and Stalker chase paths also reject Territory and active Sanctuary crossings plus generated resource Collider overlaps. Gigantia may leave Territory through a newly selected outside target, but it never selects or re-enters a Territory/Sanctuary path.
- Wave 종료·정착·내재화 시 중복 Spawn
- Despawn 후 projectile registry와 이벤트 정리
- World monster record는 고정 pivot Chunk로 색인하며 refresh에서는 플레이어
  주변 후보와 현재 활성 record만 처리하는지
- World monster Spawn 후보는 source 순서와 refresh 예산을 지키고 같은 record
  ID를 한 번만 선택하며, 실제 `Runner.Spawn`은 Legacy `SpawnRecord`만 호출하는지
- 월드 장애물 bounds cache는 파괴된 장애물을 무시하고 기존 segment/bounds
  판정과 같은 결과를 내는지

## SandTomb 벌레지옥

- State Authority가 activation radius 진입으로 SandTomb을 활성화하고, Networked `TickTimer` 4초 뒤 폭발과 Despawn을 결정한다.
- 활성 중 sucked-into radius에 들어온 Runner는 Territory 확장 사선을 진입 위치에서 일시 정지한 채 흡입된다. 반경 이탈, 폭발, 또는 Despawn은 해당 위치와 현재 Runner 위치를 연결한 뒤 사선 기록을 재개한다.
- 폭발 순간 sucked-into radius 안이고 영토 보호 밖인 Runner는 최대 체력 10%와 현재 체력 30%의 합만큼 피해를 받는다.

## 기술 부채

Local Monster 계층과 Network Monster 계층이 병존한다. 어떤 경로가 활성인지 소비자별로 확인해야 한다.
Network World Monster의 Chunk 후보 선택은 `ProjectIO.Monsters` 순수 assembly가,
Unity 장애물 bounds broadphase는 Legacy assembly의 adapter가 담당한다. Monster
AI tick cadence와 전투 규칙은 Legacy Network Monster가 계속 소유한다.

World Monster의 이동 안전성 판정은 Legacy `WorldMonster`가 State Authority에서 수행한다. 바위는 `WorldObstacleBoundsIndex`, 자원은 Physics query로 생성된 `ResourceVisible` Collider를 확인하며, Territory·Sanctuary는 일정 간격의 경로 표본으로 통과를 막는다. Client는 기존 NetworkObject Transform 복제만 관찰한다.

일반 `allowTerritoryExit=false` WorldMonster patrol, Strider slide, Stalker chase는
`IsMovementPathBlocked`의 endpoint sample이 Territory와 Sanctuary를 모두 판정하므로,
직전 endpoint safe-zone 중복 query를 수행하지 않는다. `allowTerritoryExit` 특수 규칙을
쓰는 Centipede와 patrol candidate 경로는 이 통합 범위 밖이며 기존 판정을 유지한다.

## SandTomb range presentation

SandTomb presents the activation radius as an inner transparent disk and the sucked-into radius as a transparent outer annulus. Both layers reuse the existing 64-segment runtime mesh approach, while the existing activation, suction, damage, timer, and Despawn rules remain unchanged.

## Stalker projectile attack

`Stalker` keeps its serialized sensing and attack-entry ranges, chase state, and three-attacks-per-second timer. It enters attack at `Attack Range` and remains in attack until the target exceeds the larger `Chase Resume Range`; the Stalker prefab uses 5m and 5.5m respectively to avoid range-boundary state chatter. On each attack tick, its State Authority spawns the shared `MonsterProjectile` from the optional `Stalker.prefab` `Muzzle` child, or the equivalent local `(0, 1, 0.5)` fallback position when that reference is absent, with 1 damage, 8m/s speed, and a 5-second lifetime; it no longer calls `IDamageable.TakeDamage` directly.

`MonsterProjectile.Initialize` accepts a `Monster` owner, so both Stalker and `ShooterWorldMonster` use the same network projectile. State Authority ignores only the firing monster's colliders, resolves Runner damage and despawn, while other peers observe the replicated NetworkObject and NetworkTransform. Host/Client runtime and Late Join evidence remain a required manual check.

## WorldMonster 권위 넉백

- `WorldMonster.TryApplyKnockback`은 State Authority에서만 성공하는 진입점이다. 방향, 총 거리와 지속시간을 Networked timer·velocity로 저장하고 기존 NetworkRigidbody 이동을 사용하므로 다른 Peer는 복제 위치만 관찰한다.
- 새 넉백은 진행 중 넉백을 교체한다. 넉백 tick은 일반 AI 이동보다 먼저 실행하며 바위, 생성 자원 Collider, Territory와 활성 Sanctuary를 검사하는 기존 `IsMovementPathBlocked`에서 경로가 막히면 즉시 중단한다.
- 기본 이동형 WorldMonster는 넉백을 허용한다. SandTomb, Rafflesia 구현 타입인 `ShooterWorldMonster`, Gigantia 구현 타입인 `Centipede`는 넉백에 면역이지만 피해는 정상 적용된다.
- Runner 산탄총의 3m 이내 성공 적중은 1m/0.2초 넉백을 요청한다. 권위 없는 Peer 호출, 0 이하 거리·지속시간과 유효하지 않은 방향은 상태를 바꾸지 않는다.
- Knockback timer와 velocity는 현재 상태로만 Late Join에 복원되며 Monster Despawn과 함께 제거된다. 일회성 target result는 Runner 소유자의 presentation event이며 Monster가 저장하지 않는다.

## World Monster Spawn 후보 선택 slice

`WorldMonsterSpawnSystem`은 주변 Chunk record의 destroyed, active monster,
Territory 포함, active Chunk 범위 상태를 순수 `WorldMonsterSpawnCandidate`로
변환한다. `SelectWorldMonsterSpawnCandidatesUseCase`는 기존 source 순서를
유지하면서 refresh 예산까지 안정 record ID를 중복 없이 선택하고 source
index만 반환한다.

선택된 index의 실제 NetworkObject 생성은 기존 State Authority 경로의
`WorldMonsterSpawnSystem.SpawnRecord`가 계속 담당한다. UseCase는 Fusion,
Unity object, Territory, dormant 이동과 Spawn/Despawn 수명주기를 소유하지 않는다.

## 검증

- 실제 순수 Logic·UseCase와 NUnit source를 링크한 검증에서 기존 Chunk 3개와
  신규 후보 선택 5개 테스트가 8/8 통과했다.
- 순수 Logic, UseCase, 수정된 Legacy `WorldMonsterSpawnSystem` 독립 컴파일은
  오류 0개였다. Legacy 컴파일에는 기존 미사용·직렬화 필드 경고 2개가 남았다.
- 원본 Unity Editor가 프로젝트를 열고 있어 별도 batchmode import는 수행하지
  않았다. 작업자는 Unity 테스트와 제시된 Host·Client Authority·중복 Spawn
  확인을 완료했으며 별도 이상을 보고하지 않았다.
