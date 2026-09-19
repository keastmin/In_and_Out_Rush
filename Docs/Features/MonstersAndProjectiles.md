# Monsters and Projectiles

Status: Current

Last reviewed: 2026-09-20

## Track monster spawn, traversal, and speed contract

- `TrackMonsterSpawnGroup` stores `TrackMonsterSpawnType` (`Normal`, `ElitePredator`, `EliteWalker`, or `Boss`) and a spawn-unit count. Prefab selection is centralized on `TrackMonsterWaveSpawnTable`, which owns Normal Small/Medium/Large, both Elite, Boss, and dedicated internalized references.
- One Normal unit expands in the exact order Small, Small, Small, Medium, Large. One special unit spawns one matching prefab. Existing wave timings, repeat values, and unit counts are preserved and the migrated groups are `ElitePredator`. Missing wave data falls back to four Normal units (20 monsters). Internalization spawns exactly the requested count from its dedicated prefab.
- On Stage 3, reaching the first line endpoint teleports the authoritative `NetworkTransform` to the second line start. Normal and Boss monsters damage the Runner once and despawn only at the second endpoint. Elite Predator and Elite Walker instead teleport to the first line start and repeat without endpoint damage. All live types, including Elites, remain eligible for round-end settlement damage and despawn. Internalized monsters keep their no-damage natural completion rule.
- A valid Territory is resolved at spawn or lazily after initialization. Any track monster outside it receives a 1.5x movement multiplier, including Elite, Boss, and internalized monsters. Round 9 adds a one-time permanent 1.5x multiplier to current and future track monsters; together these equal 2.25x before the existing strengthening multiplier, and all factors compose multiplicatively.
- Spawn, Transform movement, path transitions, endpoint damage, settlement, permanent speed mutation, and Despawn are State Authority-only. Track monster prefabs have no Rigidbody or NetworkRigidbody3D; clients observe replicated NetworkObject/NetworkTransform results. Their Body Collider is an enabled trigger on the Monster layer.

## Tower damage target contract

- `TrackMonster` implements `ITowerDamagedMonster` with spawn priority and `TakeTowerDamage`. Tower target scans find the interface on a detected Collider's parent and deal damage only through that contract on State Authority.
- Attack and Center Towers include trigger Colliders in their range checks. Direct fire, Blade hits, and Missile explosions resolve the parent damage target; the NetworkObject and shot visual resolve the parent/root and child Collider respectively. The shared `Monster.prefab` and World Monster Rigidbody path remain unchanged.

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

## World Monster 시간별 분포와 체력

- 초기 배치는 기존 Spawn Table 수량·반경, 원점 중심 XZ 원과 Y=0, Territory 제외 및 SandTomb 추가 제한을 유지한다. 면적당 밀도는 정규화 거리 t에 대해 `1+2t`로 증가하여 가장자리가 중심의 3배다. `WorldMonsterPopulationPolicy`가 방사 누적분포 `(3t²+4t³)/7`을 역산한다.
- `StageBootstrapper.YOU`가 `InitializeStageTime(TimeSystem)`으로 시간을 주입한다. State Authority는 `TimeSystem.ElapsedTime`을 기준으로 스트리밍 refresh 전에 이벤트를 한 번 처리한다. 정비 시간도 포함되며 개별 라운드마다 다시 시작하지 않는다.
- 900초에는 전체 record 중 그 순간 비활성인 생존 개체만 검사한다. 현재 XZ 거리/그룹 생성 반경을 0~1로 제한한 t에 대해 `1-1/(1+2t)` 확률로 영구 제거한다. 가장자리 제거율은 약 2/3이며 활성 개체는 유지한다. 재배치·보충·이후 비활성 전환에 대한 소급 제거는 없다. 균등화는 기대 밀도이며 활성 개체, 이동, Territory·기존 사망으로 인해 전체의 정확한 균등 배치는 보장하지 않는다.
- 1500초에는 모든 생존 record의 현재 체력만 0.25배로 변경한다. 최대 체력과 다른 스탯은 유지한다. 활성 개체는 Networked Health, 비활성 개체는 저장 체력을 변경한다. 아직 활성화되지 않은 개체는 프리팹 기본 체력을 사용한다.
- 스트리밍 Despawn 직전에 현재 체력을 기록하고, 다음 Spawn의 기본 체력 초기화 이후 권위 전용 `Monster.TryRestoreCurrentHealth`로 복원한다. Chunk 왕복으로 체력이 회복되거나 약화가 반복되지 않는다. 사망·제거 record는 다시 생성하지 않는다.
- 새 순수 정책 NUnit 11개 테스트가 독립 .NET 실행에서 통과했다. Monster Logic 및 전체 Assembly-CSharp 직접 C# 컴파일은 오류 0개다. Unity import/Fusion weaving과 실제 Host·Client·Late Join 동작은 이번 작업에서 실행하지 않았으며 수동 검증이 필요하다.

