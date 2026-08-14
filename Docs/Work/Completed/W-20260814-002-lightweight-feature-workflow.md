# W-20260814-002 경량 기능 작업 파이프라인

Status: Completed

## 담당자

AI 개발 환경 재구성 작업자

## 기능

공통 AI 기능 작업 절차

## 목표

Pull 필요 여부와 기존 Active 충돌을 먼저 확인하고, 충돌이 없으면 Active 문서를 작성한 뒤 원격 Push가 확인된 경우에만 구현하는 경량 협업 파이프라인을 구성한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `skill-creator`

## 예상 수정 코드

- `AGENTS.md`
- `README.md`
- `Docs/README.md`
- `Docs/AI_COLLABORATION_QUICKSTART.md`
- `Docs/Work/README.md`
- `Docs/Work/TEMPLATE.md`
- `Docs/Work/Active/README.md`
- `Docs/Decisions/ADR-0002-경량-기능-작업-파이프라인.md`
- `.agents/skills/manage-feature-work/`
- 기존 기능별 Skill의 공통 게이트 선행조건

## 예약 Scene·Prefab·Data Asset

없음

## 공용 계약 또는 Bootstrapper 변경

없음

## 다른 활성 작업과 겹치는 부분

없음

## 범위 밖

- 게임 코드와 Unity Asset 수정
- 상대 작업자의 승인 절차
- 에이전트의 자동 Commit·Push
- 자동 stash, reset, merge, rebase 또는 강제 Push

## 완료 조건

- Pull이 필요하면 에이전트가 알리고 구현 전에 멈춘다.
- 기존 Active 예약과 작업 범위가 겹치면 에이전트가 알리고 멈춘다.
- 충돌이 없으면 에이전트가 Active 문서를 먼저 작성한다.
- Active 문서가 원격에 Push된 것이 확인된 뒤에만 구현한다.
- 구현 후 관련 기능 문서와 Project Map을 필요한 경우에만 갱신한다.

## 실제 변경

- `manage-feature-work` Skill과 Git 확인 스크립트를 추가했다.
- `CheckStart`, `CheckReservation`, `VerifyReservation`의 세 단계로 흐름을 제한했다.
- 상대 작업자의 승인과 에이전트의 자동 Commit·Push를 제거했다.
- 작업자는 Pull과 Active 문서 Commit·Push만 담당하도록 공통 지침과 빠른 시작 문서를 갱신했다.
- 기존 기능별 Skill에 원격 Active 예약 검증 선행조건을 연결했다.

## 검증 결과

- 임시 bare 원격과 두 작업자 clone을 이용한 통합 테스트 통과
- 기존 Active 문서가 `ACTIVE_FILE`로 보고되는 것 확인
- Active 문서 Push 전 `RESERVATION_HAS_LOCAL_CHANGES` 차단 확인
- 작업자 Push 후 원격 예약 확인 및 구현 진입 통과
- 상대 작업자의 후속 Push를 `PULL_REQUIRED`로 차단
- Pull 후 새 Active와 변경 파일만 다시 보고하는 것 확인
- 미커밋 변경이 있는 작업 시작을 `WORKTREE_NOT_CLEAN`으로 차단
- Windows PowerShell 5 구문과 ASCII 실행 호환성 확인
- `quick_validate.py`는 번들 Python의 PyYAML 부재로 실행하지 못해 Skill frontmatter와 메타데이터를 별도 검증

## 남은 위험

- 두 작업자가 서로 다른 upstream 브랜치에만 Active를 Push하면 예약을 서로 확인할 수 없다.
- Active 문서에 수정 예정 경로와 공용 연결부를 부정확하게 적으면 의미상 충돌 탐지 정확도가 낮아진다.
- Git 인증 또는 네트워크 장애가 있으면 Fetch 단계에서 중단된다.
