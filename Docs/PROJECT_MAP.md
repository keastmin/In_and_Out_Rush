# ProjectIO Project Map

마지막 대조: 2026-08-29, implementation base `ce92742`

이 문서는 전체 코드 설명서가 아니라 작업 요청을 올바른 기능 문서와 진입점으로 보내는 지도다.

| 기능 | 책임 | 주요 진입점 | 공용 연결부·주요 Asset | 기능 문서 |
|---|---|---|---|---|
| 세션과 Additive Scene | 방 생성·참가, 역할 등록, 게임 Scene 로딩 | `MatchMaker`, `NetworkManager`, `PlayerRegistry` | `LobbyScene`, `GameWorld`, `GamePresentation`, `GameRoot` | [SessionAndScenes](Features/SessionAndScenes.md) |
| Stage 초기화 | 시스템 발견, 플레이어 스폰, 기능 연결 순서 | `StageBootstrapper` partials | `Core.prefab`, `GameRoot` | [StageInitialization](Features/StageInitialization.md) |
| Grid와 장애물 | 셀 점유, 표시 Chunk, 장애물 생성·제거 | `InfiniteGrid`, `InfiniteGridObstacleSpawner` | Territory, Track, Tower, Resource Spawn | [GridAndObstacles](Features/GridAndObstacles.md) |
| Territory | Polygon 영역 상태·청크/쿼드트리 판정·확장과 네트워크 Trail 표현 | `TerritorySystem`, `Territory`, `TerritorySpatialIndex`, `TerritoryExpansionSession` | Grid, Fog, Resource, Monster, Sacred Zone, Sanctuary, `build-chunk-territory` | [Territory](Features/Territory.md) |
| Track와 Round | 트랙 확장, 라운드 시간과 웨이브 연결 | `TrackSystem`, `TimeSystem` | Track monster, StageBootstrapper | [TrackAndRounds](Features/TrackAndRounds.md) |
| Resource Spawn | 자원 배치 계획, 네트워크 Spawn, AOI와 수집 | `ResourceSpawnSystem`, `ResourcePlacementSettings` | Territory, 장애물, Laboratory, ResourceSystem | [ResourceSpawn](Features/ResourceSpawn.md) |
| Resource Economy | 광물·가스 상태와 비용 지불 | `KIM.Dev.ResourceSystem`, `Cost` | PlayerBuilder, Tower, UI | [ResourceEconomy](Features/ResourceEconomy.md) |
| Construction | 타워 건설의 Spawn·Grid 검증·지불·롤백 조정 | `TowerConstructionUseCase`, `TowerBuildManager` | Grid, Resource Economy, Fusion Spawn | [TowerAndLaboratory](Features/TowerAndLaboratory.md) |
| Player Runner | 이동, 전투, 체력, 장비 사용 | `PlayerRunner`, `NetworkInputSystem` | Territory, Monster, UI, Camera | [PlayerRunner](Features/PlayerRunner.md) |
| Runner Item·Skill | 아이템 소비 전략, 스킬 선택·실행 | `RunnerItemInventory`, `RunnerSkillCaster` | Runner, Network Spawn, Laboratory | [RunnerItemsAndSkills](Features/RunnerItemsAndSkills.md) |
| Player Builder | 건설·이동·판매 상태와 입력 | `PlayerBuilder`, `PlayerBuilderStateMachine` | Grid, Tower, Resource, Builder UI | [PlayerBuilder](Features/PlayerBuilder.md) |
| Tower와 Laboratory | 타워 생명주기·공격·지원·업그레이드 | `Tower`, `TowerBuildManager`, `Laboratory` | Grid, Resource, Runner, Builder | [TowerAndLaboratory](Features/TowerAndLaboratory.md) |
| Monster와 Projectile | 월드·트랙 몬스터와 투사체 | `Monster`, `WorldMonsterSpawnSystem`, `TrackMonsterSpawnSystem` | Track, Territory, Runner, Stage | [MonstersAndProjectiles](Features/MonstersAndProjectiles.md) |
| Sacred Zone·Sanctuary·Gate | 정화 구역, 안식처, 출구와 승패 흐름 | `SacredZoneSystem`, `SanctuaryView`, `Gate` | Territory, Monster, StageSystem | [SacredZoneSanctuaryGate](Features/SacredZoneSanctuaryGate.md) |
| Stage UI | Builder·Runner HUD와 입력 전달 | `StageUIController`, `PlayerBuilderUI`, `PlayerRunnerUI` | StageBootstrapper, Resource, Time | [StageUI](Features/StageUI.md) |
| Fog와 Camera | 로컬 시야 마스크와 역할별 카메라 | `FogOfWarSystem`, `CinemachineSystem` | TerritoryVisible, PlayerRunner, PlayerBuilder | [FogOfWarAndCamera](Features/FogOfWarAndCamera.md) |
| Ping | Runner 핑 생성과 표시 | `PingSystem`, `PlayerPing` | PlayerRunnerPingGuide, Fusion | [Ping](Features/Ping.md) |
| 공통 Lifecycle·Spawn | Entity/System/View 수명과 일반 Spawn 정책 | `Dev.*` lifecycle, `Spawner`, `SpawnPolicy` | 여러 기능이 사용하는 Legacy 기반 | [SharedLifecycleAndSpawn](Features/SharedLifecycleAndSpawn.md) |
| Test Mode | Lobby 없이 Host 테스트 환경 구성 | `TestModeGameSceneSetup`, `PlayerRunnerTestModeGUI` | `GameRoot`, Runner prefab | [TestMode](Features/TestMode.md) |

