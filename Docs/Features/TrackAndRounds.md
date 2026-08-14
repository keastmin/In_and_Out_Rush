# Track and Rounds

Status: Current

Last reviewed: 2026-08-14

## 책임

트랙의 상태와 확장, 라운드 시간, 웨이브 시작·종료와 버서크 시점을 관리한다.

## 주요 진입점

- `Assets/02_Scripts/System/TrackSystem.cs`
- `Assets/02_Scripts/Time/Network/TimeSystem.cs`
- `Track`, `TrackVisible`, `RangeIndexMapper`
- `StageBootstrapper.YOU`의 라운드 이벤트 연결

## 주요 소비자

Track Monster Spawn, Grid 차단 셀, Tower 정리, Stage UI timer, Resource·Obstacle 정리.

## 관련 Asset

- `GameWorld.unity`의 Track과 TimeSystem
- Track monster wave table

## 변경 시 확인

- Host만 라운드와 Track 상태를 변경하는지
- Client timer와 Track 표시가 복원되는지
- 이벤트 중복 구독과 Scene unload 정리
- Track 확장 시 Grid·Obstacle·Tower 소비자가 한 번씩 반응하는지

## 기술 부채

라운드 규칙 일부가 StageBootstrapper에 있어 Time·Track 도메인과 Stage 조립 책임의 경계가 불명확하다.
