# 작업 상태 운영 규칙

## 작업자가 하는 일

작업자는 Active 예약 문서의 예상 범위와 충돌 여부를 확인하고 진행 요청을 알린다. 구현 완료 후에는 로컬 결과를 테스트하고 최종 Commit·Push 진행 요청을 알린다. 기능 구현이나 일반 문서의 별도 승인은 요구하지 않는다.

1. Pull이 필요하다고 에이전트가 알리면 최신 변경을 Pull한다.
2. 에이전트가 작성한 Active 문서의 범위와 충돌 여부를 확인하고 `진행해줘`라고 알린다.
3. 에이전트가 구현 완료를 보고하면 로컬 결과를 테스트하고, 테스트가 끝난 뒤 최종 `진행해줘`라고 알린다.
4. 에이전트가 보고한 Push 실패, 인증 오류 또는 제품 결정을 처리한다.

에이전트는 작업자의 각 진행 요청 후 해당 단계에 필요한 자신의 변경만 정확한 파일 경로로 stage하여 Commit·Push한다. 기존 사용자 변경은 포함하지 않으며, 모든 기능 문서를 읽을 필요는 없다.

에이전트 Commit 제목은 `<prefix>: 한국어 커밋 내용` 형식을 사용한다. 허용 prefix는 `feat`, `fix`, `refactor`, `perf`, `docs`, `test`, `chore`, `build`, `ci`다.

## 1. 시작 확인

모든 구현 작업은 `.agents/skills/manage-feature-work/`의 `CheckStart`로 시작한다. 에이전트는 Fetch 후 다음을 확인한다.

- Pull이 필요한 원격 commit
- 로컬 미커밋 변경과 미공개 commit
- 기존 Active 예약

Pull이 필요하면 사용자에게 알리고 멈춘다. 자동 Pull, stash, reset, merge, rebase 또는 강제 Push는 하지 않는다.

## 2. 예약 충돌 확인

에이전트는 기존 Active 문서의 예약 코드, Scene·Prefab·Data Asset, Bootstrapper, 공개 계약과 네트워크 Spawn 경계를 요청 범위와 비교한다.

- 겹치면 해당 Active 문서와 충돌 경계를 사용자에게 알리고 멈춘다.
- 겹치지 않으면 새 Active 문서를 작성한다.

## 3. Active 작성과 예약 Push 확인

`TEMPLATE.md`를 복사해 `Active/W-YYYYMMDD-NNN-short-name.md` 형식으로 만든다. 같은 기능의 작업이 없어도 Scene·Prefab 또는 공용 연결부가 겹칠 수 있으므로 예상 수정 대상을 먼저 적는다.

Active 문서 작성 후에는 `CheckReservation`, stage, Commit, Push를 실행하지 않고 작업자의 진행 요청을 기다린다. 이때 작업자는 로컬 Active 문서의 범위와 충돌 가능성을 확인한다.

작업자가 진행을 요청하면 에이전트는 먼저 `CheckReservation`으로 다음을 다시 확인한다.

- 로컬과 upstream이 동기화됨
- 예약 문서만 변경됨
- 기준 Commit 이후 새로 변경된 파일과 Active 예약

검증이 통과하면 에이전트는 해당 Active 문서 경로만 명시적으로 stage하여 Commit·Push한다. Commit 제목은 `<prefix>: 한국어 커밋 내용` 형식을 사용한다. 예약 문서 Push 후에는 `VerifyReservation`으로 다음을 확인한다.

- 로컬과 upstream이 동기화됨
- 해당 Active 문서가 upstream에 존재함
- 상태가 `Reserved`임
- 최초 확인 이후 새로 변경된 파일과 Active 예약

검증이 통과하면 별도의 기능 승인 없이 구현을 시작한다.

## 예약 충돌

- 같은 코드 파일, Scene, Prefab, ScriptableObject를 두 Active 작업이 동시에 예약하지 않는다.
- 충돌하면 공용 변경을 작은 선행 작업으로 분리하거나 한 작업이 끝날 때까지 다른 작업의 범위를 줄인다.
- Scene·Prefab 전담자를 고정하지 않는다. 해당 작업 파일에 기록된 작업자가 그 작업 동안만 수정 권한을 가진다.
- Pull 후에는 변경된 Active 파일, 관련 기능 문서, 공용 연결부 diff만 우선 확인한다.

구현 중 예약 범위가 늘어나면 해당 파일을 변경하기 전에 Active 문서를 갱신하고 작업자의 진행 요청을 기다린다. 진행 요청 후 에이전트가 갱신된 예약 문서만 다시 `CheckReservation`, Commit·Push하고 원격 검증을 완료한다.

## 4. 구현과 완료

- 결과, 실제 수정 파일, 검증 명령과 결과, 남은 위험을 Active 문서와 필요한 기능 문서에 기록한다.
- 자동 검증을 마치면 작업자에게 구현 완료와 테스트 방법을 보고하고, Commit·Push하지 않은 채 최종 진행 요청을 기다린다. Active 문서는 이 대기 동안 `Active/`에 유지한다.
- 작업자가 최종 진행을 요청하면 예약 범위에 기록된 에이전트 변경과 관련 문서를 정확한 경로로 stage하고, 작업 문서를 `Completed/`로 이동한 뒤 Commit·Push한다.
- 작업자가 추가 수정을 요청하면 구현과 검증을 다시 수행하고 최종 진행 요청을 다시 기다린다.
- 주요 진입점이나 책임이 바뀌었을 때만 기능 문서와 Project Map을 갱신한다.
- 반복 절차나 저장소 공통 규칙이 바뀐 경우에만 Skill 또는 `AGENTS.md`를 갱신한다.
- 일상적인 문서 갱신을 작업자에게 승인받지 않는다.

## Handoff

Handoff는 여러 작업 또는 여러 날에 걸쳐 승인 단계, 롤백, 다음 진입점을 유지해야 하는 장기 작업에만 둔다. 일반 작업은 Completed 기록을 사용한다.
