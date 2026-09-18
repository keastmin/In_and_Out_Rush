# W-20260918-001 월드 몬스터 시간별 분포와 체력 약화

Status: Completed

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
- 추가 요청: WorldMonsterSpawnSystem Inspector에 제거 시각과 체력 약화 시각을 초 단위로 노출한다(기본 900/1500, 0 이상). 정책에는 설정값을 전달하며 이미 실행된 이벤트는 Inspector 변경으로 재실행하지 않는다.
- 추가 요청: F10 테스트 패널에 월드 몬스터 전체 활성화 토글을 추가한다. 켜면 모든 생존 record를 거리와 관계없이 기존 refresh 생성 예산에 따라 활성화하고, 끄면 기존 플레이어 주변 Chunk 스트리밍으로 복귀한다. 제거·사망 record는 복원하지 않는다. 전체 활성화 중에도 시간 이벤트는 기존 활성/비활성 판정을 그대로 사용한다.

## 읽을 문서와 Skill

- AGENTS.md, Docs/Work/README.md, Docs/PROJECT_MAP.md
- Docs/Features/MonstersAndProjectiles.md, Docs/Features/TrackAndRounds.md
- manage-feature-work, photon-fusion-feature, graphify (기존 그래프 없음)

## 예상 수정 코드

- Assets/02_Scripts/Monster/WorldMonsterSpawnSystem.cs
- Assets/02_Scripts/Monster/Monster.cs
- Assets/02_Scripts/Stage/Network/StageBootstrapper.YOU.cs
- Assets/02_Scripts/Player/Player Runner/Network/PlayerRunner.cs (추가 예약: 테스트 요청·결과와 의존성 주입)
- Assets/02_Scripts/Test Mode/PlayerRunnerTestModeGUI.cs (추가 예약: 토글)
- Assets/02_Scripts/Features/Monster/Logic/WorldMonsterPopulationPolicy.cs 및 .meta (신규)
- Assets/02_Scripts/Features/Monster/Tests/WorldMonsterPopulationPolicyTests.cs 및 .meta (신규)
- Docs/Features/MonstersAndProjectiles.md
- Docs/Features/TestMode.md (추가 예약: 설정 위치·토글 사용법)
- 본 작업 문서 (완료 시 Docs/Work/Completed/로 이동)

## 예약 Scene·Prefab·Data Asset

Assets/01_Scenes/GameScene.unity (최종 단계에서 작업자가 명시적으로 포함 요청). 기존 Spawn Table 수량·반경 유지.

작업자가 저장한 GameScene.unity의 제거 900초·체력 약화 1500초 및 테스트 NetworkBool 초기값 false를 최종 커밋에 포함한다. Scene 변경은 새 직렬화 값 6줄 추가이며 기존 fileID·guid 참조 변경은 없다.

## 공용 계약 또는 Bootstrapper 변경

- StageBootstrapper.YOU.cs에서 기존 TimeSystem을 WorldMonsterSpawnSystem에 명시적으로 전달한다. TimeSystem 시간 진행 규칙은 변경하지 않는다.
- Monster에 권위 검사를 갖춘 체력 복원 진입점을 추가한다. Track Monster의 기존 생성·스탯 규칙은 유지한다.
- 초기 분포와 제거 확률 계산은 기존 Monster Logic assembly의 순수 정책으로 분리한다.
- 추가 예약: Bootstrapper가 Host의 PlayerRunner에 스폰 시스템을 명시적으로 전달한다. 테스트 UI는 PlayerRunner의 요청·복제 결과만 사용하며 전역 검색으로 시스템을 찾지 않는다.

## 네트워크·Peer 동등성

