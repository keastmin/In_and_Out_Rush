# Tower and Laboratory

Status: Current

Last reviewed: 2026-08-14

## 책임

타워 생성·점유·공격·지원·버프·업그레이드와 Laboratory 상호작용·공급을 관리한다.

## 주요 진입점

- `Tower`, `TowerBuildManager`, `TowerUpgradeManager`, `TowerManager`
- Attack·Support·Center Tower 구현
- `Laboratory`, `SupplyTowerManager`, `TeleportTowerPairManager`

## 주요 연결

PlayerBuilder, PlayerRunner, Grid, Track, Resource Economy, Monster, Builder UI.

## 관련 Asset

- `Assets/03_Prefabs/Tower/`
- `Assets/03_Prefabs/Laboratory/`
- `GameWorld.unity`의 Tower Manager

## 변경 시 확인

- Fusion Spawn·Despawn과 Authority
- Grid 점유 등록·해제
- 비용 차감과 환불
- Track 확장으로 파괴되는 타워와 buff registry 정리
- prefab의 NetworkObject와 직렬화 참조

## 기술 부채

타워 종류가 하나의 디렉터리와 공용 manager에 모여 있어 공개 계약과 개별 효과 경계를 점진적으로 명확히 해야 한다.
