# W-20260821-001 SandTomb 벌레지옥 사선 중단 및 재개

Status: Completed

## 동기화 기준

- Base Commit: 3cc8e8dcf0ecbb29948cb6f94d2357b01434d9fa
- 공용 Upstream: origin/rebuild-development-environment

## 담당

Codex

## 기능

Monster and Projectile, Territory, Player Runner

## 목표

SandTomb이 activation radius 진입으로 활성화된 뒤 4초 동안, 흡입 반경에 진입한 Runner의 Territory 사선을 해당 진입 위치에서 일시 정지하고 흡입한다. 반경 이탈 또는 폭발 Despawn 시 정지점과 현재 위치를 하나의 직선으로 확정한 뒤 기존 사선 갱신을 재개한다. 활성 중 재진입마다 새 정지 구간을 시작한다. 폭발 시 흡입 반경 안의 Runner에게 폭발 시점의 최대 체력 10%와 현재 체력 30%의 합을 피해로 적용한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/MonstersAndProjectiles.md`
- `Docs/Features/Territory.md`
- `Docs/Features/PlayerRunner.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Monster/Monster.cs`
- `Assets/02_Scripts/Monster/SandTomb.cs`
- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/03_Prefabs/Field/SandTomb.prefab`
- `Docs/Features/MonstersAndProjectiles.md`
- `Docs/Features/Territory.md`
- `Docs/Work/Active/W-20260821-001-sandtomb-trail-suspension.md`

## 예약 Scene·Prefab·Data Asset

- `Assets/03_Prefabs/Field/SandTomb.prefab`

Scene, ScriptableObject, ProjectSettings, Package 변경은 없다.

## 공용 계약 또는 Bootstrapper 변경

- `Monster`가 등록한 `TerritorySystem` 참조를 파생 Monster가 사용하도록 보호 범위로 노출한다.
- `TerritorySystem`은 State Authority 전용 외부 강제 이동 사선 정지·재개 계약을 제공한다. 사선 경로, 복제 RPC, 교차 및 영토 확장 규칙은 기존 구현이 계속 소유한다.
- `StageBootstrapper`, PlayerRunner public API, Network prefab table은 변경하지 않는다.

## 네트워크·Peer 동등성

- Input Authority: Host 및 Client Runner의 기존 이동 입력이 위치를 만든다. SandTomb은 별도 입력이나 RPC 요청을 받지 않는다.
- State Authority: SandTomb의 활성화, 4초 TickTimer, 흡입, 사선 정지·재개 지시, 폭발 피해 및 Despawn을 한 번만 결정한다. TerritorySystem도 State Authority에서만 경로와 영토를 변경한다.
- 복제: 기존 Territory path RPC가 정지점→현재 위치 연결선을 Proxies에 전달하고, SandTomb의 Networked state와 TickTimer가 활성 상태를 Late Join peer에 복제한다. 피해는 PlayerRunner Health Networked 상태로 관찰한다.
- Host/Client: 두 Peer 모두 자신의 Runner 이동에 대해 같은 정지 사선, 연결선, 흡입 위치, 폭발 피해 및 Despawn 결과를 본다. Host는 State Authority와 로컬 표현을 중복 실행하지 않는다.
- 준비·정리: TerritorySystem 또는 Runner 참조가 없는 경우 authoritative mutation을 하지 않으며, 반경 이탈·폭발·예상치 못한 Despawn에서 정지 구간을 한 번만 복구한다. Late Join은 기존 활성 사선 경로의 한계를 유지하되, 이후 연결 및 Health 복제를 관찰한다.

## 다른 활성 작업과 겹치는 부분

- `W-20260820-006-match-progression-transition-policy.md`는 Match Progression 및 `TimeSystem`만 예약한다. 이 작업은 해당 코드, asmdef, 문서를 수정하지 않는다.
- 현재 Active 예약과 코드, Prefab, Bootstrapper, Network spawn 경계의 직접 충돌은 없다.

## 범위 밖

- WorldMonster Spawn 규칙과 SandTomb 배치/간격
- PlayerRunner 이동, 체력, UI, 공용 damage API의 구조 변경
- Territory 알고리즘, Trail chunk renderer, Lifeline 규칙의 일반적 리팩터링
- Scene, Network prefab table, VFX, 신규 폭발 반경 Asset

## 완료 조건

- 활성 SandTomb은 4초 뒤 폭발·Despawn하며, 폭발 순간 `_suckedIntoRadius` 안의 비보호 Runner에게 `MaxHealth * 0.10f + Health * 0.30f` 피해를 한 번 적용한다.
- 활성 중 흡입 반경 진입마다 현재 위치를 새 포인트 1로 저장하고, 해당 구간에서 사선 갱신을 멈추며 흡입한다.
- 반경 이탈, 폭발 또는 기타 Despawn은 포인트 1→현재 위치 연결선을 실제 Territory 경로와 복제 사선에 추가하고, 현재 위치에서 사선 갱신을 재개한다.
- 연결선의 자기 교차는 기존 Lifeline 또는 사망 규칙을 적용하고, 현재 위치가 영토 내부면 연결 후 기존 영토 확장으로 종료한다.
- Host 및 Client Runner의 사선 표시, 흡입, 폭발 피해, Despawn 결과를 별도로 확인하거나, 실행하지 못한 항목과 수동 절차를 기록한다.
- 관련 프로젝트 컴파일, `git diff --check`, `git status`를 확인한다.

## 실제 변경

예약 단계.

## 검증 결과

예약 단계.

## 잔여 위험

- SandTomb과 TerritorySystem의 FixedUpdate/Render 순서 차이로 정지·재개 프레임의 경로 점이 달라질 수 있어 Host와 Client runtime 검증이 필요하다.
- 기존 Territory path RPC는 Late Join 시 진행 중 경로를 재구성하지 않으므로, 이 작업은 그 기존 범위를 확대하지 않는다.

## 구현 결과

- `SandTomb`은 4초 Networked 폭발 타이머, 반경 진입별 사선 정지·흡입, 반경 이탈·폭발·Despawn의 사선 재개, 폭발 피해식을 사용한다.
- `TerritorySystem`은 State Authority 전용 사선 정지·재개 API로 연결선을 path RPC에 즉시 반영하고, 기존 교차·Lifeline·영토 확장 흐름을 사용한다.
- `SandTomb.prefab`은 `_explosionDelay: 4`로 갱신했다.

## 검증 기록

- `git diff --check` 통과.
- `dotnet build ProjectIO.slnx --no-restore`는 Unity가 생성하는 `Temp/obj/*/project.assets.json` 부재로 시작 전 실패했다. 코드 컴파일과 실제 Host·Client 실행은 Unity Editor에서 별도로 확인해야 한다.
- `dotnet build ProjectIO.Monsters.csproj` 및 `dotnet build Assembly-CSharp.csproj`는 오류 없이 통과했다. 후자는 기존 경고 16개를 출력했다.
- Unity Editor에서 Host와 Client를 함께 실행하는 runtime 검증은 이 환경에서 수행하지 못했다. 각 Peer에서 진입·이탈·재진입 사선, 폭발 반경 피해, Despawn 정리를 확인해야 한다.
