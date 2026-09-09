# W-20260907-001 자원 오브젝트 Host·Client 표시 복구

Status: Complete

## 동기화 기준

- Base Commit: 06d9b348b2b81cd47477699f915880bd3c9cf9cf
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

- Codex `/root`

## 기능

- Resource Spawn
- Fog of War 로컬 표시

## 목표

- `Resource` 레이어로 이동한 네트워크 자원 오브젝트가 현재 게임 카메라에서 렌더링되도록 복구한다.
- Host 로컬과 Client 프록시 모두 자원 Spawn·Late Join·Despawn 수명에 맞춰 Fog of War 숨김 대상에 등록·해제되도록 한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/ResourceSpawn.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Resource/Network/ResourceVisible.cs`

## 예약 Scene·Prefab·Data Asset

- `Assets/01_Scenes/GamePresentation.unity`
- `Assets/01_Scenes/GameScene.unity`

## 공용 계약 또는 Bootstrapper 변경

- 없음. 기존 `ResourceVisible`, Fusion Spawn, AOI, Fog of War 레지스트리 계약을 그대로 사용한다.

## 네트워크·Peer 동등성

- 입력 원점: 플레이어 입력과 무관한 표시 결함이며, 자원 생성·수집 의도 경로는 변경하지 않는다.
- State Authority: 기존처럼 Host가 `ResourceSpawnSystem`에서 자원 NetworkObject Spawn·수집·Despawn과 AOI 관심을 결정한다.
- 복제·결과 경로: Fusion NetworkObject 수명과 AOI가 Client에 자원 존재를 복제한다. 각 Peer의 `ResourceVisible.Spawned`가 로컬 Fog of War 표시 등록을 수행하고 `Despawned`가 해제한다.
- Host·Client 체감: 두 Peer의 게임 카메라가 `Resource` 레이어를 렌더링하고, 각 Peer의 로컬 러너 시야 또는 Territory 공개 영역에서 같은 자원 표시 규칙을 적용한다.
- 중복 실행·준비 상태·Late Join·정리: 레지스트리는 동일 Transform 중복 등록을 무시한다. Late Join 프록시의 `Spawned`에서도 등록하며 Despawn 시 해제한다. State Authority mutation은 추가하지 않는다.
- 런타임 검증: 자동 컴파일과 직렬화 마스크 검증 후, 실제 Host·Client에서 러너 주변 자원 표시, 원거리 Fog 숨김, Territory 공개, Late Join, 수집 Despawn을 각각 확인한다. 멀티 Peer 실행을 이 환경에서 완료하지 못하면 수동 절차와 미검증 상태를 기록한다.

## 다른 활성 작업과 겹치는 부분

- `Docs/Work/Active/`에는 안내용 `README.md` 외 예약이 없어 겹침 없음.

## 범위 밖

- 자원 배치 수량·위치 알고리즘
- 자원 경제 수치와 수집 보상
- AOI 반경·Fog 시야 반경의 밸런스 변경
- 자원 프리팹 외형·머티리얼 변경

## 완료 조건

- 현재 Additive 게임 경로와 Legacy `GameScene`의 Main Camera가 `Resource` 레이어를 렌더링한다.
- Host와 Client에서 Spawn된 `ResourceVisible`이 Fog of War 레지스트리에 등록되고 Despawn 시 정리된다.
- 기존 Host 권위 Spawn·수집·AOI 계약에 변화가 없다.
- 가능한 집중 검증, 프로젝트 컴파일, `git diff --check`, `git status`를 수행하고 실제로 못 한 Host·Client 런타임 검증을 명시한다.

## 실제 변경

- `GamePresentation.unity`와 Legacy `GameScene.unity`의 Main Camera Culling Mask에 `Resource` 레이어 비트(11)를 추가했다.
- `ResourceVisible.Spawned`에서 각 Peer의 Fog of War 숨김 대상 레지스트리에 자원 Transform을 등록한다.
- `ResourceVisible.Despawned`에서 등록을 해제해 수집·Scene 종료 뒤 파괴된 자원 참조가 남지 않게 했다.
- 자원 Spawn, State Authority, AOI 반경, 배치·수집 로직은 변경하지 않았다.

## 검증 결과

- `dotnet restore ProjectIO.slnx`: 성공. 최초 `--no-restore` 빌드는 `Temp/obj/*/project.assets.json` 부재로 중단되어 복원 후 재실행했다.
- `dotnet build ProjectIO.slnx --no-restore`: 성공, 오류 0개, 기존 코드·패키지 경고 25개.
- 정적 Scene 검증: `GamePresentation.unity`, `GameScene.unity` Main Camera 마스크가 모두 `3031`이며 `Resource` 레이어 비트 `2048`을 포함한다.
- 정적 Prefab 검증: 여섯 개 개별 네트워크 자원 프리팹이 모두 레이어 11을 포함한다.
- `git diff --check`: 통과. 줄 끝 변환 예고 외 공백 오류 없음.
- 실제 Host·Client 멀티 Peer 실행은 이 환경에서 수행하지 않아 런타임 Peer 동등성은 미검증이다.
- 작업자의 최종 Commit·Push 진행 요청을 수신했다. Host·Client 런타임 결과의 구체적인 로그나 캡처는 전달되지 않아 독립 검증 상태는 변경하지 않았다.

## 남은 위험

- Host와 Client 각각에서 러너 시야 안 자원 표시, 시야 밖 Fog 숨김, Territory 공개, Client Late Join, 수집 Despawn을 실제 플레이로 확인해야 한다.
