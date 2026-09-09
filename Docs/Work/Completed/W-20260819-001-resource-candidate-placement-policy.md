# W-20260819-001 자원 후보 배치 판정 도메인 분리

Status: Complete

## 동기화 기준

- Base Commit: 187dbd973577c016e91a57cff16b95f4bea11db0
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Resource Spawn

## 목표

기존 Resource Spawn 동작을 유지하면서 후보 위치가 장애물 및 이미 배치된 자원과 요구 최소 거리를 만족하는지 판정하는 흐름 하나를 순수 도메인 로직으로 분리한다. Legacy `ResourceSpawnSystem.TryFindResourcePosition` 호출자 한 곳만 새 정책을 사용한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/ResourceSpawn.md`
- `manage-feature-work`
- `migrate-feature-slice`

## 예상 수정 코드

- `Assets/02_Scripts/Resource/Network/ResourceSpawnSystem.cs`
- `Assets/02_Scripts/Features/ResourceSpawn/Runtime/ResourceCandidatePlacementPolicy.cs`
- `Assets/02_Scripts/Features/ResourceSpawn/Runtime/ResourceCandidatePlacementPolicy.cs.meta`
- `Assets/02_Scripts/Features/ResourceSpawn/Tests/ResourceCandidatePlacementPolicyTests.cs`
- `Assets/02_Scripts/Features/ResourceSpawn/Tests/ResourceCandidatePlacementPolicyTests.cs.meta`
- `Docs/Features/ResourceSpawn.md`
- `Docs/Work/Active/W-20260819-001-resource-candidate-placement-policy.md`

## 예약 Scene·Prefab·Data Asset

없음

## 공용 계약 또는 Bootstrapper 변경

- `ProjectIO.ResourceSpawn` 순수 assembly에 후보 XZ 위치, 장애물까지의 XZ 거리 제곱, 기존 자원 위치·최소 거리만 입력받는 `ResourceCandidatePlacementPolicy` 공개 순수 정책을 추가한다.
- 기존 Bootstrapper, 직렬화 필드, Resource placement ScriptableObject 계약은 변경하지 않는다.

## 다른 활성 작업과 겹치는 부분

없음. 확인 시 `Docs/Work/Active/`에는 안내용 `README.md`만 존재한다.

## 범위 밖

- Fusion Spawn, NetworkObject, Authority, AOI, Late Join 수명주기 변경
- Territory 이벤트와 Territory 내부·외부 판정 변경
- Scene, Prefab, ScriptableObject, ProjectSettings, Package 변경
- 후보 위치 샘플링, 예산 계획, 재시도 횟수와 Random 호출 순서 변경
- `ResourceSpawnSystem` 외 다른 소비자 전환
- 기존 `ResourceBudgetPlanner` 또는 다른 Legacy 경로 제거

## 완료 조건

- 장애물은 기존과 동일하게 요구 clearance와의 거리가 같아도 후보를 거부한다.
- 기존 자원은 후보와 기존 자원의 최소 거리 중 큰 값을 사용하며, 기존과 동일하게 정확히 경계에 있으면 허용한다.
- 전체 구역에 확정된 자원과 현재 구역에서 임시 배치된 자원을 모두 판정한다.
- Territory 판정과 위치 샘플링 뒤 `ResourceSpawnSystem.TryFindResourcePosition` 한 곳만 새 정책을 호출한다.
- Fusion Spawn, Territory 이벤트, Scene·Prefab 및 직렬화 계약 diff가 없다.
- 순수 EditMode 집중 테스트, 프로젝트 스크립트 컴파일, `dotnet build ProjectIO.slnx --no-restore`, `git diff --check`를 가능한 범위에서 통과한다.
- 롤백은 `ResourceSpawnSystem`을 기존 내부 장애물·자원 거리 판정으로 되돌리고 새 순수 정책과 테스트 파일을 제거하는 것으로 한정한다.
- 다른 Resource Spawn 소비자의 전환은 후속 작업으로 미룬다.

## 실제 변경

- `ProjectIO.ResourceSpawn` 순수 assembly에 `ResourceCandidatePlacementPolicy`와 XZ 위치·최소 거리를 담는 기배치 자원 값을 추가했다.
- 장애물은 요구 clearance 경계를 포함해 거부하고, 기배치 자원은 후보·기존 최소 거리 중 큰 값보다 가까울 때만 거부하도록 기존 비교 규칙을 유지했다.
- Legacy `ResourceSpawnSystem.TryFindResourcePosition` 한 곳이 Territory 판정을 기존 위치에서 수행한 뒤 새 정책을 호출하도록 연결했다.
- Collider 최근접점과 bounds fallback의 XZ 거리를 Legacy 어댑터에서 계산하고 재사용 목록으로 순수 정책에 전달한다.
- 전체 구역 확정 자원과 현재 구역 임시 자원을 모두 정책 입력으로 전달한다.
- `ResourceCandidatePlacementPolicyTests` 5개를 추가하고 `Docs/Features/ResourceSpawn.md`에 두 번째 slice 경계와 검증을 기록했다.
- Fusion Spawn, Territory 이벤트, Scene, Prefab, ScriptableObject, Bootstrapper와 `Docs/PROJECT_MAP.md`는 변경하지 않았다.

실제 수정 파일:

- `Assets/02_Scripts/Resource/Network/ResourceSpawnSystem.cs`
- `Assets/02_Scripts/Features/ResourceSpawn/Runtime/ResourceCandidatePlacementPolicy.cs`
- `Assets/02_Scripts/Features/ResourceSpawn/Runtime/ResourceCandidatePlacementPolicy.cs.meta`
- `Assets/02_Scripts/Features/ResourceSpawn/Tests/ResourceCandidatePlacementPolicyTests.cs`
- `Assets/02_Scripts/Features/ResourceSpawn/Tests/ResourceCandidatePlacementPolicyTests.cs.meta`
- `Docs/Features/ResourceSpawn.md`
- `Docs/Work/Completed/W-20260819-001-resource-candidate-placement-policy.md`

## 검증 결과

- 실제 `ResourceCandidatePlacementPolicyTests.cs`를 호출한 임시 .NET/NUnit 하네스: 5/5 Passed.
- `dotnet build ProjectIO.ResourceSpawn.Tests.csproj --no-restore`에 새 Runtime·Tests 파일을 임시 targets로 주입: 성공, 경고 0개, 오류 0개.
- 수정한 `ResourceSpawnSystem.cs`를 Unity 기존 `Assembly-CSharp.dll`, Fusion·Unity 참조와 새 `ProjectIO.ResourceSpawn.dll`로 독립 컴파일: 성공, 오류 0개, 기존 직렬화 필드 경고 3개.
- Unity 6000.0.69f1 EditMode 실행: Licensing Client 재연결이 완료되지 않아 테스트 시작 전 중단. 결과 파일 없음.
- `dotnet restore ProjectIO.slnx --ignore-failed-sources`: 성공.
- 전체 `dotnet build ProjectIO.slnx --no-restore`: 새 `ProjectIO.ResourceSpawn`과 Tests assembly는 빌드됐으나, 생성된 `Assembly-CSharp.csproj`가 이미 삭제·이동된 Legacy 소스 경로 28개를 참조해 실패. 이번 예약 파일과 무관한 기존 생성 프로젝트 노후화로 분리 기록.
- `git diff --check`: 성공.
- 예약 구현에서 Scene·Prefab·ProjectSettings·Packages는 변경하지 않음.
- 작업자가 Unity 테스트 확인 완료를 보고하고 최종 Commit·Push를 요청함.

## 남은 위험

- Unity Collider의 최근접점 계산은 Legacy 어댑터에 남고 순수 정책은 계산된 XZ 거리 제곱만 판정한다. Host 실행에서 Collider 형상별 배치 결과와 Unity EditMode 8개 전체 테스트(기존 3개와 신규 5개)는 작업자 수동 확인이 남아 있다.
- Unity 재검증 전에 Licensing Client 상태와 생성 csproj의 삭제된 Legacy 경로를 갱신해야 한다.
- 작업자 테스트 과정에서 생긴 `Assets/03_Prefabs/Core.prefab`, `Assets/03_Prefabs/Resource/Network/Resource Zone.prefab`, `ProjectIO.slnx` 로컬 변경은 예약 밖 사용자 변경으로 보존하며 이번 Commit에서 제외한다.
