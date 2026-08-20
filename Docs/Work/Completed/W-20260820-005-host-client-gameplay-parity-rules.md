# W-20260820-005 Host·Client 플레이 체감 동등성 규칙

Status: Complete

## 동기화 기준

- Base Commit: c700b008c9ce016a23128ee9b33a7fc3faf060ef
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

공통 AI 개발 환경, Photon Fusion 작업 절차

## 목표

공유 게임 상태와 NetworkObject 수명은 Host·State Authority가 결정하되, Player Builder와 Player Runner를 Host 로컬 플레이어 또는 Client 피어에서 조작할 때 입력 가능 여부, 요청 결과, 실패 피드백과 로컬 표현의 플레이 체감이 동등하도록 저장소 공통 규칙과 Fusion Skill의 기본 완료 조건을 강화한다. 사용자가 매 기능 요청에서 별도로 Host·Client 동등성을 반복하지 않아도 네트워크 기능의 예약, 구현, 검증 단계가 이를 자동으로 다루게 한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/Work/TEMPLATE.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`
- Codex `skill-creator` Skill

## 예상 수정 코드

- `AGENTS.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`
- `Docs/Work/TEMPLATE.md`
- `Docs/Work/Active/W-20260820-005-host-client-gameplay-parity-rules.md`

## 예약 Scene·Prefab·Data Asset

없음. 코드, Scene, Prefab, ScriptableObject, ProjectSettings와 Package는 변경하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- 네트워크 기능의 기본 계약에 "공유 결과는 State Authority가 결정하고, Player Builder·Runner의 Host 로컬 입력과 Client Input Authority 입력은 같은 요청·결과 계약을 거쳐 동등한 성공·실패 체감을 제공한다"는 불변식을 추가한다.
- Host 전용 실행 경로와 플레이어 입력·로컬 표현 경로를 구분하고, 권위가 없다는 이유로 Client의 유효한 입력, 읽기 전용 사전 판정, 피드백 또는 표현을 조용히 비활성화하지 않도록 한다.
- Host가 로컬 Client이기도 한 경우 직접 실행과 요청 경로가 중복되어 Spawn, 상태 변경, 이벤트 또는 표현이 두 번 발생하지 않도록 한다.

## 다른 활성 작업과 겹치는 부분

- `W-20260820-004-construction-tower-build-usecase.md`는 Construction 코드, `TowerBuildManager`, 기능 문서와 `ProjectIO.slnx`를 예약한다.
- 이번 작업은 공통 규칙과 Skill, 작업 템플릿만 수정하며 Construction 예약 코드·기능 문서·Asset을 변경하지 않는다.
- Construction 작업은 이미 Host Builder와 Client Builder의 성공·실패 동등성을 완료 조건으로 기록했으므로 의미 충돌 없이 강화된 공통 규칙의 선행 사례로 취급한다.

## 범위 밖

- 현재 Player Builder, Player Runner 또는 다른 네트워크 기능 코드의 결함 수정
- RPC, Networked property, Authority, Spawn·Despawn, AOI schema 변경
- 기존 기능의 Host·Client 런타임 전수 감사
- Scene, Prefab, ScriptableObject 또는 Network Prefab table 변경
- 자동화된 다중 Peer 테스트 하네스 신규 구현

## 완료 조건

- `AGENTS.md`가 Host authoritative 공유 결과와 Host·Client 플레이 체감 동등성을 서로 다른 책임으로 명시한다.
- `photon-fusion-feature` Skill이 입력 원점, Authority 전달, 결과 복제·응답, 로컬 표현, 실패 피드백과 Host 중복 실행을 설계·검증하도록 요구한다.
- Builder·Runner 네트워크 작업은 별도 언급이 없어도 Host 로컬 플레이어와 Client Input Authority 플레이어의 성공·거부·실패 흐름을 완료 조건에 포함한다.
- 권위 확인은 authoritative mutation을 보호하되, 복제 상태 읽기, 입력 수집, 미리보기·HUD와 결과 피드백을 불필요하게 차단하지 않는다는 기준을 명시한다.
- `manage-feature-work`와 `Docs/Work/TEMPLATE.md`가 네트워크·Peer 동등성 설계와 검증 증거를 예약 단계에서 기록하게 한다.
- 실제 Host·Client 런타임 검증을 수행하지 못하면 컴파일만으로 동등성을 완료 처리하지 않고 미검증 항목과 수동 테스트 절차를 남기게 한다.
- 변경한 Skill을 `skill-creator`의 `quick_validate.py`로 검증하고 `git diff --check`를 통과한다.

## 실제 변경

- `AGENTS.md`의 네트워크 공통 규칙에 공유 게임 결과의 Host·State Authority 소유와 Player Builder·Runner의 Host 로컬·Client Input Authority 플레이 체감 동등성을 별도 불변식으로 추가했다.
- 플레이어 의도를 Input Authority에서 수집해 State Authority가 검증·상태 변경·Spawn하고, 복제 상태 또는 명시적 결과 응답으로 요청 Peer에 돌려주는 기본 흐름을 기록했다.
- `HasStateAuthority`와 `IsInSimulation` 검사를 authoritative mutation 보호와 구분해 복제 상태 읽기, 로컬 입력, 미리보기·HUD와 실패 피드백을 불필요하게 차단하지 않도록 했다.
- `photon-fusion-feature` Skill에 Host·Client 요청의 end-to-end 추적, 실패 결과 반환, Host 직접 경로의 계약 통합, Peer별 준비 상태와 Builder·Runner 런타임 검증 기준을 추가했다.
- `manage-feature-work` Skill이 네트워크 작업 예약 시 입력 원점, 권위 처리, 결과 반환, Host·Client 성공·실패 체감과 런타임 검증 절차를 기록하도록 했다.
- `Docs/Work/TEMPLATE.md`에 `네트워크·Peer 동등성` 항목을 추가하고 Builder·Runner 네트워크 작업에서는 별도 요청이 없어도 생략하지 못하게 했다.
- 코드, Scene, Prefab, ScriptableObject, Network schema와 현재 활성 기능 구현은 변경하지 않았다.

## 검증 결과

- `skill-creator/scripts/quick_validate.py .agents/skills/photon-fusion-feature`: `Skill is valid!`
- `skill-creator/scripts/quick_validate.py .agents/skills/manage-feature-work`: `Skill is valid!`
- 번들 Python에 누락된 PyYAML은 `Temp/CodexSkillValidationPyYaml`에만 임시 설치해 검증했고 검증 직후 경로 삭제를 확인했다.
- `rg`로 공통 규칙, 두 Skill과 템플릿의 Peer 동등성·State Authority·런타임 검증 문구가 모두 존재함을 확인했다.
- `git diff --check`: 통과. 기존 Git 줄바꿈 설정에 따른 LF→CRLF 경고만 있고 공백 오류는 없다.
- 이번 변경은 AI 작업 규칙과 문서 템플릿만 수정하므로 Unity 컴파일과 Host·Client 런타임 게임 검증 대상은 아니다.

## 남은 위험

- 규칙 강화는 이후 구현의 판단과 검증을 개선하지만 기존 네트워크 기능의 잠재적 Client 전용 결함을 자동으로 수정하지는 않는다.
- 실제 Peer 동등성은 각 기능 Slice에서 Host·Client 런타임 테스트로 계속 확인해야 한다.
- 이미 작성된 기존 Active 문서는 새 템플릿 항목을 소급해서 갖지 않을 수 있으므로, 해당 작업의 기존 네트워크 완료 조건을 기준으로 검증한다.