- 플레이어 입력 없이 스테이지 시간으로 실행되는 시스템 기능이다. Input Authority 요청·RPC 추가 없음.
- State Authority만 초기 좌표 추첨, 제거, 체력 변경, Spawn/Despawn을 수행한다. 시간 경계 처리는 streaming refresh에 앞서 진행하며 각 이벤트는 한 번만 실행한다.
- Host와 Client는 기존 Networked Health 및 NetworkObject 위치·수명 복제로 같은 결과를 관찰한다. 지속 상태를 RPC로 전달하지 않는다.
- 비활성 상태는 Host record에 유지하고 Spawned의 기본 체력 초기화 이후 저장 체력을 복원하여 덮어쓰기를 방지한다.
- Late Join은 현재 활성 개체의 복제 상태를 받는다. 이후 스트리밍 Spawn도 보존 체력으로 생성한다.
- 준비되지 않은 TimeSystem의 네트워크 상태를 읽지 않으며 teardown에서 참조와 일회성 플래그를 정리한다.
- 실제 Host·Client 실행: 899→900, 1499→1500 경계를 넘기고 활성 개체 유지, 비활성 제거, 체력 1/4, 반복 tick 중복 적용 방지, Chunk 왕복 후 체력 유지, Late Join과 Scene 종료를 각각 확인한다. 실행하지 못한 항목은 미검증으로 기록한다.
- 추가 테스트 토글은 Editor/Development Build 전용이다. Input Authority의 PlayerRunner에서 요청하며 Client 요청은 해당 PlayerRunner의 InputAuthority→StateAuthority RPC로 전달한다. Host 로컬도 동일한 권위 검증·변경 경로를 사용한다. 준비 상태와 적용 결과는 복제 상태로 UI에 표시하고, 준비 전 조작은 비활성화한다. Release에는 테스트 조작을 제공하지 않는다.
- 전체 활성화는 GameObject.SetActive가 아니라 기존 Fusion Spawn/Despawn 및 record 수명 경로를 이용한다. 켜진 동안 전체 record를 후보로 삼고 거리로 Despawn하지 않는다. 토글을 끌 때 현재 체력을 보존하며 기존 Chunk 범위 밖 개체를 비활성화한다.

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
- 추가 요청: Inspector에서 10/20초로 설정해 각 이벤트가 한 번 발생하는지, 임계 시각을 지난 뒤 낮추거나 높여도 중복 적용하지 않는지 검사한다.
- 추가 요청: F10 토글 ON/OFF를 Host 로컬·Client 입력 각각에서 확인한다. ON은 살아 있는 전체 record만 점진적으로 활성화하고 OFF는 주변 Chunk로 복귀하며 체력·사망·제거 상태를 보존해야 한다. Fog/AOI 표현 규칙은 변경하지 않으며 Scene View와 Hierarchy에서 생성 위치를 확인할 수 있다.

## 실제 변경

- 예약 Commit `bd46ed7` Push 및 VerifyReservation 통과 후 구현.
- WorldMonsterPopulationPolicy와 대응 NUnit 테스트·meta 추가.
- 초기 외곽 가중 분포, 전체 비활성 record의 900초 확률 제거, 전체 생존 record의 1500초 현재 체력 약화 및 스트리밍 체력 보존 추가.
- Monster.TryRestoreCurrentHealth에 권위·네트워크 준비 상태·유효한 양수 검사 추가. 최대 체력과 다른 스탯 유지.
- StageBootstrapper.YOU에서 TimeSystem 명시 주입. 기능 문서 갱신. 작업자가 저장한 GameScene.unity 설정 포함. Prefab·Data Asset 변경 없음.
- 작업자 요청에 따라 추가 예약 절차 없이 테스트 편의 기능을 함께 구현: Inspector 시간 설정, F10 전체 활성화/주변 Chunk 복귀 토글, Scene Gizmos 위치 표시, 읽기 전용 현재·최대 체력 Inspector.
- 추가 실제 파일: Assets/Editor/MonsterEditor.cs 및 Assets/Editor/MonsterEditor.cs.meta. 테스트 편의 기능의 별도 문서 작성은 작업자 지시에 따라 생략.

## 검증 결과

