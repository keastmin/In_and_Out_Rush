# W-20260918-001 라플라시아 패턴 공격과 영구 무력화

Status: Reserved

## 동기화 기준

- Base Commit: 749b894d1fb53a22d36cd243fc473725f01d7109
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

현재 작업자와 Codex

## 기능

Monster와 Projectile / 기존 Rafflesia의 ShooterWorldMonster 동작 교체

## 목표

- 고정형 라플라시아. 생성 수량·위치 분포와 체력 10은 유지한다. 지름 3타일 원형 외형은 작업자가 편집하고 접촉 Collider를 외형에 맞춰 조절한다. 몸체는 이동을 막지 않는다.
- 시야 반경 기본 10, 장애물 차폐 무시. 영역·활성 안식처 안의 러너는 대상에서 제외한다. 유효 대상 감지 즉시 월드 축 기준 8각부터 시작한다. 대상을 잃으면 즉시 중단하며 재감지 시 8각부터 재시작한다.
- 8각 동시 8발 1회 → 십자 동시 4발 4회 → X자 동시 4발 4회 → 회전 → 반복. 십자·X자 반복 간격 0.25초, 각 패턴 마지막 발사부터 다음 패턴까지 1.5초.
- 회전은 좌우에서 동시에 1발씩, 시계방향 22.5도 간격으로 0.15초마다 8회 발사하여 총 16발. 시작 방향 포함, 각 시작점에서 157.5도 위치가 마지막 발사이며 180도 끝점에서는 추가 발사하지 않는다. 각 투사체는 직진한다.
- 라플라시아 투사체 기본 피해량 1, 속도 1, 크기 배율 1, 수명 5초, 최대 이동거리 5. 인스펙터에서 조절한다. 수명 또는 사거리 도달 시 소멸. 발사자 외 물체 충돌 시 소멸하고 러너에게만 피해를 주며 기존 영역·안식처 보호를 유지한다.
- 러너 접촉은 피해 없이 영구 무력화. 총·피해 스킬 등 기존 피해 경로에서 체력이 0이 되어도 소멸 대신 영구 무력화된 몸체를 남긴다. 무력화 색상·표현 대상은 인스펙터에서 설정하며 양 Peer에서 어둡게 표시한다. 이미 발사한 투사체는 유지한다.
- 중심점 영역화 시 접촉·무력화 여부와 무관하게 소멸하며 토큰 2개 보상 연결부를 정확히 한 번 호출한다. 실제 재화·획득 UI·드랍 아이템은 구현하지 않는다.
- 청크 비활성화·재활성화에도 고정 위치와 무력화 상태를 보존하고, 비활성 개체의 영역화도 보상 연결부에서 누락·중복되지 않게 한다.

## 읽을 문서와 Skill

- AGENTS.md, Docs/Work/README.md, Docs/PROJECT_MAP.md
- Docs/Features/MonstersAndProjectiles.md
- manage-feature-work, photon-fusion-feature, replace-existing-feature
- graphify: 기존 그래프 없음 확인. 이번 예약 조사는 기능 문서와 실제 코드·직렬화 참조를 직접 확인했다.

## 예상 수정 코드

- Assets/02_Scripts/Monster/ShooterWorldMonster.cs: 기존 단발 조준 공격 교체, 감지·접촉·무력화·표현.
- Assets/02_Scripts/Monster/Monster.cs: 체력 소진과 영역화 원인을 분리하는 최소 확장 지점. 다른 몬스터 기본 동작 유지.
- Assets/02_Scripts/Monster/WorldMonsterSpawnSystem.cs: 고정 위치·무력화 상태 저장/복원, 활성·비활성 영역화의 단일 보상 경로.
- Assets/02_Scripts/Projectile/MonsterProjectile.cs: 선택적 크기·최대 사거리 설정. 기존 Stalker 호출 기본값과 수명 유지.
- Assets/02_Scripts/Features/Monster/Logic/RafflesiaAttackPattern.cs 및 .meta: 순수 패턴 순서·발사 간격·방향 계산.
- Assets/02_Scripts/Features/Monster/Tests/RafflesiaAttackPatternTests.cs 및 .meta: 패턴 수량·방향·마지막 발사 후 대기·재시작 검증.
- Docs/Features/MonstersAndProjectiles.md: 최종 계약·인스펙터 설정·수동 검증 기록.
- 본 작업 문서. 완료 요청 후 동일 파일명을 Docs/Work/Completed/로 이동.

## 예약 Scene·Prefab·Data Asset

