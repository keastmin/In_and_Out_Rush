# Ping

Status: Current

Last reviewed: 2026-08-14

## 책임

Runner의 핑 요청을 네트워크로 전달하고 다른 플레이어가 관찰할 수 있는 핑 오브젝트와 로컬 가이드를 관리한다.

## 주요 진입점

- `Assets/02_Scripts/Ping System/PingSystem.cs`
- `PlayerPing`
- `PlayerRunnerPingGuide`
- `StageBootstrapper.InitializePingSystem`

## 관련 Asset

`GameWorld.unity`의 Ping System, Ping Direction Guide와 관련 prefab.

## 변경 시 확인

- 요청 권한과 Spawn Authority
- 핑 수명과 Despawn
- Runner guide가 로컬 플레이어에만 연결되는지
- 재접속·Scene 전환 후 정적 참조가 남지 않는지

## 기술 부채

초기화가 StageBootstrapper와 PlayerRunner 생성 순서에 의존한다.
