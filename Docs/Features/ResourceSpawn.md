# Resource Spawn

Status: Pilot slice complete

Last reviewed: 2026-08-14

## 책임

광물·가스 배치 계획을 만들고, 장애물과 최소 거리를 피해서 Host가 자원 NetworkObject를 Spawn하며, Territory 확장에 따른 수집과 Client AOI를 관리한다.

## 책임지지 않는 것

보유 광물·가스와 비용 지불은 `ResourceEconomy.md`가 담당한다. Territory 형상 자체는 `Territory.md`가 담당한다.

## 주요 진입점

- `Assets/02_Scripts/Resource/Network/ResourceSpawnSystem.cs`
- `ResourcePlacementSettings`, `ResourceVisible`, `ResourceZone`
- `StageBootstrapper.YOUInitializeObjects`
- 새 순수 로직: `Assets/02_Scripts/Features/ResourceSpawn/Runtime/`

## 상태와 권한

- Host·State Authority가 자원 생성, 수집, Despawn과 AOI 강제 관심을 결정한다.
- Client는 복제된 NetworkObject와 로컬 표시를 관찰한다.
- 시작 위치는 Spawn된 Laboratory에서 주입된다.

## 의존성

- Territory 확장 이벤트
- `InfiniteGridObstacleSpawner`와 `IWorldObstacleConsumer`
- `KIM.Dev.ResourceSystem`
- Fusion Runner와 PlayerRef

## 관련 Asset

- `Core.prefab`의 ResourceSpawnSystem과 ResourceSystem
- Resource placement ScriptableObject
- `Assets/03_Prefabs/Resource/`
- `GameWorld.unity`의 Resource 표시

## asmdef 파일럿

`ProjectIO.ResourceSpawn` assembly에는 `Assembly-CSharp`에 의존하지 않는 순수 계획 로직만 둔다. 기존 `ResourceSpawnSystem`과 직렬화 타입은 의존성 경계가 정리될 때까지 이동하지 않는다. 테스트 assembly는 Editor 전용으로 둔다.

첫 slice에서 `ResourceBudgetPlanner`를 분리하고 Legacy `ResourceSpawnSystem`이 선택된 option index를 기존 `ResourceChunkPlacementSettings`로 변환하도록 연결했다. Fusion Spawn, Territory, Grid, Scene·Prefab 참조는 변경하지 않았다.

## 변경 시 확인

- 같은 구역에서 예산을 초과하지 않고 가능한 최대 예산을 사용하는지
- 장애물·기존 자원 최소 거리
- State Authority 없는 Client가 Spawn·수집을 실행하지 않는지
- Territory 이벤트 해제, Despawn 후 관심 목록 정리
- ScriptableObject와 prefab 직렬화 호환성

## 기술 부채

배치 계획, 위치 샘플링, Fusion Spawn, Territory 수집, AOI가 한 클래스에 모여 있다. 파일럿은 예산 계획부터 분리한다.

## 검증

- Unity 6000.0.69f1 batch import와 script compile 성공
- `ProjectIO.ResourceSpawn.Tests` EditMode 테스트 3개 통과
- `dotnet build ProjectIO.slnx --no-restore` 오류 0개
