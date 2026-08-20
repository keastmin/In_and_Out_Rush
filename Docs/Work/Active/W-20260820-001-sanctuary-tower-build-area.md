# W-20260820-001 성소 타워 건설 영역

Status: Reserved

## 동기화 기준

- Base Commit: `22618452cd61c6f4663d47a6cca3706c93b12d59`
- 공용 Upstream: `origin/rebuild-development-environment`

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

예약 단계.

## 검증 결과

예약 단계.

## 남은 위험

- 실제 Host·Client 런타임 검증은 구현 후 환경에서 가능한 범위를 확인한다.
