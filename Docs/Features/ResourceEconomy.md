# Resource Economy

Status: Current

Last reviewed: 2026-08-20

## 책임

공유 광물·가스 네트워크 상태와 비용 충분성·차감, UI 갱신 연결을 관리한다.

## 주요 진입점

- `Assets/02_Scripts/Resource/Network/ResourceSystem.cs`
- `Assets/02_Scripts/Tower/Cost.cs`
- `ProjectIO.ResourceEconomy.ResourcePaymentPolicy`
- `ProjectIO.ResourceEconomy.Adapters.Fusion.ResourcePaymentFusionAdapter`
- `ResourceInfoUI`, `ResourceView`

## 상태와 권한

`ResourceSystem`의 Networked Mineral·Gas가 기준 상태다. `ResourcePaymentPolicy`는 현재 잔액과 비용에서 차감 후 잔액만 계산하는 Unity·Fusion 비의존 순수 로직이다. `ResourcePaymentFusionAdapter`는 모든 Peer가 복제된 잔액으로 충분성을 확인하게 하고, NetworkObject 유효성과 State Authority를 확인한 뒤에만 Networked 값을 기록한다.

## 주요 소비자

PlayerBuilder 타워 건설·이동·판매, Laboratory 공급과 업그레이드, Stage UI.

첫 전환 Slice에서는 `TowerBuildManager`의 건설 충분성 확인과 authoritative 차감만 새 Adapter를 사용한다. 이동·판매·업그레이드·속성·Laboratory 소비자는 기존 `ResourceSystem` 경로를 유지한다.

## 관련 Asset

- `Core.prefab`
- `GamePresentation.unity`의 Resource UI

## 변경 시 확인

- 비용 차감의 권한과 원자성
- 순수 정책이 음수 비용을 거부하고 부족한 어느 한 자원도 부분 차감하지 않는지
- 타워 건설 검증 실패와 권한 없는 Peer가 Networked 잔액을 변경하지 않는지
- Host와 Client UI 갱신
- 테스트 자원 지급이 Development/Test Mode에만 한정되는지

## 기술 부채

동일한 이름의 Local·Network ResourceSystem이 존재하므로 문서와 코드에서 namespace를 명확히 해야 한다.
