# W-20260918-001 월드 몬스터 시간별 분포와 체력 약화

Status: Reserved

## 동기화 기준

- Base Commit: 9e22e028dfa9eabb6cd58e06ed5e2d286e38ad4e
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex (현재 작업 세션)

## 기능

World Monster 초기 배치, Chunk 스트리밍, 스테이지 시간별 개체 제거와 체력 보존.

## 목표

- 초기 생성 반경과 개체 수는 기존 테이블을 유지한다. 원점 중심 XZ 원의 면적당 밀도를 정규화 거리 t에 대해 1+2t로 설정한다. 가장자리 밀도는 중심의 3배다.
- TimeSystem.ElapsedTime이 900초 이상이 되는 첫 권위 tick에서 그 순간 비활성인 생존 record 전체를 한 번 검사한다. 그룹 생성 반경에 대한 현재 위치의 정규화 거리 t를 0~1로 제한하고 확률 1-1/(1+2t)로 영구 제거한다. 활성 개체는 유지하며 나중에 비활성화되어도 소급 제거하지 않는다.
- 1500초 이상이 되는 첫 권위 tick에서 활성·비활성 모든 생존 record의 현재 체력을 한 번만 0.25배로 줄인다. 최대 체력·공격력·이동속도는 유지한다.
- 비활성 전환 시 현재 체력을 저장하고 재생성 때 복원한다. 아직 생성되지 않은 record의 현재 체력은 프리팹 기본 체력으로 시작한다. 사망·제거 record는 부활하지 않는다.

## 읽을 문서와 Skill

- AGENTS.md, Docs/Work/README.md, Docs/PROJECT_MAP.md
- Docs/Features/MonstersAndProjectiles.md, Docs/Features/TrackAndRounds.md
- manage-feature-work, photon-fusion-feature, graphify (기존 그래프 없음)

## 예상 수정 코드

- Assets/02_Scripts/Monster/WorldMonsterSpawnSystem.cs
- Assets/02_Scripts/Monster/Monster.cs
- Assets/02_Scripts/Stage/Network/StageBootstrapper.YOU.cs
- Assets/02_Scripts/Features/Monster/Logic/WorldMonsterPopulationPolicy.cs 및 .meta (신규)
- Assets/02_Scripts/Features/Monster/Tests/WorldMonsterPopulationPolicyTests.cs 및 .meta (신규)
- Docs/Features/MonstersAndProjectiles.md
- 본 작업 문서 (완료 시 Docs/Work/Completed/로 이동)

## 예약 Scene·Prefab·Data Asset

없음. 기존 Spawn Table 수량·반경 및 Scene 직렬화 값 유지.

## 공용 계약 또는 Bootstrapper 변경

- StageBootstrapper.YOU.cs에서 기존 TimeSystem을 WorldMonsterSpawnSystem에 명시적으로 전달한다. TimeSystem 시간 진행 규칙은 변경하지 않는다.
- Monster에 권위 검사를 갖춘 체력 복원 진입점을 추가한다. Track Monster의 기존 생성·스탯 규칙은 유지한다.
- 초기 분포와 제거 확률 계산은 기존 Monster Logic assembly의 순수 정책으로 분리한다.

## 네트워크·Peer 동등성

- 플레이어 입력 없이 스테이지 시간으로 실행되는 시스템 기능이다. Input Authority 요청·RPC 추가 없음.
- State Authority만 초기 좌표 추첨, 제거, 체력 변경, Spawn/Despawn을 수행한다. 시간 경계 처리는 streaming refresh에 앞서 진행하며 각 이벤트는 한 번만 실행한다.
- Host와 Client는 기존 Networked Health 및 NetworkObject 위치·수명 복제로 같은 결과를 관찰한다. 지속 상태를 RPC로 전달하지 않는다.
- 비활성 상태는 Host record에 유지하고 Spawned의 기본 체력 초기화 이후 저장 체력을 복원하여 덮어쓰기를 방지한다.
- Late Join은 현재 활성 개체의 복제 상태를 받는다. 이후 스트리밍 Spawn도 보존 체력으로 생성한다.
- 준비되지 않은 TimeSystem의 네트워크 상태를 읽지 않으며 teardown에서 참조와 일회성 플래그를 정리한다.
- 실제 Host·Client 실행: 899→900, 1499→1500 경계를 넘기고 활성 개체 유지, 비활성 제거, 체력 1/4, 반복 tick 중복 적용 방지, Chunk 왕복 후 체력 유지, Late Join과 Scene 종료를 각각 확인한다. 실행하지 못한 항목은 미검증으로 기록한다.

## 다른 활성 작업과 겹치는 부분

없음. CheckStart 결과 AHEAD=0, BEHIND=0이며 기존 Active 예약 없음.

## 범위 밖

Track Monster 변경, 900초 재배치·추가 보충, 활성 몬스터 제거, 지형/장애물 배치 규칙 변경, 스폰 수량·반경 변경, Host migration.

## 완료 조건

- 초기 3배 밀도와 900초 제거 확률을 순수 정책 테스트로 검증한다. 균등 분포는 비활성 모집단의 기대 밀도이며 활성 개체·Territory 제외 영향으로 전체의 정확한 균등 배치는 보장하지 않는다.
- 중심·중간·가장자리 및 반경 0 경계를 검사한다. 시간 임계값 통과·중복 실행 방지와 1500초 이전 피해를 받은 개체의 보존 체력을 확인한다.
- 900초 제거는 플레이어 주변 후보만이 아니라 전체 비활성 record를 대상으로 한다.
- 1500초 이후 Chunk 왕복 시 체력 회복·추가 1/4 적용이 없어야 한다. 죽은 개체는 복원하지 않는다.
- 가능한 집중 테스트·컴파일, git diff --check, git status를 확인하고 Host·Client 실행 여부를 명시한다.

## 실제 변경

예약 문서만 작성. 구현 전.

## 검증 결과

- CheckStart 통과. 샌드박스 밖에서 동기화 확인 완료.
- 기존 스폰 시스템, 체력 초기화, TimeSystem 연결부 정적 조사 완료.

## 남은 위험

- Fusion Spawned 이후 체력 복원 순서 및 Client/Late Join 표시의 실제 런타임 검증 필요.
- 확률 제거이므로 적은 개체 수에서는 분포 편차가 발생한다.
