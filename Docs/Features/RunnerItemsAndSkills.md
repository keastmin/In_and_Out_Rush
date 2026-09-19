# Runner Items and Skills

Status: Current

Last reviewed: 2026-09-20

## 책임

Runner 아이템 슬롯, 소비 전략, 선택 스킬과 각 아이템의 네트워크 효과를 관리한다.

## 주요 진입점

- `RunnerItemInventory`, `RunnerItemConsumer`, `IItemConsumptionStrategy`
- `RunnerSkillCaster`, `RunnerSkillType`
- BarrierWave, ElectricGrenadeProjectile, IncineratorDrone, Biodecomposition handlers
- Legacy `Assets/02_Scripts/Obtainable/`

## 주요 연결

보급 아이템 ID 7000–7004는 각각 Lifeline, Barrier, Incinerator, ElectricGrenade, BiodecompositionDevice에 대응한다. `PlayerRunner.TryReceiveSupply` → `TryReceiveItemSupply`가 지정 슬롯에만 1개 추가하며 무작위 Item 지급은 제거했다. 기존 `Item` Obtainable도 `RunnerItemType`을 포함한다. 소지 한도에 도달하면 false를 반환하여 보급 타워가 해당 항목을 보존한다.

슬롯 수량은 `PlayerRunner.ItemCounts`의 NetworkArray와 OnChangedRender로 표시한다. 초기 상태·변경 상태를 복제해 Input Authority HUD에 반영하며, 수령 결과는 Runner 메시지 팝업으로 전달한다. 기존 초기 소지량, 최대 소지량, 아이템 효과 및 사용 입력은 유지한다.

현재 Runner 프리팹은 생명선 3/3, 방벽 99/99로 시작하여 시작 직후 추가 수령이 거부될 수 있다. 이는 현재 프리팹 설정이며 기획상 권장 수량을 의미하지 않는다. 아이템 사용 후에도 수령되지 않는 경우의 F Raycast 판정과 미검증 사항은 [연구소 보급 질의응답](../개발%20일지/2026-09-20-연구소-보급-질의응답.md)에 기록한다.

PlayerRunner, Laboratory 공급, Runner UI, Fusion Spawn, Monster와 Projectile.

## 관련 Asset

Runner prefab의 item definition과 각 NetworkObject prefab, Runner item slot UI.

## 변경 시 확인

- 아이템 개수 차감과 실제 효과의 Authority
- Spawn된 효과의 Late Join 필요 여부
- 취소·실패 시 소비 일관성
- Despawn과 이벤트 정리

## 기술 부채

Legacy Obtainable의 스킬/무기 경로는 유지된다. 개별 아이템 보급은 RunnerSupply의 상품 ID와 실제 RunnerItemInventory 소비 경로를 기준으로 한다.