## Rafflesia 패턴 공격과 영구 무력화

- `ShooterWorldMonster`의 기존 조준 단발 사격을 월드 축 기준 패턴으로 교체했다. 기존 타입·GUID·생성 수량·분포와 기본 체력 10을 유지한다.
- 시야 반경 기본 10 안의 살아 있는 Runner를 차폐 검사 없이 감지한다. Territory·활성 Sanctuary 안의 Runner는 제외한다. 감지 즉시 8각부터 발사하며 대상 이탈·안전지대 진입·사망 시 즉시 중단하고 재감지 시 8각부터 재시작한다.
- 8각 8발 1회 → 십자 4발씩 4회 → X자 4발씩 4회 → 회전 2발씩 8회 → 반복한다. 십자·X자 간격 0.25초, 회전 간격 0.15초, 각 패턴 마지막 발사 후 다음 패턴까지 1.5초다. 이상적인 패턴 시작 시각은 0, 1.5, 3.75, 6, 8.55초이며 실제 발사는 Fusion tick 단위다.
- 회전은 월드 왼쪽(-X)과 오른쪽(+X)에서 동시에 시작하고 위(+Y)에서 내려다본 시계방향으로 22.5도씩 회전한다. 각각 8발, 총 16개 방향으로 직진하며 157.5도가 마지막 발사다. 180도 끝점에서 추가 발사하지 않는다.
- 러너 접촉 또는 체력 소진은 Networked `IsDisabled`를 영구 설정하고 어두운 몸체를 남긴다. 접촉 피해·이동 차단·넉백은 없다. 체력 0의 복원은 라플라시아만 허용한다.
- 청크 비활성화·재활성화에도 위치·체력·무력화 상태를 보존하며 비활성 위치 이동을 생략한다. 무력화된 몸체는 영구 유지 계약에 따라 900초 비활성 인구 정리에서도 보존한다. 정상 개체 및 다른 몬스터의 기존 인구 정책은 유지한다.
- 중심점이 Territory에 포함되면 접촉·무력화 여부와 무관하게 소멸한다. 활성 이벤트와 비활성 record를 `WorldMonsterSpawnSystem.CaptureRecord`로 통합하고 record 종료를 먼저 기록해 중복 보상을 막는다.
- 토큰 연결부는 State Authority의 `WorldMonsterSpawnSystem.RafflesiaTokenRewardRequested(int recordId, Vector3 position, int amount)` 이벤트다. amount는 2이고 ID는 현재 Stage 기록 내에서 안정적이다. 향후 재화 소비자가 시스템 수명에 맞춰 구독·해제한다. 현재 실제 잔액·드랍 아이템·Client UI·보상 저장은 구현하지 않는다.

### 인스펙터와 외형 편집

