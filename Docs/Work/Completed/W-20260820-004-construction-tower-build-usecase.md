# W-20260820-004 Construction 타워 건설 UseCase Slice

Status: Complete

## 동기화 기준

- Base Commit: a98bc562d97a378515e898a663965f35a992a9aa
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Construction, Tower and Laboratory

## 목표

`TowerBuildManager.TryBuildTower`에 연속으로 배치된 Host 건설 절차를 `ProjectIO.Construction.TowerConstructionUseCase` 한 Slice로 분리한다. 기존 `TowerBuildManager`가 Host 권위 검증과 Fusion Spawn을 계속 소유하면서, Spawn된 Tower의 실제 Grid 점유 검증, 특수 Tower 초기화, authoritative Resource 지불, 성공 커밋 또는 실패 롤백을 하나의 결정적 흐름으로 조정한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/TowerAndLaboratory.md`
- `Docs/Features/GridAndObstacles.md`
- `Docs/Features/ResourceEconomy.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/migrate-feature-slice/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Features/Construction.meta`
- `Assets/02_Scripts/Features/Construction/UseCases.meta`
- `Assets/02_Scripts/Features/Construction/UseCases/ProjectIO.Construction.asmdef`
- `Assets/02_Scripts/Features/Construction/UseCases/ProjectIO.Construction.asmdef.meta`
- `Assets/02_Scripts/Features/Construction/UseCases/ITowerConstructionOperation.cs`
- `Assets/02_Scripts/Features/Construction/UseCases/ITowerConstructionOperation.cs.meta`
- `Assets/02_Scripts/Features/Construction/UseCases/TowerConstructionResult.cs`
- `Assets/02_Scripts/Features/Construction/UseCases/TowerConstructionResult.cs.meta`
- `Assets/02_Scripts/Features/Construction/UseCases/TowerConstructionUseCase.cs`
- `Assets/02_Scripts/Features/Construction/UseCases/TowerConstructionUseCase.cs.meta`
- `Assets/02_Scripts/Features/Construction/Tests.meta`
- `Assets/02_Scripts/Features/Construction/Tests/ProjectIO.Construction.Tests.asmdef`
- `Assets/02_Scripts/Features/Construction/Tests/ProjectIO.Construction.Tests.asmdef.meta`
- `Assets/02_Scripts/Features/Construction/Tests/TowerConstructionUseCaseTests.cs`
- `Assets/02_Scripts/Features/Construction/Tests/TowerConstructionUseCaseTests.cs.meta`
- `Assets/02_Scripts/Tower/Build/TowerBuildManager.cs`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/TowerAndLaboratory.md`
- `ProjectIO.slnx`
- `Docs/Work/Completed/W-20260820-004-construction-tower-build-usecase.md`

## 예약 Scene·Prefab·Data Asset

없음. 새 순수 스크립트·asmdef와 대응 `.meta`만 추가하며 Scene, Prefab, ScriptableObject, Network Prefab table은 변경하지 않는다.

## Construction 공개 계약

- `ITowerConstructionOperation`은 한 번의 Host 건설 시도에 필요한 `TrySpawn`, `ValidatePlacement`, `TryInitialize`, `TryPay`, `Commit`, `Rollback` 포트를 정의한다.
- `TowerConstructionUseCase`는 위 포트를 정해진 순서로 한 번씩 실행하고 `TowerConstructionResult`로 성공 또는 실패 단계를 반환한다.
- UseCase assembly는 Unity·Fusion·Legacy `Assembly-CSharp` 타입을 참조하지 않는다.
- 기존 `TowerBuildManager`가 요청별 operation을 구성해 UseCase에 전달하는 Fusion Adapter 역할을 맡는다. 별도 Bootstrapper나 Service Locator는 추가하지 않는다.

## 성공·실패 흐름

1. `TowerBuildManager`가 기존처럼 State Authority, Runner, Grid, Resource Adapter, Builder 존재와 요청자의 Input Authority를 먼저 검증한다.
2. `TrySpawn`은 `TowerBuildManager`가 기존 `Runner.Spawn`으로 정확히 한 번 수행하고 Spawn 결과의 `Tower`를 보관한다.
3. `ValidatePlacement`는 Host에서 Spawn된 Tower의 ID와 `GridPlaceable.HasGridOccupation`을 검사한다. 실제 점유는 기존 `GridPlaceable.Spawned -> InfiniteGrid.CanPlaceAt/AddActiveCell` 경로가 authoritative 기준이다.
4. `TryInitialize`는 기존 Center Tower 제한과 Supply Tower 초기화를 검사한다.
5. `TryPay`는 Spawn된 authoritative Tower의 `Cost`를 기존 `ResourcePaymentFusionAdapter.TryPay`에 한 번 전달한다. 별도 `CanAfford` 후 차감으로 나누지 않아 지불 시점의 Networked 잔액을 원자적으로 판정한다.
6. 모든 단계가 성공하면 기존 의존성 주입과 Center Tower 수량 갱신을 `Commit`한다.
7. Spawn 이후 어느 단계든 실패하면 `Rollback`이 Grid 점유를 해제하고 `Runner.Despawn`을 정확히 한 번 수행한다. 실패 결과는 기존 RPC 결과 경로로 요청 Peer에 전달한다.

