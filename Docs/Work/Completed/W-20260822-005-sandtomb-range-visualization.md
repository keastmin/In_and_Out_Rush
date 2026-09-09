# W-20260822-005 SandTomb 범위 시각화 확장

Status: Complete

## 동기화 기준

- Base Commit: 4b183fba844a4327130665fb225bcfc3703f3e29
- 공용 Upstream: origin/rebuild-development-environment
- 담당: Codex

## 기능

SandTomb의 현재 단일 활성화 범위 메쉬 표시를 활성화 범위와 끌어당김 범위를 함께 표시하는 투명 시각 레이어로 확장한다.

## 목표

- `_activationRadius` 내부 원과 `_suckedIntoRadius` 외곽 범위를 동시에 표시한다.
- 외곽 범위는 활성화 범위와 겹치지 않는 링 형태로 구성해 투명도 누적과 Z-fighting을 줄인다.
- 기존 64분할 프로시저럴 메쉬 구조를 재사용하고, 메쉬 콜라이더나 게임플레이 판정은 추가하지 않는다.
- 기존 Networked `State`의 활성/비활성 표시 계약은 유지하고, 두 시각 레이어에 동일하게 적용한다.
- 기존 URP 몬스터 머티리얼을 범위 표시용 투명 설정으로 조정하고, 렌더러별 알파는 공유 머티리얼을 복제하지 않는 방식으로 적용한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/MonstersAndProjectiles.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Monster/SandTomb.cs`

## 예약 Scene·Prefab·Data Asset·Material

- `Assets/03_Prefabs/Field/SandTomb.prefab`
- `Assets/04_Materials/Monster/Active Centipede.mat`
- `Assets/04_Materials/Monster/Inactive Centipede.mat`

## 예상 수정 문서

- `Docs/Features/MonstersAndProjectiles.md`

## 공용 계약 또는 Bootstrapper 변경

없음. SandTomb의 기존 직렬화 필드, Fusion 상태, Spawn 계약, Bootstrapper 연결부는 변경하지 않는다.

## 네트워크·Peer 동등성

새로운 Fusion 상태·Spawn·RPC·Authority 계약은 없다. 기존 Networked `State`와 `OnChangedRender`가 Host, Client, Late Join에서 두 범위 렌더러의 활성/비활성 표시를 동일하게 갱신하도록 유지한다. Despawn과 `OnDestroy`에서 런타임 메쉬가 정리되는지 확인한다.

## 다른 Active 작업과 겹치는 부분

없음. `W-20260822-004-gpu-chunk-mask-presentation.md`는 Territory Chunk presentation 파일과 리소스만 예약하며, 본 작업의 SandTomb 코드·Prefab·Material·기능 문서와 겹치지 않는다.

## 완료 조건

- 활성화 원과 끌어당김 외곽 링이 각각의 직렬화 반지름과 일치한다.
- 두 레이어가 투명하게 표시되고, 범위 중첩에 따른 과도한 알파 누적이나 Z-fighting이 없다.
- 기존 활성화 판정, 끌어당김 판정, 피해, 타이머, Despawn 동작을 변경하지 않는다.
- Host/Client 시각 상태와 Late Join 초기 표시를 가능한 범위에서 확인하고, 실행하지 못한 검증은 결과에 남긴다.
- 집중 검증, `git diff --check`, 변경 파일 및 문서 상태를 기록한다.

## 실제 변경

- `Assets/02_Scripts/Monster/SandTomb.cs`: 활성화 원과 끌어당김 외곽 링 메쉬 생성, 두 렌더러의 상태 머티리얼·알파 적용, `MaterialPropertyBlock` 지연 생성, 런타임 메쉬 정리
- `Assets/03_Prefabs/Field/SandTomb.prefab`: `Sucked Into Range` 시각 자식과 직렬화된 렌더러·알파 설정 추가
- `Assets/04_Materials/Monster/Active Centipede.mat`
- `Assets/04_Materials/Monster/Inactive Centipede.mat`: URP Alpha Transparent 설정과 기본 알파 조정
- `Docs/Features/MonstersAndProjectiles.md`: SandTomb 범위 표시 계약 기록

## 검증 결과

- `dotnet build Assembly-CSharp.csproj --no-restore`: 통과, 오류 0개. 기존 프로젝트 경고 13개가 남아 있다.
- 작업자 런타임 로그의 `MaterialPropertyBlock` null 예외를 재현 원인으로 확인하고, 지연 생성 수정 후 같은 빌드를 다시 실행해 오류 0개를 확인했다.
- `git diff --check`: 통과.
- SandTomb Prefab의 정의 fileID 중복 없음과 새 자식·렌더러·부모·직렬화 참조 존재를 정적 확인했다.
- 두 범위 머티리얼의 `RenderType: Transparent`, Alpha blend, `ZWrite: 0` 설정을 정적 확인했다.
- Unity Editor 실행 파일이 없어 실제 메쉬 화면과 URP 렌더링 결과는 확인하지 못했다.
- Host, Client, Late Join, Despawn 런타임 검증은 실행하지 못했다.

## 남은 위험

- URP 투명 머티리얼의 렌더 순서와 지면 깊이 정렬에 따라 카메라 각도별 겹침이 달라질 수 있다.
- Unity Editor/Host·Client 런타임을 실행하지 못했으므로 두 범위의 실제 색상·알파·Peer 동등성은 작업자 수동 확인이 필요하다.
