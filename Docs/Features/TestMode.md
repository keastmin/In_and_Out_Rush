# Test Mode

Status: Current

Last reviewed: 2026-08-14

## 책임

Lobby를 거치지 않고 게임 Scene에서 Host Runner, ResourceManager, NetworkManager를 구성하고 개발용 조작을 제공한다.

## 주요 진입점

- `Assets/02_Scripts/Test Mode/TestModeGameSceneSetup.cs`
- `PlayerRunnerTestModeGUI`
- `ResourceSystemTestInput`

## 관련 Asset

`GameRoot.unity`의 Game Scene Setup prefab과 Runner·NetworkManager·ResourceManager prefab.

## 변경 시 확인

- 기존 Lobby 세션이 있을 때 중복 환경을 만들지 않는지
- Additive 세 Scene이 필요한 테스트에서 올바른 Scene 집합을 여는지
- Development·Editor 전용 기능이 Release 게임 규칙에 영향을 주지 않는지
- 테스트 종료 후 Runner와 DontDestroyOnLoad 객체 정리

## 기술 부채

단일 `GameRoot`를 직접 실행하면 World와 Presentation 준비를 보장하지 않는다. Additive 테스트 시작 절차를 별도 자동화할 필요가 있다.
