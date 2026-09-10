# W-20260829-003 Fog of War 주기 비용 최적화

Status: Completed

## 동기화 기준

- Base Commit: c98a5620ed6c9d624d0ae56d2481a503e30da54a
- 공용 Upstream: `origin/rebuild-development-environment`

## 담당자

Codex 메인 에이전트.

## 기능

Fog of War 로컬 마스크 렌더링, Territory 기반 숨김 판정과 WorldMonster 표시 등록.

## 목표

- `FogOfWarSystem.LateUpdate`의 0.15초 주기 전체 `Renderer`/`Canvas` 검색과 부모
  `MonoBehaviour` 반복 검색을 제거한다.
- 각 Peer의 WorldMonster Fusion 수명주기에서 로컬 표시 root만 등록·해제하고, Fog는 등록
  revision이 바뀔 때만 실제 Renderer·Canvas 목록을 다시 만든다.
- Territory mesh의 world-space triangle과 빠른 reject 자료를 mesh/transform 변경 시에만
  준비해, 숨김 대상마다 반복하던 local-to-world 변환과 불필요한 exact triangle 검사를 줄인다.
- Territory mask 생성·blur는 Territory mesh, transform, world bounds 또는 visible range가
  바뀔 때만 수행하고, 매 frame에는 cached mask와 Runner brush만 합성한다.
- Overlay mesh geometry는 world bounds 또는 높이가 바뀔 때만 갱신해 매 frame 배열 할당,
  `Mesh.Clear`와 bounds 재계산을 제거한다.
