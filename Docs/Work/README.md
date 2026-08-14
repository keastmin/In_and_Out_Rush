# 작업 상태 운영 규칙

## 공통 전제

- 두 협업자는 Active 예약이 보이는 동일한 공용 upstream 브랜치를 사용한다.
- 모든 구현 작업은 `.agents/skills/manage-feature-work/`를 먼저 적용한다.
- Git 상태 전이는 Skill의 `scripts/FeatureWork.ps1`가 수행한다.
- 자동 stash, reset, merge commit, 강제 Push는 사용하지 않는다.

## 1. 첫 동기화

`Preflight`는 깨끗한 작업 트리에서 Fetch하고 필요한 경우에만 `pull --ff-only`를 수행한다. 로컬 변경, 미공개 commit, 분기된 브랜치 또는 Fetch 실패가 있으면 구현과 예약 작성을 시작하지 않는다.

## 2. 예약 작성과 재확인

`TEMPLATE.md`를 복사해 `Active/W-YYYYMMDD-NNN-short-name.md` 형식으로 만든다. 같은 기능의 작업이 없어도 Scene·Prefab 또는 공용 연결부가 겹칠 수 있으므로 예상 수정 대상을 먼저 적는다.

이 단계에서는 Active 문서 외의 파일을 수정하지 않는다. `RefreshReservation`으로 원격을 다시 확인하고, 새로 변경된 파일과 Active 예약만 비교한다.

## 3. 사용자 승인과 예약 공개

- 에이전트는 예약 범위와 충돌 검사 결과를 보여주고 한 번 승인을 기다린다.
- 승인은 Active 문서만 Commit·Push하고 최종 검증 후 구현을 계속하는 것을 허용한다.
- 승인 후 Active 문서를 `Status: Reserved`로 바꾸고 승인 사실을 기록한다.
- `PublishReservation`은 Active 문서 외의 변경이나 staging이 있으면 중단한다.
- Push 후 원격에서 예약 문서를 찾지 못하면 구현하지 않는다.

## 4. 구현 직전 검증

`VerifyImplementation`으로 Fetch, 필요한 fast-forward Pull, 원격 예약 존재를 확인한다. 새 변경이 발견되면 해당 변경 파일과 Active 문서만 다시 읽고 의미상 충돌을 검사한다.

Git 파일 충돌이 없어도 같은 초기화 연결부, 공개 계약, Spawn 흐름, Fusion NetworkObject, Scene, Prefab 또는 ScriptableObject를 건드리면 예약 충돌로 본다.

## 예약 충돌

- 같은 코드 파일, Scene, Prefab, ScriptableObject를 두 Active 작업이 동시에 예약하지 않는다.
- 충돌하면 공용 변경을 작은 선행 작업으로 분리하거나 한 작업이 끝날 때까지 다른 작업의 범위를 줄인다.
- Scene·Prefab 전담자를 고정하지 않는다. 해당 작업 파일에 기록된 작업자가 그 작업 동안만 수정 권한을 가진다.
- Pull 후에는 변경된 Active 파일, 관련 기능 문서, 공용 연결부 diff만 우선 확인한다.

구현 중 예약 범위를 확대해야 하면 Active 문서를 갱신하고 재확인, 승인, 예약 Push와 구현 직전 검증을 반복한다.

## 5. 완료

- 결과, 실제 수정 파일, 검증 명령과 결과, 남은 위험을 기록한다.
- 파일을 `Completed/`로 이동한다.
- 주요 진입점이나 책임이 바뀌었을 때만 기능 문서와 Project Map을 갱신한다.
- 구현 결과의 최종 Commit·Push는 사용자가 별도로 요청하거나 승인한 경우에만 수행한다.
- 최종 변경이 공개되기 전까지 원격 Active 문서는 보수적인 작업 잠금으로 남는다.

## Handoff

Handoff는 여러 작업 또는 여러 날에 걸쳐 승인 단계, 롤백, 다음 진입점을 유지해야 하는 장기 작업에만 둔다. 일반 작업은 Completed 기록을 사용한다.
