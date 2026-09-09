# W-20260814-001 Resource Spawn 리팩토링 파일럿

Status: Complete

## 담당자

현재 작업을 수행한 협업자와 AI

## 기능

Resource Spawn

## 목표

기존 네트워크 Spawn과 직렬화 참조를 유지하면서 자원 예산 계획 로직을 독립 assembly로 분리한다. 이 결과로 ProjectIO의 도메인 폴더와 asmdef 운영 방식을 검증한다.

## 읽은 문서와 Skill

- `Docs/Features/ResourceSpawn.md`
- `Docs/Features/GridAndObstacles.md`
- `Docs/Features/Territory.md`
- `migrate-feature-slice`
- `photon-fusion-feature`

## 실제 변경

- `ProjectIO.ResourceSpawn` 순수 runtime asmdef 추가
- `ProjectIO.ResourceSpawn.Tests` Editor test asmdef 추가
- `ResourceBudgetPlanner`로 fillable budget과 option 선택 로직 분리
- Legacy `ResourceSpawnSystem`이 새 planner 결과를 기존 설정 객체로 변환하도록 연결
- Scene, Prefab, ScriptableObject와 네트워크 Spawn·AOI 동작은 변경하지 않음

## 예약 Scene·Prefab·Data Asset

없음. 실제 Asset 변경 없음.

## 공용 계약 또는 Bootstrapper 변경

없음

## 범위 밖

- Resource Spawn 전체를 새 assembly로 이동
- Territory 소비자를 Chunk Territory로 전환
- Prefab 또는 Scene 참조 변경
- 네트워크 Spawn·AOI 규칙 변경

## 검증 결과

- Unity 6000.0.69f1 batch import: 성공, script compile 오류 0개
- Unity EditMode `ProjectIO.ResourceSpawn.Tests`: 3/3 Passed
- `dotnet restore ProjectIO.slnx --ignore-failed-sources`: 성공
- `dotnet build ProjectIO.slnx --no-restore`: 성공, 오류 0개, 기존 경고 22개
- 런타임 Host·Client 동작은 변경하지 않았으므로 이번 순수 로직 slice에서 별도 실행하지 않음

## 롤백

`ResourceSpawnSystem.CreateResourceBudgetPlan`을 기존 내부 DP 구현으로 되돌리고 `Assets/02_Scripts/Features/ResourceSpawn/`을 제거하면 된다. Unity 직렬화 Asset에는 롤백 항목이 없다.

## 남은 위험

기존 `ResourceSpawnSystem`의 Fusion, Territory, Grid 의존성 때문에 전체 런타임 파일은 첫 asmdef에 포함하지 않았다. 다음 slice는 위치 샘플링 또는 장애물 판정을 순수 계약으로 분리할지 별도 작업으로 승인해야 한다.
