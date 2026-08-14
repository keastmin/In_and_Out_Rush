# Fog of War and Camera

Status: Current

Last reviewed: 2026-08-14

## 책임

로컬 플레이어 시야와 Territory 기반 Fog 마스크, 숨김 대상 표시, Runner·Builder 역할별 Cinemachine 카메라를 관리한다.

## 주요 진입점

- `FogOfWarSystem`, `FogOfWarRuntimeDriver`
- `CinemachineSystem`
- `PlayerRunnerCinemachineController`, `PlayerBuilderCinemachineController`

## 상태 경계

Fog와 Camera는 로컬 표현이다. Territory와 플레이어 위치를 읽지만 authoritative 게임 상태를 변경하지 않는다.

## 관련 Asset

`GamePresentation.unity`의 Main Camera, UI Camera, Fog of War prefab, Cinemachine cameras.

## 변경 시 확인

- Additive Scene에서 Territory mesh 참조 주입
- Main Camera와 역할별 target 준비 순서
- 비활성 Fog prefab 탐색
- 로컬 표현에 불필요한 Networked 상태가 추가되지 않는지

## 기술 부채

Fog가 런타임에 Renderer와 Canvas를 넓게 검색한다. 규모가 커지면 등록 기반 경계를 검토한다.
