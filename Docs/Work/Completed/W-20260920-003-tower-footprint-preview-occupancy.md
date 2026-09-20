# W-20260920-003 타워 점유 셀의 범위형 설치 미리보기 수정

Status: Completed

## 동기화 기준

- Base Commit: 90758aaa08f1d526c3ffa7c905515ba0d6497410
- 공용 Upstream: `origin/rebuild-development-environment`

## 담당자

Codex

## 기능

Grid 점유 조회와 Player Builder의 범위형 타워 설치 미리보기.

## 목표

센터타워 등 설치 범위에 기존 타워의 점유 셀이 들어오면 설치 불가 지역과 동일하게 해당 범위 셀과 타워 고스트를 설치 불가 색상으로 표시한다. Host와 Client의 읽기 전용 미리보기가 복제된 Grid 점유를 동일하게 사용하게 한다.

## 읽을 문서와 Skill

- `AGENTS.md`, `Docs/PROJECT_MAP.md`
- `Docs/Features/GridAndObstacles.md`, `Docs/Features/PlayerBuilder.md`, `Docs/Features/TowerAndLaboratory.md`
- `.agents/skills/manage-feature-work/SKILL.md`, `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Grid/InfiniteGrid.cs`: 프록시에서도 유효한 복제 Grid 점유를 읽도록 조회 준비 조건 수정. State Authority의 점유 변경 조건은 유지.

## 예약 Scene·Prefab·Data Asset

없음

## 공용 계약 또는 Bootstrapper 변경

없음. `InfiniteGrid`의 기존 공개 조회와 `NetworkGrid` 복제 계약을 그대로 사용한다.

## 네트워크·Peer 동등성

- 입력 원점: Host 로컬 또는 Client Input Authority의 `PlayerBuilderTowerBuildState`가 마우스 위치에서 설치 미리보기를 계산하고 기존 요청 경로로 건설 의도를 보낸다.
- 권위와 결과: `TowerBuildManager`와 타워의 Grid 점유 등록은 기존처럼 State Authority가 검증·변경한다. 점유는 `InfiniteGrid.NetworkGrid`로 복제되고 건설 성공·거부 결과는 기존 응답 경로로 요청 Peer에 돌아간다.
- 미리보기: Host와 Client가 각자의 로컬 Builder에서 복제된 점유 셀을 읽어 기존 타워와 겹치는 셀을 차단 표시하고 고스트를 설치 불가 색상으로 표시한다. 빈 범위의 녹색 표시와 실제 성공 경로도 확인한다.
- 중복·준비·Late Join·정리: 읽기 조건만 조정하며 Spawn, RPC, 권위 변경 또는 중복 실행을 추가하지 않는다. Grid NetworkObject가 아직 유효하지 않을 때의 접근을 막고, Late Join의 복제 점유와 타워 Despawn 후 점유 해제도 확인한다.
- 런타임 검증: Host와 Client 각각에서 기존 타워 위에 센터타워 범위를 겹쳐 빨간 셀·고스트 및 건설 거부를 확인하고, 빈 셀로 옮겨 녹색 및 건설 성공을 확인한다. 가능한 경우 Late Join과 타워 제거 후 미리보기 갱신도 확인한다. 다중 Peer 실행 환경이 없으면 수행하지 못한 항목과 작업자 절차를 명시한다.

## 다른 활성 작업과 겹치는 부분

없음. 예약 검사 시 `Docs/Work/Active/`에 다른 작업 문서가 없었다.

## 범위 밖

타워 건설 RPC·Spawn·지불 로직, Grid 점유의 authoritative 변경, 타워 이동 미리보기, Scene·Prefab·Shader 변경.

## 완료 조건

- 기존 타워가 설치 범위 안에 있을 때 해당 셀과 고스트가 설치 불가 색상으로 일치한다.
- 설치 불가 지역·Track 차단과 빈 범위의 기존 표시 및 Host 권위 건설 결과가 유지된다.
- 가능한 집중 검증과 프로젝트 컴파일, `git diff --check`, Host·Client 런타임 검증 결과 또는 미실행 사유와 수동 절차를 기록한다.
- 문제가 생기면 이 작업의 `InfiniteGrid.cs` 읽기 조건 변경을 되돌려 기존 동작으로 복귀할 수 있다.

## 실제 변경

- `Assets/02_Scripts/Grid/InfiniteGrid.cs`: `CanUseNetworkGrid`의 `Object.IsInSimulation` 조건을 제거했다. Spawn이 끝난 유효한 Grid 프록시도 복제된 `NetworkGrid`를 읽을 수 있으며, `IsCellOccupied`를 공유하는 범위 셀 미리보기와 고스트 판정에 타워 점유가 반영된다.
- 점유 추가·제거의 `HasStateAuthority` 검사, 타워 건설 RPC·Spawn·지불, Scene·Prefab·Shader는 변경하지 않았다.

## 검증 결과

- 정적 경로 확인: `PlayerBuilderTowerBuild.EvaluateBuildFootprint`가 `IsCellOccupied`로 차단 셀을 정하고 `PlayerBuilderTowerBuildState.SnapshotTowerGhost`가 같은 결과로 고스트 색상을 고른다. `TowerBuildManager.CanBuildAt`도 `InfiniteGrid.CanPlaceAt`을 사용한다.
- Unity 6000.0.69f1 배치 모드에서 프로젝트 스크립트 컴파일 완료. 컴파일 오류 기록 없음.
- EditMode `ProjectIO.GridPlacement.Tests.TowerPlacementPolicyTests`: 4 passed, 0 failed. 테스트 결과 파일: `C:/Users/kemin/AppData/Local/Temp/projectio-tower-preview-tests.xml`.
- `git diff --check` 통과. 변경 파일은 예약된 `InfiniteGrid.cs`와 이 작업 문서뿐이다.
- Host 로컬 및 Client Input Authority의 실제 게임 미리보기·건설 결과는 이 환경에서 다중 Peer를 실행하지 않아 각각 미검증이다.

## 남은 위험

- Fusion 복제 점유가 Client에 도착하는 시점까지의 일시적인 표시 차이와 실제 색상은 런타임에서 확인해야 한다.
- 작업자 확인 절차: Host와 Client에서 각각 Builder로 접속한다. 기존 타워를 설치한 뒤 센터타워 고스트의 범위를 그 타워의 점유 셀 위로 옮겨 해당 셀과 고스트가 빨간색인지 확인한다. 빈 범위에서는 녹색인지 확인하고 설치 성공을 확인한다. 기존 타워를 제거한 뒤 다시 녹색으로 갱신되는지 확인한다. 가능하면 타워 설치 후 새 Client로 참가해 같은 차단 표시가 나타나는지도 확인한다.
