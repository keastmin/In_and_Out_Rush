# W-20260910-002 트랙 변형과 트랙 몬스터 스폰 개편

Status: Reserved

## 동기화 기준

- Base Commit: eae857c1f3e00c845096f0692abba614fb9ef479
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

- Track and Rounds
- Monsters and Projectiles
- Grid·Obstacle의 Track segment 소비

## 목표

- 기존 타원 확대 경로를 1차 타원, 2차 중앙 직선, 3차 중앙 수직 교차 직선의 단계형 Track으로 완전히 교체한다.
- 라운드 3·7 종료 정산 뒤 Track을 변형하고 라운드 9 종료 정산 뒤 모든 Track Monster에 영구 1.5배 이동 속도를 한 번 적용한다.
- 3차 Track의 구간 순간이동과 정예 반복 순환, Territory 밖 추가 1.5배 가속을 State Authority에서 처리한다.
- Spawn Group을 유형 enum 기반으로 바꾸고 Normal 한 unit을 Small 3, Medium 1, Large 1 순서로 생성한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/TrackAndRounds.md`
- `Docs/Features/MonstersAndProjectiles.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/replace-existing-feature/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Track/Track.cs`
- `Assets/02_Scripts/Track/TrackSystem.cs`
- `Assets/02_Scripts/Track/Local/TrackVisible.cs`
- `Assets/02_Scripts/Features/Track/Logic/ProjectIO.Tracks.asmdef`
- `Assets/02_Scripts/Features/Track/Logic/TrackStage.cs`
- `Assets/02_Scripts/Features/Track/Logic/TrackAxis.cs`
- `Assets/02_Scripts/Features/Track/Logic/TrackPath.cs`
- `Assets/02_Scripts/Features/Track/Logic/TrackSegment.cs`
- `Assets/02_Scripts/Features/Track/Logic/TrackGeometryGenerator.cs`
- `Assets/02_Scripts/Features/Track/Logic/TrackTraversalAction.cs`
- `Assets/02_Scripts/Features/Track/Logic/TrackTraversalPolicy.cs`
- `Assets/02_Scripts/Features/Track/Tests/ProjectIO.Tracks.Tests.asmdef`
- `Assets/02_Scripts/Features/Track/Tests/TrackGeometryGeneratorTests.cs`
- `Assets/02_Scripts/Features/Track/Tests/TrackTraversalPolicyTests.cs`
- `Assets/02_Scripts/Monster/TrackMonster.cs`
- `Assets/02_Scripts/Monster/TrackMonsterSpawnSystem.cs`
- `Assets/02_Scripts/Monster/TrackMonsterWaveSpawnTable.cs`
- `Assets/02_Scripts/Features/Monster/Logic/TrackMonsterSpawnType.cs`
- `Assets/02_Scripts/Features/Monster/Logic/TrackMonsterNormalSize.cs`
- `Assets/02_Scripts/Features/Monster/Logic/TrackMonsterFormationPolicy.cs`
- `Assets/02_Scripts/Features/Monster/Tests/TrackMonsterFormationPolicyTests.cs`
- `Assets/02_Scripts/Grid/InfiniteGrid.cs`
- `Assets/02_Scripts/Grid/InfiniteGridTrackBlockedCellCollector.cs`
- `Assets/02_Scripts/Grid/InfiniteGridTrackOverlapTester.cs`
- `Assets/02_Scripts/Grid/InfiniteGridObstacleSpawner.cs`
- `Assets/02_Scripts/Stage/Network/StageBootstrapper.YOU.cs`
- 위 신규 폴더·파일의 대응 `.meta` 파일

## 예약 Scene·Prefab·Data Asset

- `Assets/01_Scenes/GameWorld.unity`
- `Assets/01_Scenes/GameScene.unity`
- `Assets/03_Prefabs/Field/Track.prefab`
- `Assets/08_Data/TrackMonster/Track Monster Wave Spawn Table.asset`

## 공용 계약 또는 Bootstrapper 변경

- `TrackSystem.OnTrackChanged`를 `Vector3[]` 대신 완성된 `Track`을 전달하는 계약으로 교체한다.
- `Track`은 여러 `TrackPath`와 명시적 `TrackSegment`를 제공하며 Grid·Obstacle 소비자는 이 segment만 사용한다.
- `StageBootstrapper`가 맵 중심과 반지름을 TrackSystem에 제공하고 정산 완료 뒤 변형·가속·보류 Spawn을 조립한다.
- `TrackMonsterSpawnGroup`은 직접 Prefab 대신 `TrackMonsterSpawnType`과 spawn unit 수를 제공한다.

## 네트워크·Peer 동등성

- 사용자 입력은 없으며 기존 Host 라운드 이벤트가 변형과 Spawn의 원점이다.
- Track 단계, 초기 seed, 주축, 시작·도착 반전, 중심, 직선 길이와 revision은 `TrackSystem`의 State Authority만 변경한다.
- Persistent Track 상태는 Networked property로 복제하며 Host와 Client는 같은 파라미터로 로컬 표현·Grid 차단 상태를 재구성한다. 기존 RPC-only 꼭짓점 동기화는 제거한다.
- Track Monster Spawn, 이동, 구간 Teleport, Runner 피해, 정산과 Despawn은 State Authority가 한 번만 실행하고 Client는 NetworkObject·NetworkRigidbody 결과를 관찰한다.
- Host 로컬과 Client는 동일한 Track 단계·두 선 표시·몬스터 위치와 Runner 체력 결과를 보며 권위 없는 Peer는 Track을 변형하거나 몬스터 상태를 바꿀 수 없다.
- Late Join은 현재 Networked Track 상태와 살아 있는 NetworkObject에서 복원한다. 정산 coroutine, 보류 Spawn, Track 이벤트 구독은 Scene unload와 Despawn에서 중복 실행 없이 정리한다.
- 실제 Host·Client 실행이 불가능하면 Host 1명과 Client 1명으로 1→2→3차 표시, Late Join, 2→3 Teleport, 정예 반복, 라운드 정산, 피해 단일 실행을 확인하는 수동 절차를 남긴다.

## 다른 활성 작업과 겹치는 부분

없음. `CheckStart` 시 `Docs/Work/Active/README.md` 외 Active 예약이 없었다.

## 범위 밖

- Territory 확장·판정 구현 자체 변경
- 월드 몬스터 이동·Spawn 규칙 변경
- 정예·보스의 신규 웨이브 밸런스 설계
- 라운드 3·7·9 및 기존 강화 라운드 일정 변경

## 완료 조건

- 1차 타원은 기존 크기·노이즈를 유지하고 2·3차 직선은 맵 중심, 지름 1/8 길이, 직각 관계를 만족한다.
- 분리된 Path 사이에 표시·Grid·Obstacle용 가짜 segment가 생성되지 않는다.
- 3차에서 일반·보스는 최종 피해 후 제거되고 정예는 라운드 안에서 반복하지만 라운드 종료 정산에는 포함된다.
- 기존 웨이브 그룹은 수량·지연·반복을 보존한 채 모두 `ElitePredator`로 전환된다.
- Normal unit과 특수 unit, 내재화 정확 수량 Spawn이 지정된 테이블 Prefab을 사용한다.
- 영구·Territory 밖 속도 배율이 독립적으로 곱해져 동시 적용 시 2.25배가 되고 기존 강화 배율도 유지된다.
- 집중 EditMode 테스트, Unity/프로젝트 컴파일, 직렬화 참조 검사, `git diff --check`를 수행하고 Host·Client 미실행 항목은 명시한다.

## 실제 변경

예약 단계. 구현 전.

## 검증 결과

예약 단계. 미실행.

## 남은 위험

- Track의 새 Networked 상태는 Fusion CodeGen과 실제 Late Join에서 확인해야 한다.
- 두 Scene과 Track Prefab의 YAML 직렬화 변경은 Unity import 후 누락 참조 검사가 필요하다.
- 롤백은 이 예약의 구현 commit을 되돌려 기존 `ExpandTrack`, RPC vertex sync, 그룹별 Prefab 경로를 복원하는 방식으로 수행한다.
