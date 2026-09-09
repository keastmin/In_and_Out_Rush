# W-20260815-008 작업자 테스트 후 Commit·Push 대기 절차

Status: Reserved

## 동기화 기준

- Base Commit: 380b85181f7420a6b7f4c02548fd9007dc095933
- 공용 Upstream: origin/rebuild-development-environment

## 담당

Codex

## 기능

ProjectIO의 Active 예약 및 구현 완료 Git 작업 흐름

## 목표

- Active 예약 문서 작성 후 작업자의 명시적 진행 요청까지 대기한다.
- 진행 요청 후 `CheckReservation`, 예약 문서 Commit·Push, `VerifyReservation`을 순서대로 수행하고 구현을 시작한다.
- 구현과 검증 완료 후 작업자가 테스트할 수 있도록 Commit·Push 없이 다시 대기한다.
- 최종 진행 요청 후 완료 문서 이동과 구현 결과 Commit·Push를 수행한다.
- 에이전트 Commit 제목을 `prefix: 한국어 커밋 내용` 형식으로 통일한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/AI_COLLABORATION_QUICKSTART.md`
- `Docs/Work/Active/README.md`
- `Docs/Decisions/ADR-0003-에이전트-제한적-Git-쓰기-권한.md`
- `manage-feature-work`
- `skill-creator`

## 예상 수정 파일

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/AI_COLLABORATION_QUICKSTART.md`
- `Docs/Work/Active/README.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/manage-feature-work/agents/openai.yaml`
- `Docs/Decisions/ADR-0003-에이전트-제한적-Git-쓰기-권한.md`
- `Docs/Decisions/ADR-0004-작업자-테스트-후-Commit-Push-대기.md`

## 예약 Scene·Prefab·Data Asset

없음

## 공용 계약 또는 Bootstrapper 변경

없음. 저장소 협업 규칙과 문서·Skill만 변경한다.

## 다른 Active 작업과 충돌 경계

기존 `W-20260815-007-host-peer-territory-performance-refactor`는 Territory·Monster 구현 파일을 예약한다. 이번 작업은 협업 규칙 문서와 `manage-feature-work` Skill만 수정하므로 코드·Asset·공용 연결부 충돌이 없다.

## 범위 밖

- 게임 런타임 코드, Scene, Prefab, ScriptableObject, ProjectSettings, Package, 테스트 코드
- 기존 완료 작업 문서의 내용 수정
- 자동 Pull, stash, reset, merge, rebase, force Push
- `git add .`, `git add -A` 같은 broad staging

## 완료 조건

- 예약 문서 작성 후 작업자 진행 요청 전에는 `CheckReservation`, stage, Commit, Push를 실행하지 않는다.
- 진행 요청 후 `CheckReservation` 통과, 예약 문서 단독 Commit·Push, `VerifyReservation` 통과를 확인한다.
- 구현 완료 후 최종 진행 요청 전까지 구현 결과와 Active 문서를 로컬에 유지한다.
- 최종 진행 요청 후 정확한 범위만 stage하고 Active 문서를 Completed로 이동한 뒤 Commit·Push한다.
- 관련 문서에 기존 즉시 Commit·Push 규칙이 남아 있지 않다.
- 모든 에이전트 Commit 제목이 허용된 prefix와 한국어 내용 형식을 따른다.
- `git diff --check`와 `git status`가 통과한다.

## 실제 변경

- `AGENTS.md`에 예약 진행 요청과 구현 완료 후 최종 Commit·Push 요청을 분리한 규칙을 반영했다.
- `Docs/Work/README.md`, `Docs/AI_COLLABORATION_QUICKSTART.md`, `Docs/Work/Active/README.md`에 두 단계 대기 흐름을 반영했다.
- `.agents/skills/manage-feature-work/SKILL.md`와 `agents/openai.yaml`에 예약 대기, 구현 결과 테스트 대기, 정확한 stage 범위, 커밋 제목 형식을 반영했다.
- `ADR-0003`을 보완하고 `ADR-0004-작업자-테스트-후-Commit-Push-대기.md`를 추가했다.
- 모든 에이전트 Commit 제목 규칙을 `<prefix>: 한국어 커밋 내용`으로 통일했다.

## 검증 결과

- `CheckStart` 통과: `rebuild-development-environment`와 `origin/rebuild-development-environment`가 `380b85181f7420a6b7f4c02548fd9007dc095933`에서 동기화됨.
- 기존 Active 예약과 의미상 충돌 없음.
- 작업자 진행 요청 후 `CheckReservation` 통과.
- 예약 문서만 `docs: 작업자 테스트 후 Commit·Push 대기 절차 예약` 커밋으로 Commit·Push 완료.
- `VerifyReservation` 통과: 구현 기준 `2ad0ce706a3de92499651a223ca1503bf695f3a8`.
- `python -X utf8 C:\Users\User\.codex\skills\.system\skill-creator\scripts\quick_validate.py .agents/skills/manage-feature-work` 통과.
- `git diff --check` 통과.
- 구현 규칙 문서에서 기존 즉시 Commit·Push 지침을 검색하고 새 대기 절차로 통일된 것을 확인했다.
- 구현 결과 문서 변경은 작업자 최종 진행 요청 전까지 로컬에 유지 중이다.

## 남은 위험

- 예약 문서 작성 후 최초 Push 전까지 다른 작업자가 원격에서 이 예약을 볼 수 없다. 작업자 진행 요청 시 `CheckReservation`이 원격 변경과 Active 예약을 다시 검사한다.
- 작업자가 구현 결과를 테스트하는 동안 의도적인 미커밋 변경이 유지되므로, 무관한 변경이 섞이지 않도록 최종 진행 요청 전에 변경 범위를 재확인해야 한다.
