# ProjectIO 공통 AI 작업 규칙

이 문서는 모든 협업자와 AI에게 동일하게 적용되는 저장소 공통 규칙이다. 개인별 namespace, 담당자 이름, 임시 작업 상태는 이 문서에 기록하지 않는다.

## 기능 작업 사전 파이프라인

- 코드, Scene, Prefab, ScriptableObject, ProjectSettings, Package, 테스트 또는 구현 문서를 바꿀 수 있는 요청은 다른 Skill보다 먼저 `manage-feature-work` Skill을 적용한다.
- 사용자는 기능 목표를 자연어로 요청하면 된다. 에이전트가 동기화 확인, Active 충돌 검사, 예약 문서 작성과 원격 예약 검증을 수행한다.
- 원격에 새 commit이 있으면 에이전트는 Pull이 필요하다고 알리고 멈춘다. 사용자가 Pull 완료를 알린 뒤 다시 확인한다.
- 기존 Active 예약과 요청 범위가 겹치면 겹치는 문서와 코드·Asset·공용 연결부를 알리고 구현하지 않는다.
- 충돌이 없으면 에이전트가 Active 문서를 먼저 작성하고 대기한다. 작업자가 문서를 확인했다고 알리면 에이전트가 해당 문서만 정확히 stage하여 Commit·Push하고, 원격 존재를 검증한 뒤 구현한다.
- Active 문서에 대한 작업자의 확인 알림은 필요하지만 기능 범위나 구현 내용의 별도 승인은 요구하지 않는다. Pull이 필요한 경우의 Pull은 사용자에게 알리고, 예약·구현 결과의 Commit·Push와 완료 알림은 에이전트가 담당한다.
- 에이전트는 현재 Active 예약에 기록된 자신의 변경만 명시적인 파일 경로로 stage·Commit·Push할 수 있다. 기존 사용자 변경은 포함하지 않으며, 자동 Pull, stash, reset, merge, rebase 또는 강제 Push로 Git 상태를 바꾸지 않는다. `git add .`와 `git add -A` 같은 broad staging도 사용하지 않는다.
- 구현 중 예약하지 않은 공용 파일이나 Asset이 필요하면 해당 변경 전에 Active 범위를 갱신하고 작업자 확인을 기다린 뒤, 갱신된 예약 문서의 `CheckReservation`·Commit·Push·`VerifyReservation`을 완료한 후 계속한다.
- 설명, 읽기 전용 조사와 리뷰처럼 저장소 구현을 변경하지 않는 요청에는 이 파이프라인을 적용하지 않는다.

## 작업 전 읽기 순서

1. 이 `AGENTS.md`를 읽는다.
2. 구현 요청이면 `manage-feature-work` Skill의 `CheckStart`를 실행한다.
3. `Docs/PROJECT_MAP.md`에서 대상 기능과 공용 연결부를 찾는다.
4. `Docs/Work/Active/`에서 겹치는 작업과 Scene·Prefab 예약을 확인한다.
5. 대상 `Docs/Features/<Feature>.md`만 읽는다.
6. 작업 성격에 맞는 기능별 Skill을 읽는다.
7. 기능 문서가 가리키는 실제 코드, 호출자, Scene, Prefab을 확인한다.

문서와 코드가 다르면 실제 코드와 직렬화 참조를 기준으로 판단하고, 작업 범위 안에서 문서를 갱신한다. 예상하지 못한 공용 의존성이 발견될 때만 조사 범위를 넓힌다.

## 활성 작업 예약

- `manage-feature-work` 절차에 따라 구현 전에 `Docs/Work/Active/`에 작업별 파일을 만든다.
- 기능, 목표, 예상 수정 파일, 공용 계약, Scene·Prefab, 충돌 가능성을 기록한다.
- 같은 파일이나 공용 연결부가 이미 예약되어 있으면 구현 전에 협업자와 범위를 조정한다.
- Active 문서가 공용 upstream에 Push된 것을 확인하기 전에는 구현하지 않는다.
- 작업이 끝나면 검증 결과를 기록하고 `Docs/Work/Completed/`로 이동한다.
- 장기 작업만 별도 `HANDOFF.md`를 사용한다. 짧은 작업은 완료된 작업 파일로 충분하다.

## 인코딩과 파일 보존

- 기존 한글과 비ASCII 문자를 의도한 수정 외에는 변경하지 않는다.
- 인코딩, BOM, LF/CRLF를 명시적 이유 없이 바꾸지 않는다.
- 작은 수정에 파일 전체를 재작성하지 않고 최소 patch를 사용한다.
- Unity Asset을 추가·이동·삭제할 때 대응하는 `.meta` 파일을 함께 보존한다.
- 무관한 사용자 변경을 되돌리거나 함께 정리하지 않는다.
- 인코딩이 불명확하거나 UTF-8이 아니면 저장하기 전에 보고한다.

## 코드와 도메인 구조