- Assets/03_Prefabs/Field/Rafflesia.prefab: 발사·표현 기본값, 기존 Collider의 비차단 접촉 설정과 참조. 기존 GUID 보존.
- Scene, Spawn Table, 공용 투사체 Prefab, 공용 재질은 수정하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- Monster의 체력 소진과 영역화 확장 지점은 원인별로 분리하고 기존 파생형 호출을 조사한다.
- MonsterProjectile 초기화 확장은 기존 호출에 호환되는 기본값을 제공한다.
- 토큰 연결부는 WorldMonsterSpawnSystem에서 권위 전용 영역화 보상 알림으로 제공한다. 기존 생성 시스템이 개체에 직접 연결하며 전역 검색·Singleton 기반 신규 의존성 전달은 도입하지 않는다.
- Bootstrapper 변경 없음. 새로운 공용 파일이 필요하면 수정 전에 예약을 갱신한다.

## 네트워크·Peer 동등성

- Runner 이동·무기·스킬 입력은 기존 Input Authority 및 서버 전달 경로를 유지한다. 접촉, 감지, 공격 타이밍, 피해, 무력화, 보상 알림, Spawn/Despawn은 State Authority에서만 결정한다.
- 무력화는 Networked 지속 상태로 복제하고 Host 로컬과 Client 모두 같은 색상 표현을 적용한다. 투사체는 기존 네트워크 Spawn·위치 복제 경로를 사용한다. 신규 입력·미리보기·HUD는 없다.
- 청크 기록에는 무력화 상태를 별도로 보존하고 체력 0을 일반 사망 개체로 오인해 삭제하지 않게 한다. Late Join은 현재 무력화 상태를 복원한다. 이미 발사한 탄은 발사자의 무력화·영역화 후에도 자체 수명을 따른다.
- Host 로컬 및 Client Input Authority Runner 각각 접근→공격 시작→시야 이탈/안전지대 진입→즉시 공격 중단→재감지 8각 시작을 확인한다.
- 두 Peer 각각 접촉과 총·피해 스킬로 무력화하고 접촉 피해 없음, 어두운 몸체 유지, 이후 공격 없음, 청크 왕복 후 상태·위치 유지, Late Join 표현을 확인한다.
- 정상/무력화/비활성 개체를 영역화하고 중심점 판정, 소멸, 권위 측 토큰 2개 알림 1회 및 Client 중복 호출 없음을 확인한다. 이미 발사한 탄의 수명·사거리·충돌·안전지대 보호도 확인한다.
- 권한 없는 상태 변경 거부, 반복 피해·접촉·영역 확장의 중복 보상 방지, Despawn·Scene 종료 이벤트 정리를 확인한다. 실제 Host·Client 실행을 못 하면 미검증으로 남기고 위 절차를 작업자에게 제공한다.

## 다른 활성 작업과 겹치는 부분

없음. CheckStart 동기화 완료, Active에는 README.md만 존재한다.

## 범위 밖

- 실제 토큰 재화 시스템, 드랍 아이템·획득 UI 구현.
- 외형 제작, 전체 몬스터 리팩토링, Territory 내부 구조 변경.
- 기존 생성 수량·분포·시간별 인구 정책 변경.

## 완료 조건

- 기존 ShooterWorldMonster 타입과 Prefab GUID를 유지하되 단발 조준 사격은 요청한 패턴으로 완전히 교체한다. 두 공격 경로가 병행 실행되지 않는다.
- 패턴 순서·수량·방향·시간과 취소/재시작 집중 테스트 통과.
- 영구 무력화, 비차단 접촉, 영역화 보상 연결부, 청크 복원, Host·Client 표현 검증.
- 가능한 프로젝트 컴파일, Prefab 참조 점검, git diff --check와 변경 범위 확인. 실행하지 못한 검증은 구분한다.
- 롤백은 본 작업의 명시된 파일 변경을 이전 버전으로 복원하고 기존 Prefab GUID·단일 소비 경로를 유지하는 방식이며, 자동 Git reset/revert는 수행하지 않는다.

## 실제 변경

예약 문서만 작성. 구현 시작 전 작업자의 예약 진행 요청 대기.

## 검증 결과

- CheckStart: AHEAD=0, BEHIND=0, READY_TO_CHECK_CONFLICTS.
- ShooterWorldMonster 스크립트 GUID의 Prefab 소비자는 Rafflesia.prefab 하나임을 확인했다.
- 구현·런타임 테스트는 아직 실행하지 않았다.

## 남은 위험

- 공용 Monster의 기존 종료 경로와 비활성 Spawn record의 영역화 경로가 다르므로 원인 분리 및 중복 보상 검증 필요.
- Trigger 전환 후 기존 무기·스킬 적중 query가 외형 Collider를 감지하는지 실제 확인 필요. 추가 공용 코드 변경이 필요하면 범위 확장 절차를 따른다.
- 외형과 접촉 크기는 작업자가 최종 편집하며 지름 3타일 일치 여부를 수동 확인한다.
