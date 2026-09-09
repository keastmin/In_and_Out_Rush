# Active Work

현재 구현 중이거나 구현 전 예약을 위해 작성된 작업을 둔다. 작업별 파일 하나를 사용하며 Scene·Prefab·공용 연결부 예약을 구현 전에 기록한다.

Active 문서 작성 후에는 작업자의 진행 요청 전까지 `CheckReservation`, Commit, Push를 실행하지 않는다. 진행 요청 후 예약 문서가 공용 upstream에 존재하는 것을 `VerifyReservation`으로 확인한 뒤에만 구현한다.

구현과 검증이 끝나도 작업 문서는 `Active/`에 유지한다. 작업자는 로컬 구현을 테스트하고 최종 진행을 요청하며, 그 요청 후에만 에이전트가 완료 문서를 `Completed/`로 이동하고 구현 결과를 Commit·Push한다.

예약 범위를 바꾸면 수정된 Active 문서에 대해 다시 작업자 진행 요청, `CheckReservation`, Commit·Push, 원격 검증을 수행한다.
