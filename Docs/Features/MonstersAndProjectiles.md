# Monsters and Projectiles

Status: Current

Last reviewed: 2026-08-14

## 책임

월드·트랙 몬스터의 Spawn, 이동, 전투, 정착과 몬스터·플레이어 투사체 수명을 관리한다.

## 주요 진입점

- `Monster`, `WorldMonster`, `TrackMonster`
- `WorldMonsterSpawnSystem`, `TrackMonsterSpawnSystem`
- `MonsterProjectile`, `MonsterProjectileRegistry`, `Projectile`
- World·Track monster spawn table

## 주요 연결

PlayerRunner, Track, Territory, Sacred Zone, Sanctuary, Tower, Stage round events, and `InfiniteGridObstacleSpawner` world obstacles.

## 관련 Asset

Monster와 Projectile prefab, `GameWorld.unity`의 spawn parent와 systems, `WorldObstacle` instances spawned by `InfiniteGridObstacleSpawner`.

## 변경 시 확인

- Host Spawn과 State Authority
- Client 표시와 Late Join
- World monster obstacle-list injection after authoritative obstacle spawn
- Patrol, slide, and chase paths avoiding the occupied bounds of spawned rocks
- Wave 종료·정착·내재화 시 중복 Spawn
- Despawn 후 projectile registry와 이벤트 정리

## 기술 부채

Local Monster 계층과 Network Monster 계층이 병존한다. 어떤 경로가 활성인지 소비자별로 확인해야 한다.
