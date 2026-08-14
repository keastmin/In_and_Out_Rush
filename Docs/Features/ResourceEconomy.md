# Resource Economy

Status: Current

Last reviewed: 2026-08-14

## 책임

공유 광물·가스 네트워크 상태와 비용 충분성·차감, UI 갱신 연결을 관리한다.

## 주요 진입점

- `Assets/02_Scripts/System/ResourceSystem.cs`
- `Assets/02_Scripts/Tower/Cost.cs`
- `ResourceInfoUI`, `ResourceView`

## 상태와 권한

`ResourceSystem`의 Networked Mineral·Gas가 기준 상태다. 변경 요청과 비용 차감은 State Authority 경로를 확인해야 한다.

## 주요 소비자

PlayerBuilder 타워 건설·이동·판매, Laboratory 공급과 업그레이드, Stage UI.

## 관련 Asset

- `Core.prefab`
- `GamePresentation.unity`의 Resource UI

## 변경 시 확인

- 비용 차감의 권한과 원자성
- Host와 Client UI 갱신
- 테스트 자원 지급이 Development/Test Mode에만 한정되는지

## 기술 부채

동일한 이름의 Local·Network ResourceSystem이 존재하므로 문서와 코드에서 namespace를 명확히 해야 한다.
