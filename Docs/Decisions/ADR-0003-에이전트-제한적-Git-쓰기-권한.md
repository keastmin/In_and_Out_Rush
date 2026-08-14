# ADR-0003 에이전트 제한적 Git 쓰기 권한

Status: Accepted

Date: 2026-08-14

Supersedes: ADR-0002 경량 기능 작업 파이프라인의 작업자 Commit·Push 정책

## 배경

Active 예약 문서를 에이전트가 작성한 직후 자동 Commit·Push하면 작업자가 예약 범위와 충돌 가능성을 확인하기 전에 원격 상태가 바뀐다. 반대로 작업자가 모든 구현 Commit·Push를 맡으면 에이전트가 관리하는 예약 작업의 Git 흐름이 불필요하게 중단된다.

## 결정

1. `manage-feature-work`의 `CheckStart`, `CheckReservation`, `VerifyReservation`을 기존처럼 구현 전 게이트로 사용한다.
2. 에이전트는 Active 예약 문서를 작성하고 `CheckReservation`을 통과한 뒤 작업자 확인을 기다린다.
3. 작업자가 Active 문서를 확인했다고 알리면 에이전트가 해당 문서만 정확한 경로로 stage하여 Commit·Push하고, `VerifyReservation` 통과 후 구현한다.
4. 구현 완료 시 예약 범위에 기록된 에이전트 변경과 완료 문서만 정확한 경로로 Commit·Push한다.
5. 에이전트는 기존 사용자 변경을 stage하지 않는다. `git add .`, `git add -A`와 같은 broad staging을 사용하지 않는다.
6. Pull이 필요하면 에이전트는 사용자에게 알리고 중단한다. 자동 stash, reset, merge, rebase, 강제 Push는 계속 금지한다.
7. Commit·Push가 실패하면 현재 Git 상태를 보고하고 중단하며, 파괴적인 복구를 자동으로 시도하지 않는다.

## 결과

- 작업자가 Active 예약의 범위와 충돌 가능성을 확인한 뒤 원격 예약이 공유된다.
- 예약 확인 후에는 에이전트가 제한된 범위의 Commit·Push를 직접 수행할 수 있다.
- 기존 사용자 변경과 위험한 Git 상태 변경을 보호한다.
- 구현 승인이나 일반 문서 승인은 요구하지 않고, Active 예약 확인만 handoff로 사용한다.

## 검증

- 예약 문서 단독 `CheckReservation` 통과
- 작업자 확인 후 예약 Commit·Push 및 `VerifyReservation` 통과
- 구현 완료 파일의 정확한 stage 목록과 원격 동기화 확인
