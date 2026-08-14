# W-20260814-005 에이전트 제한적 Git 쓰기 권한

Status: Completed

## 현재 단계

- Active 예약 문서 작성 완료
- 작업자 확인 완료
- 예약 문서 Push와 원격 검증 완료 후 구현 진행

## 동기화 기준

- Base Commit: 70dc406fe56ba2ec904cd3b3930149a79fd414fc
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

저장소 공통 AI 협업 및 Active 예약 작업 파이프라인

## 목표

에이전트가 현재 Active 예약에 기록된 자신의 변경만 정확한 파일 목록으로 Commit·Push할 수 있도록 저장소 규칙과 협업 Skill을 갱신한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/AI_COLLABORATION_QUICKSTART.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `Docs/Decisions/ADR-0002-경량-기능-작업-파이프라인.md`

## 예상 수정 코드

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/AI_COLLABORATION_QUICKSTART.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `Docs/Decisions/ADR-0003-에이전트-제한적-Git-쓰기-권한.md`

## 예약 Scene·Prefab·Data Asset

없음

## 공용 계약 또는 Bootstrapper 변경

없음

## 다른 활성 작업과 겹치는 부분

없음. 기존 몬스터 코드와 작업 문서는 이 예약 범위에 포함하지 않는다.

## 범위 밖

- Unity 런타임 코드, Scene, Prefab, ScriptableObject, ProjectSettings, Package 변경
- `FeatureWork.ps1` 검증 단계 변경
- 과거 `Docs/Work/Completed/` 기록 수정
- 기존 사용자 변경의 stage·Commit·Push
- 자동 Pull, stash, reset, merge, rebase, 강제 Push

## 완료 조건

- Active 예약 문서를 작성한 뒤 작업자 확인을 위해 대기하고, 확인 알림 후에만 별도 Push와 원격 검증을 진행한다.
- 에이전트가 예약 범위 내 자신의 변경만 명시적으로 stage·Commit·Push하도록 `AGENTS.md`, 작업 문서, Quickstart, Skill을 일관되게 갱신한다.
- 기존 ADR을 수정하지 않고 새 ADR로 제한적 Git 쓰기 권한과 안전 경계를 기록한다.
- 예약 및 구현 완료 Commit·Push 후 원격 동기화와 문서 모순 여부를 확인한다.
- `git diff --check`와 최종 `git status`를 통과한다.

## 실제 변경

- 작업자 확인 대기 절차를 Active 예약 문서에 기록했다.
- `AGENTS.md`에 작업자 확인 후 제한적 Commit·Push 권한과 broad staging 금지를 기록했다.
- `Docs/Work/README.md`와 `Docs/AI_COLLABORATION_QUICKSTART.md`에 Active 확인 대기 및 에이전트 Commit·Push 흐름을 반영했다.
- `.agents/skills/manage-feature-work/SKILL.md`에 작업자 확인 대기, 정확한 stage, Commit·Push와 실패 중단 규칙을 반영했다.
- `Docs/Decisions/ADR-0003-에이전트-제한적-Git-쓰기-권한.md`를 추가해 ADR-0002의 작업자 Commit·Push 정책을 대체했다.

## 검증 결과

- `CheckStart`: `AHEAD=0`, `BEHIND=0`, `RESULT=READY_TO_CHECK_CONFLICTS`
- `VerifyReservation`: `RESULT=READY_TO_IMPLEMENT`
- `git diff --check`: 통과
- 관련 운영 문서에서 기존 작업자 Commit·Push 위임 문구와 에이전트 Commit·Push 금지 문구를 갱신한 것을 확인했다.
- `FeatureWork.ps1`와 Unity 런타임 코드는 변경하지 않았다.
- Unity 컴파일·플레이 테스트: 문서·Skill 변경 작업이므로 실행하지 않았다.

## 남은 위험

- Git 인증 또는 원격 네트워크 장애로 Push가 실패할 수 있다.
- 향후 규칙을 우회하는 broad staging이 추가되지 않도록 Skill과 완료 검증을 함께 유지해야 한다.
