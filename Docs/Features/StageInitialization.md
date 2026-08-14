# Stage Initialization

Status: Current

Last reviewed: 2026-08-15

## 책임

Additive Scene에 흩어진 시스템 참조를 준비하고 플레이어·네트워크 입력을 Spawn한 뒤 각 게임 시스템을 정해진 순서로 초기화한다.

## 주요 진입점

- `Assets/02_Scripts/Stage/Network/StageBootstrapper.cs`
- `StageBootstrapper.KIM.cs`, `StageBootstrapper.YOU.cs`
- `Assets/03_Prefabs/Core.prefab`

## 주요 흐름

`GameRoot`의 Core가 생성되면 필요한 World·Presentation 참조가 준비될 때까지 기다린다. 이후 플레이어 Spawn, 카메라와 UI 초기화, 시스템 SetUp, Laboratory·Obstacle·Sanctuary 생성 순으로 진행한다.

Host/Server는 Fusion AOI cell 크기를 64로 한 번 설정한다. 매 network tick 각
player의 기존 AOI region을 지운 뒤 현재 PlayerObject 위치에 반경 128 region
하나를 다시 등록한다. Shared Mode는 Fusion 제한에 따라 cell 설정과 명시적
clear를 건너뛴다.

## 공개 연결부

- `StageBootstrapper.Instance`
- `PlayerRunner`, `PlayerBuilder`, `Grid`, `TerritorySystem`
- `LocalPlayerRunnerSpawned`, Laboratory 주입 메서드

## 관련 Asset

- `GameRoot.unity`, `Core.prefab`, `Game Scene Setup.prefab`
- World와 Presentation에 위치한 시스템·UI prefab

## 변경 시 확인

- 비활성 오브젝트 참조 포함 여부
- Host와 Client의 초기화 순서 차이
- 같은 시스템의 중복 `SetUp` 또는 이벤트 구독
- Scene unload와 `OnDispose` 정리
- Host AOI region이 과거 player 위치에 누적되지 않고 player마다 현재 region
  하나만 남는지
- `UpdateAreaOfInterest`와 `StageBootstrapper.RegisterPlayerAreaOfInterest`
  profiler marker의 before/after 비용

## 기술 부채

여러 도메인의 조립 책임이 partial Bootstrapper에 집중되어 있다. 전체를 한 번에 교체하지 않고 소비자별 연결을 줄여야 한다.
