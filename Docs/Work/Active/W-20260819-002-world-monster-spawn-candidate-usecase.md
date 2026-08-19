# W-20260819-002 World Monster Spawn 후보 선택 UseCase 분리

Status: Reserved

## 동기화 기준

- Base Commit: 13fa6bb09ebd5251d5a8dfcd43e4976718a73d33
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Monster and Projectiles

## 목표

기존 World Monster 동작과 Fusion Authority를 유지하면서 `WorldMonsterSpawnSystem.RefreshChunkStreaming`이 주변 record 중 이번 refresh에 Spawn할 후보를 고르는 흐름 하나만 `Assets/02_Scripts/Features/Monster/`의 순수 Logic과 UseCase로 분리한다. 실제 `NetworkObject` 생성과 초기화는 기존 `WorldMonsterSpawnSystem.SpawnRecord`의 `Runner.Spawn` 경로에 남기고 같은 record가 한 refresh에서 중복 선택·Spawn되지 않게 한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/MonstersAndProjectiles.md`
- `Docs/Work/Completed/W-20260815-010-network-hotpath-spatial-refactor.md`
- `manage-feature-work`
- `migrate-feature-slice`
- `photon-fusion-feature`

## 예상 수정 코드

- `Assets/02_Scripts/Monster/WorldMonsterSpawnSystem.cs`
- `Assets/02_Scripts/Features/Monster/Logic/WorldMonsterSpawnCandidate.cs`
- `Assets/02_Scripts/Features/Monster/Logic/WorldMonsterSpawnCandidate.cs.meta`
- `Assets/02_Scripts/Features/Monster/Logic/WorldMonsterSpawnCandidatePolicy.cs`
- `Assets/02_Scripts/Features/Monster/Logic/WorldMonsterSpawnCandidatePolicy.cs.meta`
- `Assets/02_Scripts/Features/Monster/UseCases.meta`
- `Assets/02_Scripts/Features/Monster/UseCases/ProjectIO.Monsters.UseCases.asmdef`
- `Assets/02_Scripts/Features/Monster/UseCases/ProjectIO.Monsters.UseCases.asmdef.meta`
- `Assets/02_Scripts/Features/Monster/UseCases/SelectWorldMonsterSpawnCandidatesUseCase.cs`
- `Assets/02_Scripts/Features/Monster/UseCases/SelectWorldMonsterSpawnCandidatesUseCase.cs.meta`
- `Assets/02_Scripts/Features/Monster/Tests/ProjectIO.Monsters.Tests.asmdef`
- `Assets/02_Scripts/Features/Monster/Tests/SelectWorldMonsterSpawnCandidatesUseCaseTests.cs`
- `Assets/02_Scripts/Features/Monster/Tests/SelectWorldMonsterSpawnCandidatesUseCaseTests.cs.meta`
- `Docs/Features/MonstersAndProjectiles.md`
- `Docs/Work/Active/W-20260819-002-world-monster-spawn-candidate-usecase.md`

## 예약 Scene·Prefab·Data Asset

없음. Scene, Prefab, ScriptableObject, Fusion config, ProjectSettings와 Package는 변경하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- `ProjectIO.Monsters` 순수 assembly에 Unity·Fusion 타입을 참조하지 않는 후보 상태와 자격 정책을 추가한다.
- 새 `ProjectIO.Monsters.UseCases` 순수 assembly는 `ProjectIO.Monsters` Logic만 참조하며, 후보 목록·refresh Spawn 한도·선택 결과 index 목록을 입력·출력으로 사용한다.
- `StageBootstrapper`, `WorldMonsterSpawnSystem.SpawnMonsters`, `SpawnRecord`, `Runner.Spawn`, prefab 초기화 callback, State Authority와 Spawn/Despawn 소유권은 변경하지 않는다.
- RPC, `[Networked]` 상태, AOI, Late Join 복원 소스와 직렬화 필드는 추가하거나 변경하지 않는다.

## 네트워크 경계

- authoritative state와 World Monster `NetworkObject` 수명주기 소유자는 기존과 동일하게 `WorldMonsterSpawnSystem`의 State Authority다.
- UseCase는 Host/State Authority의 `RefreshChunkStreaming` 내부에서만 호출되며 NetworkObject나 Runner를 알지 못한다.
- Client와 권한 없는 호출은 기존 `OnSetUp`, `SpawnMonsters`, `FixedUpdateNetwork` Authority guard를 통과하지 못한다.
- 선택 결과는 현재 refresh에서 기존 `SpawnRecord`를 한 번 호출할 record index만 제공하며 지속 네트워크 상태를 소유하지 않는다.

## 다른 활성 작업과 겹치는 부분

없음. 확인 시 `Docs/Work/Active/`에는 안내용 `README.md`만 존재한다.

## 범위 밖

- 초기 Spawn 위치 샘플링과 Sand Tomb 제외 영역 판정
- Chunk index, dormant 이동, active record Despawn, Territory 포함 판정 규칙 변경
- Track Monster, Monster AI, Projectile, Sacred Zone, Sanctuary 변경
- Fusion `Runner.Spawn`/`Runner.Despawn`, Authority, AOI, Late Join, Scene 수명주기 변경
- Scene, Prefab, ScriptableObject, Bootstrapper, ProjectSettings, Package 변경
- 다른 Monster 소비자 또는 Spawn 흐름 전환

## 완료 조건

- 순수 정책은 destroyed, 이미 active, Territory 내부, active Chunk 범위 밖 record를 Spawn 후보에서 제외한다.
- UseCase는 기존 주변 record 순서를 유지하면서 `maxSpawnsPerRefresh` 이하를 선택하고, 같은 안정 record ID를 한 refresh에 한 번만 반환한다.
- `WorldMonsterSpawnSystem`은 후보 상태를 순수 값으로 변환하고 선택된 index만 기존 `SpawnRecord`에 전달한다.
- active Chunk이지만 refresh 예산에서 탈락한 record는 기존처럼 dormant 이동하지 않고 다음 refresh를 기다린다.
- inactive Chunk record의 dormant 이동·Territory 파괴 판정은 기존과 동일한 Legacy 경로에서 한 번만 실행된다.
- 실제 `Runner.Spawn` 호출 위치와 횟수 소유자는 기존 `SpawnRecord` 하나뿐이며 새 경로가 직접 Spawn하지 않는다.
- Host에서 선택된 record만 Spawn되고, Client·권한 없는 경로에서는 Spawn이 없으며 같은 record의 중복 Spawn이 없다.
- 순수 Logic·UseCase 집중 테스트, 관련 assembly compile, 가능한 Unity script compile, `git diff --check`를 통과한다.
- 롤백은 `RefreshChunkStreaming`을 기존 내부 후보 판정 loop로 되돌리고 새 Logic·UseCase·테스트를 제거하는 것으로 한정한다.
- 실제 Host·Client·Late Join runtime 확인이 불가능하면 환경 제한과 남은 수동 테스트를 기록한다.

## 실제 변경

예약 단계. 구현 전.

## 검증 결과

예약 단계. 구현 전.

## 남은 위험

- 후보 선택과 dormant 처리의 순서가 달라지면 refresh당 Spawn 예산 또는 Territory 진입 record 처리 결과가 바뀔 수 있으므로, index 매핑과 단일 Spawn 호출을 집중 테스트 및 정적 검사로 고정해야 한다.
