# W-20260821-007 World Monster 이동 안전성

Status: Reserved

## 동기화 기준

- Base Commit: 52cbea9c59ea57b988d0f6347b4bae104c67d974
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Monster와 Projectile, Territory, Resource Spawn

## 목표

월드 몬스터의 랜덤 목적지와 이동 구간이 Territory·활성 Sanctuary·월드 바위·생성된 자원 Collider를 침범하거나 통과하지 않게 한다. Gigantia는 Territory 안에 있을 때 정지하지 않고, 즉시 Territory 밖의 안전한 목적지를 다시 선정해 이탈한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/MonstersAndProjectiles.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Monster/WorldMonster.cs`
- `Assets/02_Scripts/Monster/Centipede.cs`
- `Assets/02_Scripts/Monster/Strider.cs`
- `Assets/02_Scripts/Monster/Stalker.cs`
- `Assets/02_Scripts/Monster/Monster.cs`
- `Docs/Features/MonstersAndProjectiles.md`
- `Docs/Work/Active/W-20260821-007-world-monster-movement-safety.md`

## 예약 Scene·Prefab·Data Asset

없음. 기존 생성 자원의 Collider와 기존 월드 장애물 bounds를 런타임에서 읽으며, Scene·Prefab·ScriptableObject·Network Prefab table은 변경하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

외부 계약 변경은 없다. `Monster`에 Territory와 활성 Sanctuary를 구분해 파생 월드 몬스터가 읽을 수 있는 보호 판정을 추가한다. `WorldMonsterSpawnSystem`의 Spawn·의존성 주입과 `ResourceSpawnSystem`의 자원 생성 계약은 바꾸지 않는다.

## 네트워크·Peer 동등성

- 플레이어 입력·RPC·새 Networked 상태는 없다.
- 기존처럼 World Monster의 State Authority만 FixedUpdate에서 목적지 선정·탈출 결정·Rigidbody velocity 변경을 수행한다.
- Host와 Client는 기존 NetworkObject/NetworkTransform 복제로 동일한 위치·이동 결과를 관찰한다. Client는 목적지를 독자적으로 선정하거나 authoritative 상태를 변경하지 않는다.
- Late Join은 기존 복제 Transform을 사용하며, 새 지속 상태나 정리 경로는 추가하지 않는다.
- 실제 Host·Client 동시 실행이 불가하면 State Authority 단일 실행과 Client 관찰 절차를 검증 결과에 분리해 기록한다.

## 다른 활성 작업과 겹치는 부분

- `W-20260820-006-match-progression-transition-policy.md`는 Match Progression 순수 정책과 `TimeSystem`만 예약한다. Monster·Territory·Resource 이동 코드, Scene, Prefab, 공용 계약은 겹치지 않는다.

## 범위 밖

- World Monster Spawn 후보·Chunk streaming·Dormant 이동
- 자원 배치·수집·AOI
- Track Monster 이동, Projectile, Player Runner 이동
- Scene, Prefab, Layer, Physics 설정 및 Network schema 변경

## 완료 조건

- 공통 순찰과 Strider 슬라이드의 목적지·이동 구간이 Territory/활성 Sanctuary, 바위, 생성 자원 Collider와 겹치거나 통과하지 않는다.
- Gigantia의 사행 오프셋을 포함한 실제 이동 구간도 동일하게 검사한다.
- Gigantia가 Territory 내부에서 발견되면 멈춰 있지 않고, Territory 밖·장애물/자원 비충돌 목적지로 재선정해 빠져나간다.
- 목적지 또는 다음 이동 구간이 막히면 정지 상태에 고착하지 않고 다음 simulation tick에서 재선정한다.
- State Authority만 이동 결정을 내리며, 기존 Spawn·복제·Late Join 경로를 변경하지 않는다.
- 집중 컴파일/테스트, `git diff --check`, Host·Client 수동 확인 절차를 기록한다.

## 실제 변경

예약 단계.

## 검증 결과

예약 단계.

## 남은 위험

- Runtime 자원 수와 Collider 크기에 따라 목적지 탐색 재시도 비용이 늘 수 있으므로, 기존 횟수 제한 안에서만 검사하고 실패 시 다음 tick에 다시 시도한다.
- Gigantia의 Territory 이탈은 내부에서 외부로 나가는 구간만 예외로 허용한다. 외부에서 Territory 안으로 들어가는 목적지나 구간은 허용하지 않으며, 활성 Sanctuary는 이탈 중에도 통과하지 않는다.
