# W-20260822-006 Strider 슬라이드 종료 감속

Status: Reserved

## 동기화 기준

- Base Commit: 110563105055488505138c6782312914d0201130
- 공통 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Monster와 Projectile 중 Strider 이동

## 목표

현재 Strider가 슬라이드 목적지까지 일정 속도로 이동한 뒤 즉시 멈추는 동작을, 기존 목적지·경로 안전성·State Authority 계약을 유지하면서 목적지에 가까워질수록 속도가 줄어드는 ease-out 이동으로 변경한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/MonstersAndProjectiles.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Monster/Strider.cs`

## 예약 Scene·Prefab·Data Asset

없음. `Strider.prefab`의 기존 이동 설정과 NetworkObject 구성은 유지한다.

## 공용 계약 또는 Bootstrapper 변경

없음. `Monster`, `WorldMonster`, `WorldMonsterSpawnSystem`, Networked 필드, Spawn 경로, RPC는 변경하지 않는다.

## 네트워크·Peer 동등성

- 입력과 RPC가 없는 AI 이동이며, 기존처럼 `Monster.FixedUpdateNetwork`와 `Strider.UpdateMonster`의 `Object.HasStateAuthority` 경로만 이동 결정을 수행한다.
- State Authority는 기존 Networked `State`, `SlideTarget`, `SlideDirection`, `RestTimer`와 Rigidbody velocity를 사용해 목적지와 감속 이동을 결정한다. 새 지속 상태나 RPC는 추가하지 않는다.
- Host와 Client는 기존 NetworkTransform/NetworkRigidbody 복제 결과를 관찰하며, Client가 목적지나 속도를 직접 변경하지 않는다.
- Host 로컬과 Client 관찰 결과, 권한 없는 호출, Stun 정지, Late Join, Despawn 정리, 중복 실행은 기존 계약을 유지하는 범위에서 확인한다.
- 실제 Host·Client 동시 실행이 불가능하면 정적 검토와 단일 실행 결과를 구분하고, 작업자가 수행할 수동 절차를 결과에 기록한다.

## 완료 조건

- 슬라이드 초반 이동 방향과 최대 속도는 기존과 같고, 목적지에 가까워질수록 속도가 감소한다.
- 도착 시 목적지를 통과하거나 영구 정지하지 않고 기존 Resting 상태와 RestTimer로 전환한다.
- 목적지 선택, Territory/Sanctuary·장애물·자원 Collider 경로 차단, State Authority guard에 회귀가 없다.
- 집중 컴파일 또는 관련 테스트, `git diff --check`를 실행하고 결과와 미검증 Host·Client 항목을 기록한다.

## 실제 변경

예약 단계. 구현 후 기록한다.

## 검증 결과

예약 단계. 구현 후 기록한다.

## 남은 위험

- 감속 비율과 도착 임계치가 짧은 슬라이드에서도 이동 완료를 보장하는지 확인해야 한다.
- Unity Host·Client 동시 실행이 불가능하면 실제 Peer별 Transform 복제와 Late Join은 미검증으로 남긴다.
