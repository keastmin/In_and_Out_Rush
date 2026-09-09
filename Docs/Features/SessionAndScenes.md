# Session and Scenes

Status: Current

Last reviewed: 2026-08-14

## 책임

Lobby에서 Host·Client 세션을 만들고 역할을 등록하며, Host가 게임 시작 시 Fusion을 통해 Additive 게임 Scene을 로드한다.

## 책임지지 않는 것

Stage 내부 시스템 초기화와 게임 규칙은 `StageInitialization.md`가 담당한다.

## 주요 진입점

- `Assets/02_Scripts/Network/Fusion Test/Start Scene Scripts/MatchMaker.cs`
- `NetworkManager`, `PlayerRegistry`, `LobbyUI`, `InterfaceManager`
- `MatchMaker.OnClickStartButton`, `OnSceneLoadDone`

## 실행 구조

Host가 `GameWorld`를 Single로 로드한 뒤 `GamePresentation`, `GameRoot`를 Additive로 로드한다. `GameScene`은 현재 Legacy·비교용으로 Build Settings에 남아 있다.

## 관련 Asset

- `Assets/01_Scenes/LobbyScene.unity`
- `Assets/01_Scenes/GameWorld.unity`
- `Assets/01_Scenes/GamePresentation.unity`
- `Assets/01_Scenes/GameRoot.unity`
- Runner, NetworkManager, ResourceManager prefab

## 변경 시 확인

- Host만 Scene Authority 경로를 시작하는지
- Client에 세 Scene이 모두 복제되는지
- 세션 종료 후 Lobby 복귀와 DontDestroyOnLoad 객체가 정리되는지
- Build Settings의 Scene 경로와 index가 유효한지

## 기술 부채

Scene 이름과 경로가 `MatchMaker` 상수에 묶여 있고, Legacy `GameScene` 제거 시점을 아직 결정하지 않았다.