- 전역 검색, mask 갱신, 숨김 판정 비용을 분리하는 Profiler marker와 warm-up 후 allocation
  확인 경계를 남긴다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/FogOfWarAndCamera.md`
- `manage-feature-work`
- `photon-fusion-feature`

## 예상 수정 코드

- `Assets/02_Scripts/Fog of War/FogOfWarSystem.cs`
- `Assets/02_Scripts/Fog of War/KIM/FogOfWarRuntimeDriver.cs`
- `Assets/02_Scripts/Fog of War/KIM/FogOfWarMaskRenderer.cs`
- `Assets/02_Scripts/Fog of War/KIM/FogOfWarTerritoryVisibility.cs`
- `Assets/02_Scripts/Fog of War/KIM/FogOfWarHiddenObjectController.cs`
- `Assets/02_Scripts/Monster/WorldMonster.cs`
- 필요한 경우 `Assets/02_Scripts/Fog of War/Tests/` 아래 집중 Editor test와 대응 `.meta`
- `Docs/Features/FogOfWarAndCamera.md`
- `Docs/Work/Active/W-20260829-003-fog-of-war-performance.md`

## 예약 Scene·Prefab·Data Asset

없음. `GamePresentation.unity`, Fog prefab, WorldMonster prefab과 다른 Scene·Prefab의 직렬화
참조는 변경하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- `StageBootstrapper.InitializeFogOfWarSystem`, `FogOfWarSystem.SetTerritorySource`,
  `SetWorldBounds`의 공개 연결과 호출 순서를 유지한다.
- Fog layer, Runner/Territory visible range, density와 `IFogOfWarAlwaysVisible` 의미를 유지한다.
- 현재 Fog-hidden layer 6의 동적 대상은 WorldMonster 계열이며 TrackMonster는
  `IFogOfWarAlwaysVisible` 계약대로 숨기지 않는다.
- WorldMonster의 authoritative AI, 피해, Spawn/Despawn 결정과 Territory 소비자 계약은
  변경하지 않는다. 각 Peer에서 Fog 표시 root를 등록·정리하는 로컬 presentation hook만
  추가한다.

## 네트워크·Peer 동등성

- 새 Networked 상태, RPC, Authority 변경, Spawn/Despawn 요청 또는 AOI 규칙은 없다.
- State Authority의 기존 WorldMonster Spawn/Despawn 결과를 각 Peer가 Fusion 수명주기로
  관찰한 뒤, `Spawned`에서는 로컬 Fog registry에 root를 한 번 등록하고 `Despawned`에서는
  제거한다. Host의 서버·로컬 역할이 표시 등록을 중복 실행해도 registry 결과는 한 건이어야 한다.
- Client가 AOI에 들어오거나 늦게 NetworkObject를 받으면 해당 Peer의 `Spawned` 이후 다음 Fog
  update에서 Renderer·Canvas가 등록되어야 한다. AOI 이탈, Despawn, disconnect와 Scene unload
  뒤에는 registry와 원래 enabled 상태가 남지 않아야 한다.
- Fog는 로컬 표현이므로 권위 mutation이나 요청/결과 RPC가 없다. Host와 Client는 각자 복제된
  Territory mesh와 로컬 Runner 위치로 같은 숨김 규칙을 계산한다.
- 실제 Host·Client 실행이 불가능하면 WorldMonster spawn, AOI enter/exit, Despawn, Territory
  확장 전후의 표시와 registry 정리를 작업자가 확인할 수 있는 수동 절차를 기록한다.

## 다른 활성 작업과 겹치는 부분

없음. `CheckStart` 시 `Docs/Work/Active/`에는 안내용 `README.md`만 존재했다.

## 범위 밖

- Fog shader 디자인, mask 해상도, visible range와 density 값 변경
- Territory Polygon·공간 인덱스·확장 복제 변경
- WorldMonster AI, Spawn 수량·streaming cadence, Fusion AOI 정책 변경
- Scene·Prefab·ScriptableObject 수정
- Camera/Cinemachine 동작 변경

## 완료 조건

- runtime Fog 경로에서 `FindObjectsByType<Renderer/Canvas>`와 매 refresh 부모 component 전수
  검색이 제거되고, 동적 WorldMonster 등록·해제가 current registry revision에 반영된다.
- Territory가 변하지 않는 frame에는 Territory polygon GL draw와 blur pass를 다시 만들지 않고,
  Overlay mesh도 geometry가 변하지 않으면 다시 쓰지 않는다.
- 숨김 판정은 기존 Runner 원형 시야, Territory triangle 내부와 visible-range edge 의미를 유지한다.
- warm-up 뒤 안정 frame의 Fog update가 managed allocation을 만들지 않고, topology/Territory
  변경 비용이 별도 Profiler marker로 구분된다.
- `dotnet build Assembly-CSharp.csproj`, 가능한 집중 test, `git diff --check`가 통과한다.
- Host와 Client에서 WorldMonster spawn/Despawn, TrackMonster always-visible, Territory 확장,
  Additive Scene teardown을 확인하거나 정확한 미검증 절차와 위험을 기록한다.

## 실제 변경

- `WorldMonster.Spawned`/`Despawned`가 각 Peer의 정적 로컬 registry에 root를 중복 없이
  등록·해제하고, 파괴된 root는 다음 registry 동기화에서 정리하도록 했다.
- Fog runtime의 전체 `FindObjectsByType<Renderer/Canvas>`를 제거했다. 각 Fog driver는 registry
  revision 또는 hidden layer mask가 바뀔 때만 등록 root의 Renderer·Canvas를 재구성하고,
  기존 enabled 상태를 복원·보존한다.
- Territory mesh/transform 변경 때 월드 삼각형, 삼각형 AABB와 전체 AABB를 캐시한다. 0.15초
  숨김 판정은 Runner 원형 시야를 먼저 검사하고, Territory AABB 빠른 거절 뒤 기존 내부/edge
  거리 판정을 수행한다.
- Territory 전용 캐시 RenderTexture를 추가했다. Territory geometry, visible range 또는 world
  bounds가 바뀔 때만 polygon draw와 2-pass blur를 갱신하고, 안정 frame에는 캐시 mask 복사와
  Runner brush 합성만 수행한다.
- Overlay mesh의 vertex/triangle 배열을 재사용하며 world bounds 또는 grid 높이가 바뀔 때만
  `Mesh.Clear`와 geometry 쓰기를 수행하고 bounds를 직접 설정한다.
- `FogOfWar.Update`, `FogOfWar.RegistrySync`, `FogOfWar.HiddenVisibility`,
  `FogOfWar.TerritoryMaskUpdate` Profiler marker를 추가했다.
- Fog 안정 frame의 `Mathf.Max(params)` 배열 할당을 중첩 2항 호출로 바꿨다.

## 검증 결과

- `CheckStart`: `READY_TO_CHECK_CONFLICTS`, `AHEAD=0`, `BEHIND=0`, base `c98a562`.
- Active 충돌: 없음.
- 읽기 전용 조사에서 현재 layer 6 Fog-hidden prefab은 WorldMonster 계열이며 TrackMonster는
  `IFogOfWarAlwaysVisible`로 제외되는 구조를 확인했다.
- `dotnet build Assembly-CSharp.csproj --no-restore`: 성공, 오류 0. 출력된 13개 경고는 기존
  third-party obsolete API, 미사용 field와 기존 Unity message signature 경고다.
- `dotnet build ProjectIO.slnx`: 성공, 오류 0. 출력된 9개 경고는 기존 Photon/third-party 및
  Editor obsolete API 경고다.
- 정적 확인: Fog runtime 경로의 `FindObjectsByType`, `Transform.hasChanged` 조작과 매 frame
  overlay 배열 생성이 제거됐다.
- `git diff --check`: 최종 문서 갱신 뒤 다시 실행한다.
- 실제 Host·Client Player Profiler와 AOI/late-join 실행 검증은 이 환경에서 수행하지 못했다.

## 남은 위험

- 새 Fog-hidden 타입이 WorldMonster 밖에서 추가되면 해당 타입도 동일한 local registry 계약에
  연결해야 한다.
- GPU pass 감소와 CPU/GC 개선은 실제 Player Profiler에서 전후 frame을 비교해야 확정할 수 있다.
- Host와 Client에서 WorldMonster spawn/AOI 진입/Despawn, TrackMonster always-visible, Territory
  확장 전후 mask, Additive Scene teardown 뒤 원래 Renderer·Canvas enabled 복원을 확인해야 한다.
