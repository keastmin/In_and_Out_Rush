# W-20260820-003 Grid 타워 배치 미리보기 Slice

Status: Complete

## 동기화 기준

- Base Commit: eb40c2af07c4bc99f9f8af73b27637e294427dc3
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Grid and Obstacles, Player Builder

## 목표

Grid의 타워 셀 배치 가능 여부 판정을 Unity·Fusion 비의존 순수 도메인 규칙으로 분리하고, 첫 Slice에서는 `PlayerBuilderTowerBuild.EvaluateBuildFootprint` 미리보기 호출자 하나만 새 판정을 사용한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/GridAndObstacles.md`
- `Docs/Features/PlayerBuilder.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/migrate-feature-slice/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Features/Grid.meta`
- `Assets/02_Scripts/Features/Grid/Logic.meta`
- `Assets/02_Scripts/Features/Grid/Logic/ProjectIO.GridPlacement.asmdef`
- `Assets/02_Scripts/Features/Grid/Logic/ProjectIO.GridPlacement.asmdef.meta`
- `Assets/02_Scripts/Features/Grid/Logic/TowerPlacementCellState.cs`
- `Assets/02_Scripts/Features/Grid/Logic/TowerPlacementCellState.cs.meta`
- `Assets/02_Scripts/Features/Grid/Logic/TowerPlacementPolicy.cs`
- `Assets/02_Scripts/Features/Grid/Logic/TowerPlacementPolicy.cs.meta`
- `Assets/02_Scripts/Features/Grid/Tests.meta`
- `Assets/02_Scripts/Features/Grid/Tests/ProjectIO.GridPlacement.Tests.asmdef`
- `Assets/02_Scripts/Features/Grid/Tests/ProjectIO.GridPlacement.Tests.asmdef.meta`
- `Assets/02_Scripts/Features/Grid/Tests/TowerPlacementPolicyTests.cs`
- `Assets/02_Scripts/Features/Grid/Tests/TowerPlacementPolicyTests.cs.meta`
- `Assets/02_Scripts/Player/Player Builder/PlayerBuilderTowerBuild.cs`
- `Docs/Features/GridAndObstacles.md`
- `ProjectIO.slnx`
- `Docs/Work/Completed/W-20260820-003-grid-tower-placement-preview-slice.md`

## 예약 Scene·Prefab·Data Asset

없음. 새 Unity 스크립트·asmdef와 대응 `.meta`만 추가한다.

## 공용 계약 또는 Bootstrapper 변경

없음. 새 순수 규칙은 건설 영역 포함, Track 차단, 셀 점유 입력만 소비한다.

## 다른 활성 작업과 겹치는 부분

없음. `Docs/Work/Active/README.md` 외 기존 Active 예약이 없다.

## Legacy와 새 진입점

- Legacy: `PlayerBuilderTowerBuild.EvaluateBuildFootprint`가 `InfiniteGrid.IsCellBuildBlocked`를 직접 호출한다.
- 새 경로: 미리보기 호출자가 기존 `InfiniteGrid` 조회 API로 셀 상태를 구성하고 `TowerPlacementPolicy`의 순수 판정을 사용한다.
- `InfiniteGrid.IsCellBuildBlocked`, `CanPlaceAt`, 실제 Tower Spawn과 Host 권위 검증은 Legacy authoritative 경로를 유지한다.

## 롤백

- `PlayerBuilderTowerBuild.EvaluateBuildFootprint`를 기존 `InfiniteGrid.IsCellBuildBlocked` 호출로 되돌리면 이 Slice만 즉시 롤백할 수 있다.
- 새 순수 assembly와 테스트는 다른 호출자 전환 전까지 독립적으로 제거 가능하며 직렬화 Asset 변경은 없다.

## 범위 밖

- `InfiniteGrid`의 실제 건설·점유·Host 검증 전환
- Grid 렌더링, Chunk 상태, Territory·Sanctuary 표시 변경
- Track 형상·차단 셀 계산 변경
- 장애물 Spawn·Despawn 변경
- Scene, Prefab, ScriptableObject 변경

## 완료 조건

- 순수 정책이 건설 영역 밖, Track 차단, 점유 셀을 각각 거부하고 유효한 빈 셀을 허용한다.
- `PlayerBuilderTowerBuild.EvaluateBuildFootprint` 한 호출자만 새 순수 판정으로 valid/blocked 셀을 분류한다.
- 미리보기 결과가 기존 `InfiniteGrid.IsCellBuildBlocked` 규칙과 동등하다.
- 실제 Tower 건설과 Host 권위 검증은 기존 경로를 유지한다.
- Grid 렌더링, Territory 표시, 장애물 Spawn 파일은 변경하지 않는다.
- 프로젝트 컴파일, 집중 테스트, `git diff --check`, `git status` 결과를 기록한다.

## 실제 변경

- Unity·Fusion 참조가 없는 `ProjectIO.GridPlacement` assembly를 추가했다.
- `TowerPlacementCellState`가 건설 영역 포함, Track 차단, 셀 점유 상태만 전달하고 `TowerPlacementPolicy.CanPlace`가 세 입력으로 배치 가능 여부를 판정한다.
- `PlayerBuilderTowerBuild.EvaluateBuildFootprint` 한 곳이 기존 `InfiniteGrid` 조회 API로 셀 상태를 구성한 뒤 새 순수 정책을 사용한다.
- `InfiniteGrid.IsCellBuildBlocked`, `InfiniteGrid.CanPlaceAt`, `TowerBuildManager`의 실제 건설 및 Host 검증 경로는 변경하지 않았다.
- Grid 렌더링, Territory·Sanctuary 표시, Track 차단 계산, 장애물 Spawn·Despawn 파일과 Scene·Prefab·ScriptableObject는 변경하지 않았다.
- 순수 정책의 허용·건설 영역 밖·Track 차단·점유 케이스를 검증하는 EditMode 테스트를 추가했다.
- `Docs/Features/GridAndObstacles.md`에 새 순수 진입점과 첫 미리보기 Slice 경계를 기록했다.

실제 수정 파일:

- `Assets/02_Scripts/Features/Grid.meta`
- `Assets/02_Scripts/Features/Grid/Logic.meta`
- `Assets/02_Scripts/Features/Grid/Logic/ProjectIO.GridPlacement.asmdef`
- `Assets/02_Scripts/Features/Grid/Logic/ProjectIO.GridPlacement.asmdef.meta`
- `Assets/02_Scripts/Features/Grid/Logic/TowerPlacementCellState.cs`
- `Assets/02_Scripts/Features/Grid/Logic/TowerPlacementCellState.cs.meta`
- `Assets/02_Scripts/Features/Grid/Logic/TowerPlacementPolicy.cs`
- `Assets/02_Scripts/Features/Grid/Logic/TowerPlacementPolicy.cs.meta`
- `Assets/02_Scripts/Features/Grid/Tests.meta`
- `Assets/02_Scripts/Features/Grid/Tests/ProjectIO.GridPlacement.Tests.asmdef`
- `Assets/02_Scripts/Features/Grid/Tests/ProjectIO.GridPlacement.Tests.asmdef.meta`
- `Assets/02_Scripts/Features/Grid/Tests/TowerPlacementPolicyTests.cs`
- `Assets/02_Scripts/Features/Grid/Tests/TowerPlacementPolicyTests.cs.meta`
- `Assets/02_Scripts/Player/Player Builder/PlayerBuilderTowerBuild.cs`
- `Docs/Features/GridAndObstacles.md`
- `ProjectIO.slnx`
- `Docs/Work/Active/W-20260820-003-grid-tower-placement-preview-slice.md`

## 검증 결과

- 순수 PowerShell/.NET 집중 하네스: 허용, 건설 영역 밖, Track 차단, 점유 4/4 통과.
- Unity 생성 `ProjectIO.ResourceEconomy.Tests.csproj`에 새 순수 로직과 NUnit 테스트 소스를 임시 주입해 컴파일: 경고 0개, 오류 0개.
- Unity 생성 `Assembly-CSharp.csproj`에 새 순수 로직 소스를 임시 주입해 `PlayerBuilderTowerBuild` 호출자까지 컴파일: 오류 0개, 기존 코드·Package 경고 16개.
- 임시 MSBuild targets는 검증 직후 제거했다.
- 참조 검색으로 `TowerPlacementPolicy` 런타임 호출자가 `PlayerBuilderTowerBuild.EvaluateBuildFootprint` 한 곳뿐임을 확인했다.
- `InfiniteGrid.IsCellBuildBlocked`, `CanPlaceAt`, `TowerBuildManager`의 기존 실제 건설 호출이 유지됨을 확인했다.
- Unity Editor가 `ProjectIO.GridPlacement`와 `ProjectIO.GridPlacement.Tests` assembly를 import·컴파일했고 Editor 로그에서 두 assembly 생성 성공을 확인했다.
- `dotnet build ProjectIO.slnx`: 성공, 오류 0개. 기존 코드와 외부 Package 경고 19개.
- 작업자가 Unity에서 Builder 배치 미리보기 동작 테스트 완료를 확인했다.
- `git diff --check`: 성공.

## 남은 위험

- 이번 Slice는 배치 미리보기 한 호출자만 전환했다. 실제 Tower 건설과 다른 Grid 소비자의 순수 정책 전환은 후속 작업으로 남아 있다.