## 네트워크 경계

- 입력 원점과 RPC signature는 기존 `TowerBuildManager.RPC_RequestTowerBuild`을 유지한다.
- State Authority만 UseCase를 실행하고 `Runner.Spawn`, Networked Resource 변경, Grid 점유, 실패 Despawn을 수행한다.
- 권한 없는 요청은 UseCase 진입 전에 기존 Builder `InputAuthority` 대조에서 거부한다.
- 성공 Tower는 기존 Fusion NetworkObject가 지속 상태이며 Late Join과 AOI 복원 경로를 그대로 사용한다.
- 실패 시 생성된 NetworkObject는 State Authority가 Despawn하고 Grid 점유를 해제하므로 Late Join에 남는 상태가 없다.
- Host가 로컬 Client이기도 한 경우에도 RPC 처리 한 번당 UseCase와 Spawn이 한 번만 실행된다.
- Networked property, RPC, AOI 설정, Spawn·Despawn 소유권은 변경하지 않는다.

## 다른 활성 작업과 겹치는 부분

없음. `Docs/Work/Active/README.md` 외 기존 Active 예약이 없다.

## Legacy와 새 진입점

- Legacy: `TowerBuildManager.TryBuildTower`가 Spawn, Grid 점유 결과 확인, 특수 Tower 초기화, 지불, 실패 Despawn, 성공 등록을 직접 순서대로 수행한다.
- 새 경로: `TowerBuildManager.TryBuildTower`가 권한과 요청자를 검증한 뒤 `TowerConstructionUseCase.Execute` 한 호출로 동일 단계를 조정한다.
- `TowerBuildManager`는 Host 권위 Spawn과 Fusion RPC Adapter로 계속 남고, 첫 Slice에서 다른 건설 호출자는 전환하지 않는다.

## 롤백

- `TowerBuildManager.TryBuildTower`를 기존 내부 순차 구현으로 되돌리면 이 Slice만 즉시 롤백할 수 있다.
- 새 `ProjectIO.Construction` assembly와 테스트는 첫 소비자가 `TowerBuildManager` 하나뿐이므로 독립적으로 제거 가능하다.
- Network schema와 직렬화 Asset 변경이 없어 Scene·Prefab 또는 세션 데이터 마이그레이션은 필요하지 않다.

## 범위 밖

- PlayerBuilder의 타워 선택, 요청 대기 상태, Ghost, Grid 미리보기와 UI
- Tower 이동·판매·업그레이드·속성 부여와 Laboratory 소비 흐름
- Grid 배치 규칙, Territory·Sanctuary·Track 판정, 점유 자료구조 변경
- Resource 비용 계산, Networked Mineral·Gas schema, 다른 Resource 소비자 변경
- Tower 종류별 능력, 공격·지원·버프 동작 변경
- Scene, Prefab, ScriptableObject, ProjectSettings, Package 변경

## 완료 조건

- Host 권위와 요청자 검증을 통과한 요청만 Construction UseCase를 실행한다.
- 성공 흐름은 Spawn, 실제 Grid 점유 검증, 특수 초기화, authoritative 지불, Commit을 순서대로 각각 한 번 수행한다.
- Spawn 또는 Grid 검증 실패는 지불·Commit을 실행하지 않고 생성된 객체와 Grid 점유를 정리한다.
- 특수 Tower 초기화 실패와 Resource 지불 실패도 같은 Rollback 경로로 Grid 점유와 NetworkObject를 정리한다.
- 지불 성공 뒤에는 실패 가능한 단계가 남지 않아 Resource 환불이 필요하지 않다.
- 성공 시에만 Tower 의존성과 Center Tower 수량을 Commit한다.
- Host Builder와 Client Builder의 성공·배치 실패·자원 부족 결과가 동일하고, 권한 없는 Builder 요청은 상태를 변경하지 않는다.
- Host 로컬 요청이 Spawn·결과 이벤트를 중복 실행하지 않는다.
- Late Join Client에는 성공 Tower만 복원되고 실패 시도 객체는 관찰되지 않는다.
- 이동·판매·업그레이드, Scene·Prefab, UI diff가 없다.
- 순수 UseCase 집중 테스트, Unity assembly 컴파일, 전체 solution 빌드, `git diff --check`를 통과한다.

## 실제 변경

