# Sacred Zone, Sanctuary and Gate

Status: Current

Last reviewed: 2026-08-20

## 책임

Sacred Zone 진행, Sanctuary 생성과 몬스터 내재화, Gate 생성·진입 및 승패 흐름을 관리한다.

## 주요 진입점

- `SacredZoneSystem`, `SacredZoneView`
- `SanctuaryView`
- `Gate`, `CollisionField`
- `StageBootstrapper.YOU`의 생성·이벤트 처리
- `StageSystem`

## 주요 연결

Territory, InfiniteGrid, Tower, Monster systems, PlayerRunner, Time·Round, Stage result UI.

활성 Sanctuary는 Territory 밖에서도 Builder가 타워를 설치할 수 있는 임시 Grid 건설 영역이다. Sanctuary가 소멸하면 State Authority가 그 영역과 겹치는 타워의 점유를 해제하고 NetworkObject를 Despawn한다.

## 관련 Asset

Sacred Zone, Sanctuary, Gate prefab과 `GameWorld.unity`; Stage result UI는 `GamePresentation.unity`.

## 변경 시 확인

- Host만 진행 상태와 Spawn을 변경하는지
- 활성 Sanctuary 안팎의 Builder 미리보기와 Host 건설 재검증이 일치하는지
- Sanctuary 소멸 시 겹치는 Tower의 Grid 점유와 NetworkObject가 정리되는지
- Gate 위치와 영역 경계
- 승패 UI가 모든 Client에 한 번만 표시되는지
- Scene 종료 시 이벤트 정리

## 기술 부채

규칙과 생성 흐름 상당 부분이 StageBootstrapper partial에 있어 독립 기능 경계가 약하다.
