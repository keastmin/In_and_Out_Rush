# Tower and Laboratory

Status: Current

Last reviewed: 2026-09-20

## 책임

타워 생성·점유·공격·지원·버프·업그레이드와 Laboratory 상호작용·공급을 관리한다.

## 주요 진입점

- `ProjectIO.Construction.TowerConstructionUseCase`
- `Tower`, `TowerBuildManager`, `TowerUpgradeManager`, `TowerManager`
- Attack·Support·Center Tower 구현
- `Laboratory`, `SupplyTowerManager`, `TeleportTowerPairManager`
- `ProjectIO.RunnerSupply.RunnerSupplyNetwork`, `RunnerSupplyCatalog`, `RunnerSupplyRules`

## 건설 흐름

`TowerBuildManager`는 기존 RPC와 State Authority 검증, `Runner.Spawn`을 소유한다. Host 건설 시도는 순수 `TowerConstructionUseCase`가 Spawn, 실제 Grid 점유 검증, 특수 Tower 초기화, authoritative Resource 지불, Commit 순서로 조정한다. Spawn 이후 실패는 공통 Rollback에서 Grid 점유를 해제하고 Fusion Despawn한다.

## 주요 연결

### 개별 아이템 보급

연구소의 `RunnerSupplyNetwork`가 10칸 구매 대기열과 센터 해금 상태를 Fusion으로 복제한다. Builder의 구매 요청은 State Authority에서 요청자, 상품 ID, 가격, 자원, 해금, 대기열 버전을 검증한 뒤 비용 차감과 추가를 수행한다. Host 로컬 요청도 같은 RPC 경로와 결과 응답을 사용한다. `SupplyTowerManager`는 주입된 상태를 기존 건설 소비자에 연결하며 별도 로컬 대기열을 소유하지 않는다.

보급 ID는 스킬 1, 무기 2, 아이템 7000–7004이다. `TowerBuildManager`는 구매 대기열과 요청 적재 목록의 순서·개별 ID를 검증하고, 건설 Commit에서 성공한 적재 목록만 한 번 소비한다. 실패한 건설은 대기열을 유지한다. `SupplyTower`는 Runner의 기존 F 상호작용을 State Authority에서 처리하여 구매한 아이템만 지급한다. 소지 한도로 받지 못한 내용물은 타워에 남고, 비었을 때만 Despawn한다.

가격·아이콘·해금 센터는 `Assets/08_Data/Runner Supply Catalog.asset`에 노출한다. 아이템 가격 출처는 기획 `땅타 프로젝트 데이터 테이블 0.60v.xlsx`의 `Item!C5:D9`이며 스킬·무기 보급은 기존 50/50을 유지한다.

생명선·방벽은 센터 조건이 없고, 소각기/전류탄/생분해 장치는 각각 화염/전격/생화학 센터가 현재 존재해야 구매할 수 있다. 러너의 소지 여유는 수령 시 검사하므로 구매 가능한 상태에서도 수령은 거부될 수 있다.

구현·편집·검증 결과는 [완료 작업 기록](../Work/Completed/W-20260920-002-laboratory-item-supply-ui.md), 구매 제한과 시작 직후 수령 불가에 관한 사용자 질문은 [별도 질의응답](../개발%20일지/2026-09-20-연구소-보급-질의응답.md)을 참고한다. 실제 Host·Client 수령 검증은 미완료다.

PlayerBuilder, PlayerRunner, Grid, Track, Sanctuary, Resource Economy, Monster, Builder UI.

공격 타워와 센터타워의 기본 공격은 Monster 레이어의 Trigger Collider를 검색하고 부모 `ITowerDamagedMonster`의 우선순위로 대상을 선택한다. State Authority만 `TakeTowerDamage`를 호출하며, 자식 Collider의 부모 NetworkObject를 발사 표시와 대상 동기화에 사용한다. Blade 범위 피해와 Missile 폭발도 같은 피해 계약을 사용한다.

## 관련 Asset

- `Assets/03_Prefabs/Tower/`
- `Assets/03_Prefabs/Laboratory/`
- `GameWorld.unity`의 Tower Manager

## 변경 시 확인

- Fusion Spawn·Despawn과 Authority
- Grid 점유 등록·해제
- 비용 차감과 환불
- Track 확장으로 파괴되는 타워와 buff registry 정리
- Sanctuary 소멸로 파괴되는 타워의 Grid 점유와 Center Tower 수량 정리
- prefab의 NetworkObject와 직렬화 참조

## 기술 부채

타워 종류가 하나의 디렉터리와 공용 manager에 모여 있어 공개 계약과 개별 효과 경계를 점진적으로 명확히 해야 한다.