- Unity·Fusion 참조가 없는 `ProjectIO.Construction` assembly에 `ITowerConstructionOperation`, `TowerConstructionResult`, `TowerConstructionUseCase`를 추가했다.
- UseCase가 Spawn, 실제 Grid 점유 검증, 특수 Tower 초기화, authoritative Resource 지불, Commit을 순서대로 실행하고 각 실패를 하나의 Rollback으로 귀결한다.
- `TowerBuildManager.TryBuildTower` 한 호출자만 새 UseCase를 사용한다. Manager 내부 요청별 Host operation이 기존 `Runner.Spawn`, spawned Tower의 `HasGridOccupation`, `ResourcePaymentFusionAdapter.TryPay`, 실패 Grid 해제·Despawn을 연결한다.
- 기존 State Authority, Builder Input Authority, 특수 Tower 사전 검증, RPC signature와 결과 전달 경로를 유지했다.
- 성공 시에만 Tower 의존성을 주입하고 Center Tower 수량을 갱신하며, 지불 뒤에는 실패 가능한 단계를 두지 않았다.
- Construction 책임과 진입점을 `Docs/PROJECT_MAP.md`와 `Docs/Features/TowerAndLaboratory.md`에 기록했다.
- PlayerBuilder, 이동·판매·업그레이드, Grid·Resource 구현, Scene·Prefab·UI 파일은 변경하지 않았다.

실제 수정 파일:

- `Assets/02_Scripts/Features/Construction.meta`
- `Assets/02_Scripts/Features/Construction/UseCases.meta`
- `Assets/02_Scripts/Features/Construction/UseCases/ProjectIO.Construction.asmdef`
- `Assets/02_Scripts/Features/Construction/UseCases/ProjectIO.Construction.asmdef.meta`
- `Assets/02_Scripts/Features/Construction/UseCases/ITowerConstructionOperation.cs`
- `Assets/02_Scripts/Features/Construction/UseCases/ITowerConstructionOperation.cs.meta`
- `Assets/02_Scripts/Features/Construction/UseCases/TowerConstructionResult.cs`
- `Assets/02_Scripts/Features/Construction/UseCases/TowerConstructionResult.cs.meta`
- `Assets/02_Scripts/Features/Construction/UseCases/TowerConstructionUseCase.cs`
- `Assets/02_Scripts/Features/Construction/UseCases/TowerConstructionUseCase.cs.meta`
- `Assets/02_Scripts/Features/Construction/Tests.meta`
- `Assets/02_Scripts/Features/Construction/Tests/ProjectIO.Construction.Tests.asmdef`
- `Assets/02_Scripts/Features/Construction/Tests/ProjectIO.Construction.Tests.asmdef.meta`
- `Assets/02_Scripts/Features/Construction/Tests/TowerConstructionUseCaseTests.cs`
- `Assets/02_Scripts/Features/Construction/Tests/TowerConstructionUseCaseTests.cs.meta`
- `Assets/02_Scripts/Tower/Build/TowerBuildManager.cs`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/TowerAndLaboratory.md`
- `ProjectIO.slnx`
- `Docs/Work/Active/W-20260820-004-construction-tower-build-usecase.md`

## 검증 결과

- 순수 PowerShell/.NET 집중 하네스: 성공, Spawn 실패, Grid 배치 거부, 초기화 실패, 지불 실패, operation 누락 6/6 통과.
- Unity 생성 `ProjectIO.ResourceEconomy.Tests.csproj`에 새 Construction UseCase와 NUnit 테스트 소스를 임시 주입해 컴파일: 경고 0개, 오류 0개.
- Unity 생성 `Assembly-CSharp.csproj`에 새 Construction 소스를 임시 주입해 `TowerBuildManager` 호출자까지 컴파일: 오류 0개, 기존 코드·Package 경고 16개.
- 임시 MSBuild targets는 검증 직후 제거했다.
- asmdef JSON 2개 유효성과 새 Unity GUID 9개의 유일성을 확인했다.
- 참조 검색으로 `TowerConstructionUseCase` 런타임 호출자가 `TowerBuildManager.TryBuildTower` 한 곳뿐임을 확인했다.
- 코드 대조로 UseCase 호출 전에 `HasStateAuthority`와 요청 Builder의 `InputAuthority` 검증이 유지되고, 기존 RPC attribute·signature가 변경되지 않았음을 확인했다.
- `git diff --check`: 성공.
- 새 `W-20260820-006-match-progression-transition-policy.md`는 TimeSystem·MatchProgression만 예약하고 Construction 파일과 `ProjectIO.slnx`를 제외하므로 별도 사용자 변경으로 보존했다.
- Unity Editor가 `ProjectIO.Construction.dll`과 `ProjectIO.Construction.Tests.dll`을 실제 생성해 새 asmdef와 테스트 assembly import를 확인했다.
- `dotnet build ProjectIO.slnx`: 오류 0개, 기존 코드·Package 경고 20개로 성공했다.
- 작업자가 요청 범위의 수동 플레이 테스트 완료를 확인했다.

## 남은 위험

- `Runner.Spawn` 직후 `GridPlaceable.Spawned`가 완료되어 `HasGridOccupation`을 읽을 수 있다는 기존 동기 실행 전제는 유지된다.
- 이번 Slice는 첫 건설 호출자 하나만 전환했으므로 이후 다른 건설 소비자를 옮길 때 동일한 Host 권위와 Rollback 계약을 유지해야 한다.
