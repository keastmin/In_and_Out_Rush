# Player Runner

Status: Current

Last reviewed: 2026-08-22

## 책임

Runner의 이동, 체력·스태미나, 전투, 회피, 버프, 순간이동과 네트워크 입력을 관리한다.

## 주요 진입점

- `Assets/02_Scripts/Player/Player Runner/Network/PlayerRunner.cs`
- `PlayerRunnerMovement`, Combat·Slide·Tumble·Buff handler
- `NetworkInputSystem`, `NetworkInputData`

## 상태와 권한

Networked 플레이어 상태는 Fusion Authority 규칙을 따른다. 로컬 Camera·HUD 표현은 네트워크 상태와 분리한다.

## 주요 연결

Territory 보호 판정, Monster·Projectile 피해, Laboratory 상호작용, Runner UI, Cinemachine, Ping.

`PlayerRunner.Render`의 기존 `OnPositionChanged`는 Territory Trail의 입력 seam이다.
Input Authority peer는 이 이벤트로 local fixed Trail을 즉시 예측 표시하고,
State Authority는 같은 이동을 검증해 confirmed stream을 만든다. PlayerRunner가
Territory 결과나 Trail network state를 직접 소유하지는 않는다.

## 관련 Asset

- Player Runner prefab
- `GamePresentation.unity`의 Runner camera와 UI

## 변경 시 확인

- Input Authority와 State Authority 구분
- Host·Client 이동과 피해 결과
- Spawn 시 StageBootstrapper 준비 상태
- 사망·Despawn 후 이벤트와 registry 정리
- Host/Client Input Authority에서 owner Trail의 즉시 표시와 State Authority
  terminal 뒤 정리 동등성

## 기술 부채

주 PlayerRunner 클래스가 여러 handler를 조정하고 전역 Stage 참조도 사용한다. 흐름별로 공개 연결부를 줄여야 한다.
