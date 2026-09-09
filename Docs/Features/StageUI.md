# Stage UI

Status: Current

Last reviewed: 2026-08-14

## 책임

Runner·Builder HUD, 타이머, 자원, Tower·Laboratory UI와 로컬 입력 전달을 담당한다.

## 주요 진입점

- `Assets/02_Scripts/Stage UI/StageUIController.cs`
- `PlayerBuilderUI`, `PlayerRunnerUI`
- Builder Main UI, Laboratory UI, Resource UI, Timer UI
- `Assets/02_Scripts/UI/`

## 책임지지 않는 것

게임 규칙, 비용 차감, 네트워크 상태 변경을 소유하지 않는다. 사용자 의도를 실제 시스템에 전달한다.

## 관련 Asset

`GamePresentation.unity`의 Canvas와 EventSystem, UI prefab.

## 변경 시 확인

- 역할별 UI 활성화
- 비활성 UI의 초기화와 참조
- 이벤트 listener 중복과 Scene unload 정리
- UI가 다른 시스템 의존성의 전달 통로가 되지 않는지

## 기술 부채

일부 UI가 구체적인 게임 시스템과 직접 연결되어 있다. 소비 기능의 공개 명령으로 점진적으로 좁혀야 한다.
