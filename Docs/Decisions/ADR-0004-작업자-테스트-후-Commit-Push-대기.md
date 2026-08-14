# ADR-0004 작업자 테스트 후 Commit·Push 대기

Status: Accepted

Date: 2026-08-15

Supersedes: ADR-0003의 구현 완료 즉시 Commit·Push 규칙

## 배경

Active 예약 문서와 구현 결과를 에이전트가 즉시 Commit·Push하면 작업자가 구현 완료 상태를 직접 테스트하기 전에 원격 상태가 확정된다. 예약 충돌 검사는 유지하면서 구현 결과를 작업자가 검증할 시간을 보장할 필요가 있다.

## 결정

1. `CheckStart`로 원격 동기화와 기존 Active 예약 충돌을 먼저 확인한 뒤 Active 예약 문서를 작성한다.
2. Active 문서 작성 후에는 작업자의 예약 진행 요청 전까지 `CheckReservation`, stage, Commit, Push를 실행하지 않는다.
3. 작업자가 진행을 요청하면 `CheckReservation`을 실행한다. 통과한 경우 Active 문서만 정확히 stage하여 Commit·Push하고, `VerifyReservation` 통과 후 구현을 시작한다.
4. 구현과 자동 검증이 끝나면 실제 변경, 테스트 결과, 남은 위험을 기록하고 작업자에게 테스트 방법을 보고한다. 이때 구현 변경과 Active 문서는 로컬에 유지하고 Commit·Push하지 않는다.
5. 작업자가 최종 진행을 요청하면 정확한 예약 범위만 stage하고 작업 문서를 `Completed/`로 이동한 뒤 Commit·Push한다. 작업자가 수정을 요청하면 수정·검증 후 최종 진행 요청을 다시 기다린다.
6. 에이전트 Commit 제목은 `<prefix>: 한국어 커밋 내용` 형식을 사용한다. 허용 prefix는 `feat`, `fix`, `refactor`, `perf`, `docs`, `test`, `chore`, `build`, `ci`다.
7. Pull 필요, 충돌, stage 범위 오류, Commit·Push 실패가 발생하면 현재 Git 상태를 보고하고 자동 복구하지 않는다.

## 결과

- 작업자는 구현 결과를 로컬에서 테스트한 뒤 최종 Commit·Push 시점을 결정할 수 있다.
- 예약 문서는 작업자의 진행 요청 전까지 원격에 공유되지 않으므로, 최초 `CheckStart`와 진행 요청 후 `CheckReservation`에서 충돌을 확인한다.
- 구현 완료 상태가 의도적으로 미커밋으로 유지되므로 무관한 변경이 섞이지 않도록 최종 요청 전 정확한 diff를 재확인한다.
- 예약과 구현 결과 모두 에이전트가 제한된 경로만 stage하고 한국어 커밋 제목으로 Push한다.

## 검증

- Active 문서 작성 후 대기 상태에서 `CheckReservation`, stage, Commit, Push가 실행되지 않는지 확인
- 진행 요청 후 `CheckReservation` → 예약 문서 Commit·Push → `VerifyReservation` → 구현 순서 확인
- 구현 완료 후 최종 진행 요청 전까지 Active 문서와 구현 변경이 로컬에 유지되는지 확인
- 최종 진행 요청 후 정확한 파일 stage, Completed 이동, 한국어 커밋 제목, Push 및 clean worktree 확인
