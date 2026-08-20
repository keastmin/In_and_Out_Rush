# W-20260820-002 타워 건설 자원 지불 Slice

Status: Reserved

## 동기화 기준

- Base Commit: 19508128c2ca64f862d5ae8e25a29997cf4b3db6
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Resource Economy, Tower Build

## 목표

타워 건설 비용의 충분성 확인과 차감 결과 계산을 순수 Resource Economy 로직으로 분리하고, authoritative Networked Mineral·Gas 변경을 Fusion Adapter가 담당하게 한다. 첫 Slice에서는 `TowerBuildManager` 호출자 하나만 새 경로로 전환한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/ResourceEconomy.md`
- `Docs/Features/TowerAndLaboratory.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/migrate-feature-slice/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Features/ResourceEconomy.meta`
- `Assets/02_Scripts/Features/ResourceEconomy/Logic.meta`
- `Assets/02_Scripts/Features/ResourceEconomy/Logic/ProjectIO.ResourceEconomy.asmdef`
- `Assets/02_Scripts/Features/ResourceEconomy/Logic/ProjectIO.ResourceEconomy.asmdef.meta`
- `Assets/02_Scripts/Features/ResourceEconomy/Logic/ResourceAmount.cs`
- `Assets/02_Scripts/Features/ResourceEconomy/Logic/ResourceAmount.cs.meta`
- `Assets/02_Scripts/Features/ResourceEconomy/Logic/ResourcePaymentPolicy.cs`
- `Assets/02_Scripts/Features/ResourceEconomy/Logic/ResourcePaymentPolicy.cs.meta`
- `Assets/02_Scripts/Features/ResourceEconomy/Adapters.meta`
- `Assets/02_Scripts/Features/ResourceEconomy/Adapters/Fusion.meta`
- `Assets/02_Scripts/Features/ResourceEconomy/Adapters/Fusion/ResourcePaymentFusionAdapter.cs`
- `Assets/02_Scripts/Features/ResourceEconomy/Adapters/Fusion/ResourcePaymentFusionAdapter.cs.meta`
- `Assets/02_Scripts/Features/ResourceEconomy/Tests.meta`
- `Assets/02_Scripts/Features/ResourceEconomy/Tests/ProjectIO.ResourceEconomy.Tests.asmdef`
- `Assets/02_Scripts/Features/ResourceEconomy/Tests/ProjectIO.ResourceEconomy.Tests.asmdef.meta`
- `Assets/02_Scripts/Features/ResourceEconomy/Tests/ResourcePaymentPolicyTests.cs`
- `Assets/02_Scripts/Features/ResourceEconomy/Tests/ResourcePaymentPolicyTests.cs.meta`
- `Assets/02_Scripts/Stage/Network/StageBootstrapper.KIM.cs`
- `Assets/02_Scripts/Tower/Build/TowerBuildManager.cs`
- `Docs/Features/ResourceEconomy.md`
- `ProjectIO.slnx`
- `Docs/Work/Active/W-20260820-002-tower-build-resource-payment-slice.md`

## 예약 Scene·Prefab·Data Asset

없음. 새 Unity 스크립트·asmdef와 대응 `.meta`만 추가한다.

## 공용 계약 또는 Bootstrapper 변경

- `StageBootstrapper.KIM`이 기존 `ResourceSystem` 참조로 `ResourcePaymentFusionAdapter`를 구성해 `TowerBuildManager`에 주입한다.
- 순수 정책은 현재 잔액과 비용으로 지불 가능 여부 및 차감 후 잔액만 계산하며 Unity·Fusion에 의존하지 않는다.
- Fusion Adapter만 `ResourceSystem`의 Networked Mineral·Gas를 읽고 State Authority에서 차감 후 값을 기록한다.

## 다른 활성 작업과 겹치는 부분

없음. `Docs/Work/Active/README.md` 외 기존 Active 예약이 없다.

## Legacy와 새 진입점

- Legacy: `TowerBuildManager`가 `ResourceSystem.Instance.Mineral/Gas`를 직접 비교·차감한다.
- 새 경로: `TowerBuildManager`가 주입된 `ResourcePaymentFusionAdapter`를 통해 순수 정책 기반 충분성 확인과 authoritative 지불을 수행한다.
- 다른 Resource Economy 소비자는 기존 `ResourceSystem.IsResourceSufficient`, `TryDeductCost`, 직접 변경 경로를 유지한다.

## 롤백

- `TowerBuildManager`의 충분성 확인과 차감을 기존 직접 `ResourceSystem` 접근으로 되돌리고 Bootstrapper 주입을 제거하면 이 Slice만 즉시 롤백할 수 있다.
- 순수 정책과 Adapter는 다른 소비자 전환 전까지 독립적으로 제거 가능하며 직렬화 Asset 변경은 없다.

## 범위 밖

- Tower 이동·판매·업그레이드·속성 비용 전환
- Laboratory 공급·업그레이드 비용 전환
- 자원 획득·환불·UI 경로 변경
- 기존 `ResourceSystem` API 삭제 또는 전체 교체
- Scene, Prefab, ScriptableObject 변경

## 완료 조건

- 순수 정책이 충분한 Mineral·Gas, 부족한 한쪽 자원, 음수 비용 거부, 정확한 차감 후 잔액을 테스트한다.
- `TowerBuildManager.CanBuildAt`은 새 Adapter의 읽기 경로로 비용 충분성을 확인한다.
- State Authority의 타워 건설 승인 경로만 Adapter를 통해 Networked Mineral·Gas를 한 번 차감한다.
- 권한 없는 Peer나 부족한 잔액은 Networked 자원을 변경하지 않는다.
- 타워 Spawn 또는 다른 검증 실패 시 비용을 차감하지 않는다.
- 다른 Resource Economy 소비자의 Legacy 경로는 변경하지 않는다.
- 프로젝트 컴파일, 집중 테스트, `git diff --check`, `git status` 결과를 기록한다.

## 실제 변경

- Unity·Fusion 비의존 `ProjectIO.ResourceEconomy` assembly에 `ResourceAmount`와 `ResourcePaymentPolicy`를 추가했다.
- 순수 정책은 음수 비용을 거부하고, Mineral·Gas가 모두 충분할 때만 차감 후 잔액을 계산한다. 실패 시 원래 잔액을 유지한다.
- `ResourcePaymentFusionAdapter`가 `ResourceSystem` NetworkObject의 유효성과 State Authority를 확인하고, 순수 정책 결과를 Networked Mineral·Gas에 기록한다.
- Host·Client 공통 사전 판정을 위해 Adapter 읽기 경로는 복제된 ResourceSystem 잔액을 모든 Peer에서 허용하고, 쓰기 경로만 NetworkObject 유효성·시뮬레이션 참여·State Authority를 요구하도록 분리했다.
- `StageBootstrapper.KIM`이 기존 `ResourceSystem`으로 Adapter를 구성해 `TowerBuildManager`에 주입한다.
- `TowerBuildManager.CanBuildAt`과 Host의 성공 건설 차감 한 호출자만 새 Adapter로 전환했다. Spawn·Grid·특수 Tower 검증 실패 또는 권한 불일치에는 차감하지 않는다.
- 이동·판매·업그레이드·속성·Laboratory 비용 소비자는 Legacy 경로에 유지했다.
- 순수 정책용 Editor 테스트 assembly와 6개 테스트 케이스를 추가하고 솔루션 프로젝트 목록을 동기화했다.
- Resource Economy 기능 문서에 순수 정책, Fusion Adapter, 전환·잔존 Legacy 경계를 기록했다.

## 검증 결과

- Unity Import/Compile: `ProjectIO.ResourceEconomy.dll`, `ProjectIO.ResourceEconomy.Tests.dll`, `Assembly-CSharp.dll` 생성과 IL post-processing 완료. 새 `error CS` 없음.
- `ResourcePaymentPolicyTests`: Unity가 생성한 test assembly에서 충분한 비용·Mineral 부족·Gas 부족·음수 Mineral·음수 Gas·정확한 잔액 6개 케이스 통과.
- 별도 순수 정책 실행 검증: `ResourcePaymentPolicy verification passed.`
- `dotnet build ProjectIO.slnx`: 성공, 오류 0개. 기존 코드와 외부 Package 경고 23개.
- `git diff --check`: 통과.
- 정적 권위 경로 확인: RPC 요청자는 Builder Input Authority와 일치해야 하며, Adapter는 유효한 ResourceSystem State Authority에서만 Networked 잔액을 기록한다. 기존 직접 Tower Build 차감은 제거되어 한 건설 승인에 새 지불 경로만 한 번 실행된다.
- 작업자 1차 수동 테스트: Host Builder 건설과 차감은 통과. Client Builder는 충분한 자원에서도 설치 불가로 표시되어, Client 읽기 경로의 `IsInSimulation` 가드를 원인으로 확인했다.
- Client 읽기 가드 수정 후 `dotnet build ProjectIO.slnx`: 재통과, 오류 0개. `ResourcePaymentPolicyTests` 6개 케이스 재통과.
- 작업자 2차 수동 테스트: Client Builder의 충분성 표시, Tower 건설, 공유 자원 사용 정상 동작 확인.

## 남은 위험

- Late Join 복원 기준은 기존 Networked Mineral·Gas이며 이번 Slice에서 새 지속 상태를 추가하지 않았다.
- 이동·판매·업그레이드·속성·Laboratory의 Legacy 비용 경로는 후속 Slice에서 소비자별로 전환해야 한다.
