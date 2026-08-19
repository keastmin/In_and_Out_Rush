# W-20260819-001 자원 후보 배치 판정 도메인 분리

Status: Reserved

## 동기화 기준

- Base Commit: `187dbd973577c016e91a57cff16b95f4bea11db0`
- 공용 Upstream: `origin/rebuild-development-environment`

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

예약 단계. 구현 전.

## 검증 결과

예약 단계. 구현 전.

## 남은 위험

- Unity Collider의 최근접점 계산은 Legacy 어댑터에 남기고, 순수 정책은 계산된 XZ 거리 제곱을 판정한다. Collider 형상별 기존 경계 동작은 집중 테스트와 컴파일 후 수동 실행 확인이 필요하다.
