# W-20260820-001 성소 타워 건설 영역

Status: Reserved

## 동기화 기준

- Base Commit: 22618452cd61c6f4663d47a6cca3706c93b12d59
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Sacred Zone·Sanctuary·Gate, Grid, Tower

## 목표

Runner가 활성화한 Sanctuary의 셀을 Territory 밖에서도 Builder가 타워를 설치할 수 있는 영역으로 취급하고, Sanctuary가 소멸할 때 그 영역에 설치된 타워를 State Authority가 자동 Despawn한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/SacredZoneSanctuaryGate.md`
- `Docs/Features/GridAndObstacles.md`
- `Docs/Features/PlayerBuilder.md`
- `Docs/Features/TowerAndLaboratory.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Grid/InfiniteGrid.cs`
- `Assets/02_Scripts/Stage/Network/StageBootstrapper.YOU.cs`
- `Docs/Features/SacredZoneSanctuaryGate.md`
- `Docs/Features/GridAndObstacles.md`
- `Docs/Features/TowerAndLaboratory.md`
- `Docs/Work/Active/W-20260820-001-sanctuary-tower-build-area.md`

## 예약 Scene·Prefab·Data Asset

없음

## 공용 계약 또는 Bootstrapper 변경

- `StageBootstrapper.YOU`가 로컬 Sanctuary 활성·소멸 수명주기를 `InfiniteGrid`에 등록·해제한다.
- `InfiniteGrid`의 Territory 필수 건설 판정은 Territory 또는 활성 Sanctuary 내부 셀을 허용한다.
- State Authority의 `InfiniteGrid`가 소멸한 Sanctuary와 겹치는 타워의 Grid 점유를 해제하고 Fusion Despawn한다.

## 다른 활성 작업과 겹치는 부분

없음. `Docs/Work/Active/README.md` 외 기존 Active 예약이 없다.

## 범위 밖

- Sanctuary 생성 규칙, 지속 시간, 시각 효과 변경
- Territory 형상 또는 Chunk Territory 저장·복제 계약 변경
- Tower 비용, 환불, 공격·버프 규칙 변경
- Scene, Prefab, ScriptableObject 변경

## 완료 조건

- 활성 Sanctuary의 모든 Tower footprint 셀이 Territory 밖이어도 빈 셀이고 Track 차단이 아니면 Builder 미리보기와 Host 권위 검증이 건설을 허용한다.
- 비활성 또는 소멸 Sanctuary는 건설 가능 영역에 포함되지 않는다.
- Sanctuary 소멸 시 그 영역과 겹치는 타워의 Grid 점유가 해제되고 State Authority가 NetworkObject를 Despawn한다.
- Center Tower 수량과 기존 Tower Despawn 정리 경로가 일관되게 유지된다.
- 프로젝트 컴파일, 관련 집중 검증, `git diff --check`, `git status` 결과를 기록한다.

## 실제 변경

- `InfiniteGrid`에 활성 Sanctuary 건설 영역 등록·해제와 `Territory 또는 활성 Sanctuary` 셀 판정을 추가했다.
- Builder 미리보기와 로컬 사전 판정, Host의 Tower Spawn 점유 검증이 공통 `InfiniteGrid.IsCellBuildBlocked` 경로를 통해 Sanctuary를 허용한다.
- Sanctuary 소멸 시 State Authority가 겹치는 Tower footprint를 수집하고 Grid 점유, Track 파괴 예약, Center Tower 수량을 정리한 뒤 Fusion `Runner.Despawn`을 호출한다.
- `StageBootstrapper.YOU`가 모든 Peer의 Sanctuary 활성·소멸 이벤트를 Grid 등록 상태에 연결하고, 실제 Tower 제거는 Grid의 State Authority 검사로 제한했다.
- Sacred Zone·Sanctuary·Gate, Grid, Tower 기능 문서에 새 연결과 검증 항목을 기록했다.

## 검증 결과

- `dotnet build ProjectIO.slnx`: 성공, 오류 0개. 기존 코드와 외부 Package 경고 23개.
- `dotnet test ProjectIO.slnx --no-build`: 종료 코드 0. 이 변경을 직접 실행하는 독립 테스트 출력은 없었다.
- Unity Editor assembly reload 로그: 새 `error CS` 없음. 기존 미사용 필드 경고만 확인했다.
- `git diff --check`: 통과.
- 정적 경로 확인: Client 미리보기와 Host RPC 재검증이 동일한 Grid 건설 판정을 사용하며, 권한 없는 Peer는 Sanctuary 등록만 해제하고 NetworkObject를 Despawn하지 않는다.
- 작업자 수동 런타임 테스트: 2026-08-20 완료 확인.

## 남은 위험

- Late Join 시 Sanctuary 자체의 로컬 수명주기 복원은 기존 구현 계약을 따르며 이번 범위에서 네트워크 상태로 전환하지 않았다. Host의 authoritative 건설 검증과 Despawn은 유지된다.