- `Assets/03_Prefabs/Field/Rafflesia.prefab`: Detection Radius=10, Projectile Damage=1, Speed=1, Size=1(원본 프리팹 배율), Lifetime=5초, Maximum Range=5m. 인스펙터에서 조절 가능하며 Muzzle은 몸체 중심 높이의 발사 원점이다.
- Body Renderers에 색상을 바꿀 Renderer를 연결한다. Disabled Color 기본은 어두운 회색이며 Disabled Material은 선택 사항이다. 외형 교체 시 Renderer 참조도 갱신한다. 공유 Material asset은 수정하지 않는다.
- 작업자가 외형을 지름 3타일 원형으로 편집하고 Body의 CapsuleCollider 두 개를 같은 외형 크기에 맞춘다. 첫 번째는 접촉용 Trigger, 두 번째는 Trigger를 무시하는 기존 산탄총의 검색·시선 판정용이다. 두 번째의 Exclude Layers=Everything, Layer Override Priority=64을 유지하여 물리 이동을 막지 않게 한다. Layer는 기존 Monster(6)를 유지한다. Collider 종류를 바꿀 때도 이 두 역할을 유지한다.
- 투사체는 수명 또는 최대 이동거리 도달 시 소멸한다. 발사자와 같은 라플라시아의 탄끼리는 충돌을 무시해 동시 발사 직후 상쇄되지 않게 한다. 다른 물체 충돌 시 소멸하고 Runner에게만 피해를 준다. 충돌 순간 영역·활성 안식처의 Runner는 보호한다. 발사자 무력화·영역화·청크 Despawn은 이미 발사한 탄을 제거하지 않는다.
- `MonsterProjectile.Initialize`의 최대 사거리·크기·보호 판정·동일 발사자 탄 예외는 선택 매개변수다. Stalker는 기존 수명·충돌 동작과 배율 1을 유지한다. 크기는 Networked 상태로 복제한다.
- 투사체 초기화 시 Rigidbody 위치·회전을 발사 Transform에 먼저 맞춘다. 물리 자동 동기화가 꺼진 환경에서 이전 물리 위치로 사거리를 계산해 일부 방향의 탄이 즉시 소멸하는 것을 방지한다. 독립 Unity 재현 검사에서 3개 위치·16방향의 48개 사례를 확인했다.

### 검증과 수동 실행 절차

- 실제 순수 소스와 NUnit 직접 실행: 20개 사례 통과(기존 인구 정책 13, 신규 패턴 7). Monster Logic 및 전체 Assembly-CSharp 직접 C# 컴파일 오류 0. 직렬화 필드 등의 경고는 남아 있다.
- Prefab local fileID 고유성·참조, 외부 GUID, 신규 .meta 및 git diff --check 확인. Unity import/Fusion weaving과 실제 Host·Client·Late Join은 미실행이므로 Peer 동등성은 미검증이다.
- Host 로컬과 Client Input Authority Runner 각각 시야 진입→이탈/영역·안식처 진입→재진입을 시험한다. 첫 8각 즉시 발사, 이탈 즉시 추가 발사 중단, 재진입 8각 시작과 양 Peer의 탄 개수·크기를 확인한다.
- 접촉·총·피해 스킬로 각각 무력화하여 통과 가능, 접촉 피해 없음, 색상 변경과 영구 공격 중단을 확인한다. 0체력 및 접촉 무력화 각각 청크 밖 왕복·Late Join 후 동일 상태·위치인지 확인한다.
- 정상·무력화·비활성 개체를 영역화하여 중심점 소멸과 권위 측 amount=2 이벤트가 record당 1회인지 확인한다. 반복 영역 확장·Client의 중복 호출과 비영역화 총·스킬 처치 보상이 없어야 한다.
- 이미 발사한 탄 유지, 같은 라플라시아 탄끼리 통과, 다른 물체 충돌 소멸, 안전지대 보호, 수명·거리 중 먼저 도달하는 제한과 인스펙터 크기의 Host·Client 일치를 확인한다.