- 코드 수정 권한을 namespace나 개인별 접미사로 구분하지 않는다. 활성 작업 예약과 Git diff로 충돌을 관리한다.
- 새 코드와 리팩토링된 코드는 가능한 한 `Assets/02_Scripts/Features/<Domain>/` 아래에 둔다.
- 기능 규모에 맞춰 `Public`, `Logic`, `UseCases`, `Adapters/Fusion`, `Adapters/Unity`, `Presentation`, `Tests` 중 필요한 폴더만 만든다.
- 빈 계층이나 미래를 위한 추상화를 미리 만들지 않는다.
- 하나의 파일에는 하나의 최상위 class, struct, enum 또는 interface만 둔다.
- 기존 Unity Asset의 대량 이동은 별도 승인된 마이그레이션 작업으로 수행한다.
- namespace는 도메인 경계를 표현할 때만 사용하고 폴더 계층을 그대로 반복하지 않는다.

## 의존성과 초기화

- UI는 표시와 사용자 입력 전달을 담당하고 게임 규칙이나 네트워크 상태를 소유하지 않는다.
- 게임 시스템 의존성은 Composition Root인 Bootstrapper 또는 전용 초기화 코드에서 실제 소비자에게 전달한다.
- UI 이벤트, 전역 검색, Singleton, Service Locator를 의존성 전달의 우회 수단으로 새로 도입하지 않는다.
- 초기화 순서가 문제라면 참조 검색을 늘리기 전에 준비 상태와 초기화 계약을 명시한다.
- 공용 계약이나 Bootstrapper 변경은 별도 작업으로 분리할 수 있는지 먼저 판단한다.

## 네트워크 기능

- Fusion 상태, Spawn, RPC, Authority, Host·Client 동작에 영향을 주면 `photon-fusion-feature` Skill을 적용한다.
- 지속 상태를 RPC만으로 관리하지 않는다.
- 네트워크 상태와 로컬 표현을 분리한다.
- 권한이 없는 객체가 authoritative 상태를 직접 변경하지 않게 한다.
- 순수 로컬 UI, 카메라, VFX에는 불필요한 네트워크 동기화를 추가하지 않는다.
- Host, Client, 권한 없는 호출, Late Join, Despawn 정리를 관련 범위만큼 검증한다.

## 기존 기능 교체와 마이그레이션

- 기존 기능을 완전히 교체하면 `replace-existing-feature` Skill을 적용한다.
- 기존 기능을 유지하면서 일부 흐름을 단계적으로 옮기면 `migrate-feature-slice` Skill을 적용한다.
- Chunk Territory 작업은 `build-chunk-territory` Skill을 적용한다.
- 기존 호출자, 이벤트, Scene·Prefab 참조, ScriptableObject, 네트워크 상태를 조사하지 않고 새 구현을 병렬로 추가하지 않는다.
- Legacy와 새 경로가 같은 Spawn, 상태 변경, 이벤트 또는 표현을 동시에 실행하지 않게 한다.
- 삭제 전 전환 완료 조건과 롤백 방법을 기록한다.

## Scene과 Prefab

- Scene·Prefab 작업의 영구 담당자를 두지 않는다.
- 수정할 Asset은 활성 작업 파일에 정확한 경로로 예약한다.
- 동일 Scene·Prefab을 두 작업이 동시에 수정하지 않는다.
- Additive 게임 구성은 `GameWorld`, `GamePresentation`, `GameRoot`의 역할을 유지한다.
- Scene 분리 후에는 코드 참조뿐 아니라 인스펙터 참조, 비활성 오브젝트, 초기화 순서, Fusion Scene 로딩을 함께 검증한다.
- YAML 직접 수정은 작은 변경에만 사용하고 fileID와 guid 무결성을 확인한다.

## asmdef

- asmdef는 도메인 경계를 실제로 강제할 수 있는 작은 범위에서만 도입한다.
- 새 assembly가 `Assembly-CSharp` 타입에 역으로 의존하지 않게 한다.
- 기존 런타임 파일을 한 번에 이동하지 않고 독립 가능한 계약이나 순수 로직부터 옮긴다.
- assembly reference, Editor 전용 테스트, Unity 직렬화 영향과 컴파일 결과를 검증한다.
- 첫 파일럿은 Resource Spawn이며 결과는 `Docs/Features/ResourceSpawn.md`에 기록한다.

## 문서 업데이트

- 내부 구현만 바뀌면 완료 작업 문서의 검증 결과만 갱신한다.
- 주요 진입점, 공개 연결부, 관련 Scene·Prefab이 바뀌면 기능 문서를 갱신한다.
- 기능 책임이나 라우팅이 바뀌면 기능 문서와 `Docs/PROJECT_MAP.md`를 함께 갱신한다.
- 저장소 전체 규칙이 바뀌면 이 문서를 갱신한다.
- 여러 기능에 반복 적용할 절차가 바뀌면 해당 Skill을 갱신한다.
- 중요한 구조 결정은 `Docs/Decisions/`에 남긴다.
- 매 작업마다 모든 문서를 수정하지 않는다.

## 완료 기준

- 요청된 동작과 수동 설정을 확인한다.
- 가능한 범위에서 집중 테스트와 프로젝트 컴파일을 실행한다.
- Scene·Prefab 변경이 있으면 누락 참조와 실행 경로를 확인한다.
- 네트워크 변경이면 Authority와 Host·Client 결과를 확인한다.
- `git diff --check`와 `git status`로 무관한 변경과 공백 오류를 확인한다.
- 실제로 수행하지 못한 검증은 완료로 표현하지 않고 이유와 남은 테스트를 기록한다.
