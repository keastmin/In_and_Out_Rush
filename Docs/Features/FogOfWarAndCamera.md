# Fog of War and Camera

Status: Current

Last reviewed: 2026-08-29

## 책임

로컬 플레이어 시야와 Territory 기반 Fog 마스크, 숨김 대상 표시, Runner·Builder 역할별 Cinemachine 카메라를 관리한다.

## 주요 진입점

- `FogOfWarSystem`, `FogOfWarRuntimeDriver`
- `FogOfWarMaskRenderer`, `FogOfWarTerritoryVisibility`
- `FogOfWarHiddenObjectController`, `WorldMonster.Spawned/Despawned`
- `CinemachineSystem`
- `PlayerRunnerCinemachineController`, `PlayerBuilderCinemachineController`

## 상태 경계

Fog와 Camera는 로컬 표현이다. Territory와 플레이어 위치를 읽지만 authoritative 게임 상태를 변경하지 않는다.

WorldMonster는 각 Peer의 Fusion `Spawned`/`Despawned`에서 로컬 Fog registry에 표시 root를
등록·해제한다. 이 연결은 Networked 상태나 RPC를 만들지 않으며, Fog는 registry revision이
바뀔 때만 해당 root 아래의 Renderer와 Canvas 목록을 다시 구성한다.

Territory mesh의 월드 삼각형과 가시성 빠른 거절 자료는 mesh 또는 transform이 바뀔 때만
재구성한다. Territory mask draw와 blur는 Territory geometry, visible range 또는 world bounds가
바뀔 때만 실행하고, 안정 frame에는 캐시된 Territory mask와 로컬 Runner brush를 합성한다.
Overlay mesh도 bounds나 grid 높이가 바뀔 때만 다시 쓴다.

## 관련 Asset

`GamePresentation.unity`의 Main Camera, UI Camera, Fog of War prefab, Cinemachine cameras.

## 변경 시 확인

- Additive Scene에서 Territory mesh 참조 주입
- Main Camera와 역할별 target 준비 순서
- 비활성 Fog prefab 탐색
- 로컬 표현에 불필요한 Networked 상태가 추가되지 않는지
- Host와 Client 각각에서 WorldMonster spawn/AOI 진입/Despawn 뒤 registry 표시와 정리
- Profiler의 `FogOfWar.Update`, `FogOfWar.RegistrySync`, `FogOfWar.HiddenVisibility`,
  `FogOfWar.TerritoryMaskUpdate` marker와 안정 frame의 GC Alloc

## 기술 부채

WorldMonster가 아닌 새 Fog-hidden 동적 타입을 추가할 때는 해당 타입도 로컬 Fog registry에
표시 root를 등록·해제해야 한다. GPU pass와 Player build의 GC 개선 폭은 실제 Host·Client
Profiler 캡처로 계속 추적한다.
