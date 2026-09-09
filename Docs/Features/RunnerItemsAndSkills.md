# Runner Items and Skills

Status: Current

Last reviewed: 2026-08-14

## 책임

Runner 아이템 슬롯, 소비 전략, 선택 스킬과 각 아이템의 네트워크 효과를 관리한다.

## 주요 진입점

- `RunnerItemInventory`, `RunnerItemConsumer`, `IItemConsumptionStrategy`
- `RunnerSkillCaster`, `RunnerSkillType`
- BarrierWave, ElectricGrenadeProjectile, IncineratorDrone, Biodecomposition handlers
- Legacy `Assets/02_Scripts/Obtainable/`

## 주요 연결

PlayerRunner, Laboratory 공급, Runner UI, Fusion Spawn, Monster와 Projectile.

## 관련 Asset

Runner prefab의 item definition과 각 NetworkObject prefab, Runner item slot UI.

## 변경 시 확인

- 아이템 개수 차감과 실제 효과의 Authority
- Spawn된 효과의 Late Join 필요 여부
- 취소·실패 시 소비 일관성
- Despawn과 이벤트 정리

## 기술 부채

Legacy Obtainable 타입과 실제 Runner Item 구현의 관계가 불명확하다. 새 기능은 실제 소비 경로를 기준으로 한다.
