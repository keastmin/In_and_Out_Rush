# Active Work

현재 승인 대기 중이거나 구현 중인 작업만 둔다. 작업별 파일 하나를 사용하며 Scene·Prefab·공용 연결부 예약을 구현 전에 기록한다.

- `Awaiting Approval`: Active 문서만 작성했고 아직 원격 예약으로 공개되지 않은 상태
- `Reserved`: 사용자가 승인했고 공용 upstream에 예약이 Push된 상태

`Reserved` 상태의 원격 문서가 확인되지 않으면 구현을 시작하지 않는다. 예약 범위를 바꾸면 승인과 예약 공개 절차를 다시 수행한다.
