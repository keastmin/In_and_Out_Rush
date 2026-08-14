# W-20260814-002 기능 작업 예약 자동화

Status: Completed

## 담당자

AI 개발 환경 재구성 작업자

## 기능

공통 AI 기능 작업 절차

## 목표

기능 구현 요청부터 원격 동기화, Active 예약 생성, 사용자 승인, 예약 전용 Commit·Push, 구현 직전 재확인까지의 절차를 공통 Skill과 저장소 스크립트로 자동화한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
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
- `Docs/Decisions/ADR-0002-기능-작업-예약-자동화.md`
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
- 사용자의 승인 없는 Commit·Push
- 자동 stash, reset, 강제 Push 또는 자동 merge

## 완료 조건

- Pull은 fast-forward 가능하고 안전한 경우에만 수행된다.
- Active 문서 작성 후 사용자 승인을 기다린다.
- 승인 후 예약 문서만 Commit·Push하도록 검증한다.
- 구현 직전에 원격 예약과 변경 파일을 다시 확인한다.
- 원격 변경이 없을 때는 전체 문서와 코드를 다시 읽지 않는다.

## 실제 변경

- `manage-feature-work` Skill과 `FeatureWork.ps1`를 추가했다.
- Preflight, RefreshReservation, PublishReservation, VerifyImplementation 상태 전이를 구현했다.
- 공통 `AGENTS.md`, 작업 문서, 템플릿, 빠른 시작 문서와 ADR을 자동화 흐름에 맞게 갱신했다.
- 네 가지 기능별 Skill에 원격 Active 예약 검증 선행조건을 연결했다.
- Windows PowerShell 5 호환성을 위해 실행 스크립트와 자동 판정 필드는 ASCII로 유지했다.

## 검증 결과

- 임시 bare 원격과 두 작업자 clone을 이용한 통합 테스트 통과
- 깨끗한 작업 트리 Preflight 및 fast-forward 경로 통과
- 승인 누락 시 `USER_APPROVAL_NOT_RECORDED` 차단 확인
- Active 문서만 Commit·Push하고 원격 존재 확인
- 협업자의 후속 Push를 구현 직전 `CHANGED_FILE`로 감지
- 미커밋 변경이 있는 Preflight를 `WORKTREE_NOT_CLEAN`으로 차단
- Windows PowerShell 5에서 전체 스크립트 실행 통과
- 모든 저장소 Skill의 frontmatter, 이름, TODO 부재와 새 `openai.yaml` 필드를 수동 검증
- `quick_validate.py`는 번들 Python의 PyYAML 부재로 실행하지 못함

## 남은 위험

- 두 협업자가 서로 다른 upstream 브랜치에만 예약을 Push하면 예약 가시성을 보장할 수 없다.
- Git 호스팅 인증 또는 네트워크 장애가 있으면 자동화는 Push나 Fetch 단계에서 중단된다.
- 구현 결과의 최종 Commit·Push 전까지 원격 Active 예약은 보수적인 잠금으로 남는다.