## 코드 디렉터리 귀속

| 현재 디렉터리 | 기능 문서 |
|---|---|
| `Network`, `Manager` | SessionAndScenes |
| `Stage`, `Global` | StageInitialization |
| `Grid`, `Field` | GridAndObstacles |
| `Territory`, `Territory Refactor`, `Features/Territory/Logic` | Territory |
| `Track`, `Time` | TrackAndRounds |
| `Resource/Network` | ResourceSpawn |
| `Resource/Local`, `System/ResourceSystem.cs` | ResourceEconomy |
| `Features/Construction` | TowerAndLaboratory |
| `Player/Player Runner` | PlayerRunner, RunnerItemsAndSkills |
| `Player/Player Builder`, `Drag`, `Interactable Object` | PlayerBuilder |
| `Tower`, `Laboratory` | TowerAndLaboratory |
| `Monster`, `Projectile` | MonstersAndProjectiles |
| `SacredZone`, `Sanctuary`, `Gate` | SacredZoneSanctuaryGate |
| `Stage UI`, `UI` | StageUI |
| `Fog of War`, 카메라 스크립트 | FogOfWarAndCamera |
| `Ping System` | Ping |
| `Lifecycle`, `Spawn`, `Obtainable` | SharedLifecycleAndSpawn 또는 소비 기능 문서 |
| `Test Mode` | TestMode |
| 루트 `NetworkInputSystem.cs` | PlayerRunner |
| 루트 `CinemachineSystem.cs` | FogOfWarAndCamera |
| 루트 `IDamageable.cs` | MonstersAndProjectiles와 PlayerRunner의 공용 Legacy 계약 |
| 루트 `ShaderTest.cs` | 실험 코드. 활성 기능 진입점으로 사용하지 않음 |

## 공용 충돌 지점

다음 파일과 Asset은 여러 기능이 공유하므로 활성 작업 파일에 반드시 예약한다.

- `StageBootstrapper.cs`, `StageBootstrapper.KIM.cs`, `StageBootstrapper.YOU.cs`
- `Core.prefab`
- `GameWorld.unity`, `GamePresentation.unity`, `GameRoot.unity`
- `EditorBuildSettings.asset`
- 공용 `NetworkRunner`, `NetworkManager`, `ResourceManager` prefab
- 여러 기능이 사용하는 공개 interface, enum, ScriptableObject

## 지도 업데이트 조건

기능 책임, 주요 진입점, 공용 연결부 또는 디렉터리 귀속이 바뀔 때만 이 문서를 갱신한다. 내부 메서드 변경이나 버그 수정은 해당 기능 문서 또는 완료 작업 기록에만 남긴다.