- CheckStart 통과. 샌드박스 밖에서 동기화 확인 완료.
- 기존 스폰 시스템, 체력 초기화, TimeSystem 연결부 정적 조사 완료.
- 새 NUnit 테스트 소스를 실제 정책 소스와 독립 .NET 9 실행기로 컴파일·실행: 최종 13/13 통과. 고정 seed 10만 표본으로 동일 면적 고리별 초기 분포와 제거 후 기대 밀도를 확인. 사용자 지정 시간·변경 후 중복 실행 방지 포함.
- 생성 csproj의 실제 source/reference/define 목록을 이용한 Roslyn 컴파일: ProjectIO.Monsters 및 전체 Assembly-CSharp 오류 0개. 전체 Assembly-CSharp에는 미사용·직렬화 필드 등 경고 256개 출력.
- 산출물은 무시되는 Temp/WorldMonsterPopulationValidation/에 저장(PolicyTests.log, Assembly-CSharp.log 및 response files). 저장소 테스트는 Unity EditMode에서도 실행 가능.
- git diff --check 통과. 변경 파일은 예약 범위 안에 있음.
- 실제 Unity import/Fusion weaving 및 Host·Client 런타임은 수행하지 않음. 열린 Unity 세션은 변경하지 않았음.
- 후속 직접 C# 컴파일: Editor·Development·Release 조건 모두 통과. MonsterEditor를 포함한 Assembly-CSharp-Editor도 오류 0개.
- 작업자는 현재 체력 감소를 확인했고, 비활성 개체만 시간 제거된다는 동작을 확인한 뒤 최종 진행을 요청함. Host·Client 양쪽 및 Late Join의 개별 검증 증거는 제공되지 않았으므로 해당 항목은 미검증으로 유지.

## 남은 위험

- Fusion Spawned 이후 체력 복원 순서 및 Client/Late Join 표시의 실제 런타임 검증 필요.
- 확률 제거이므로 적은 개체 수에서는 분포 편차가 발생한다.

## 작업자 수동 검증 절차 (미검증)

1. Unity import 후 Console 컴파일/Fusion weaving 오류가 없는지 확인하고 Test Runner에서 WorldMonsterPopulationPolicyTests를 실행한다.
2. Host와 Client로 입장한다. Host 디버거에서 record의 PivotPosition/SpawnRadius를 확인해 초기 외곽 집중과 Territory 제외를 확인한다.
3. 일부 몬스터를 활성 상태로 유지한 채 TimeSystem.ElapsedTime 899초에서 900초를 통과한다(빠른 검증은 Host 디버거의 elapsed 값 조정). 전체 비활성 record 중 외곽에서 더 많이 IsDestroyed가 설정되고 활성 몬스터는 유지되어야 한다. 다음 tick·Chunk 왕복에서 재판정하지 않아야 한다.
4. 최대 체력 10인 개체를 7까지 피해 입힌 뒤 하나는 활성, 하나는 Chunk 밖 비활성으로 둔다. 1499초에서 1500초 통과 후 양쪽 현재 체력이 1.75인지 확인한다. 미생성 체력 10 record는 2.5여야 하며 MaxHealth는 유지한다.
5. 이후 추가 피해를 입히고 Chunk를 반복 왕복한다. 마지막 현재 체력이 유지되고 추가 1/4 적용·체력 회복이 없어야 한다. 사망·제거된 개체는 다시 생성되지 않아야 한다.
6. Client에서 중복 이벤트가 없고 Host와 체력이 일치하는지 확인한다. 비권위 TryRestoreCurrentHealth는 false이며 Health를 바꾸지 않아야 한다. 1500초 이후 Late Join도 현재 체력을 받아야 한다.
7. Stage 종료·재입장 시 이전 플래그·record가 남지 않고 새 Stage 시간 이벤트가 각각 한 번 실행되어야 한다.

작업자의 최종 진행 요청 및 Scene 포함 요청에 따라 완료 처리 및 구현·GameScene.unity Commit·Push를 진행한다.
