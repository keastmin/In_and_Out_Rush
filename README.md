# IN & OUT: RUSH

두 플레이어가 **Runner**와 **Builder**로 역할을 나눠 전장을 개척하고 방어선을 완성하는 2인 비대칭 협동 액션 디펜스입니다.

| 구분 | 내용 |
|---|---|
| 개발 형태 | 2인 팀 프로젝트 · AI-assisted development |
| 엔진 | Unity 6 (`6000.0.69f1`) · C# · URP |
| 네트워크 | Photon Fusion 2 · Host/Client · State Authority 기반 |
| 핵심 플레이 | 영역 확장 · 실시간 전투 · 타워 건설 · 자원 운용 · 라운드 방어 |
| 프로젝트 상태 | 핵심 멀티플레이 시스템 개발 및 통합 진행 중 |

## 게임 소개

Runner는 안전 영역 밖으로 진입해 몬스터와 교전하고, 다시 영역 안으로 복귀해 이동 경로만큼 Territory를 확장합니다. Builder는 확보된 공간과 자원을 바탕으로 타워를 건설·이동·판매하며 방어선을 관리합니다.

두 역할은 따로 움직이지만 하나의 전황을 공유합니다. Runner가 넓힌 공간은 자원과 건설 기회를 만들고, Builder가 만든 방어선은 Runner가 더 멀리 탐색할 여유를 제공합니다. 라운드와 몬스터 웨이브를 돌파하고 Sacred Zone, Sanctuary, Gate로 이어지는 스테이지 흐름을 완수하는 것이 목표입니다.

### 플레이 역할

| Runner | Builder |
|---|---|
| 이동, 달리기, 슬라이드와 회피 | Grid 기반 타워 배치와 위치 검증 |
| 산탄총·돌격소총·쌍권총 전투 규칙 | 타워 건설·이동·판매와 자원 지불 |
| 영역 밖 이동 경로를 이용한 Territory 확장 | 공격·지원 타워와 Laboratory 운용 |
| 아이템·스킬 사용과 월드 몬스터 대응 | 확장된 Territory와 Sanctuary의 방어선 관리 |

### 핵심 플레이 루프

1. Runner가 영역 밖을 탐색하고 전투하며 확장 경로를 만든다.
2. 영역 재진입 시 Host가 경로를 검증하고 Territory를 확정한다.
3. 확장에 따라 자원이 확보되고 Builder의 건설 가능 구역이 넓어진다.
4. Builder가 자원을 지불해 타워를 배치하고 라운드 방어선을 조정한다.
5. 두 플레이어가 웨이브와 특수 구역을 돌파해 Gate까지 진행한다.

## 구현에서 집중한 문제

### 권위 서버 기반의 2인 플레이

플레이어 입력은 각 Input Authority에서 수집하고, 게임 결과를 바꾸는 판정은 State Authority가 수행합니다. 이동, 무기 탄약과 재장전, 투사체, 자원, 건설, 몬스터, 영역 확장 결과를 이 원칙에 맞춰 분리했습니다. Host의 로컬 플레이어와 Client 플레이어가 같은 요청·응답 계약을 사용하도록 구성하고, AOI와 복구 경로를 통해 두 화면의 결과가 수렴하도록 했습니다.

### 실시간 Polygon Territory

영역은 raster가 아닌 Polygon을 authoritative 상태로 유지합니다. Runner의 predicted trail과 Host의 confirmed trail을 분리하고, 재진입 시 경계 교차와 자기 교차를 검사한 뒤 확장 결과를 복제합니다.

긴 경계에서도 판정 비용을 제한하기 위해 sparse chunk hash, adaptive quadtree, supercover/DDA segment traversal을 조합한 공간 인덱스를 구현했습니다. 확장 계산은 background worker에서 수행하며, revision 검증과 reliable packet 복구를 거친 결과만 메인 스레드의 Polygon·Mesh·index에 반영합니다.

### 실패까지 포함한 건설 트랜잭션

타워 건설은 Spawn, Grid 점유 검증, 특수 타워 초기화, 자원 지불, commit 순서로 처리합니다. 중간 단계가 실패하면 Grid 점유와 NetworkObject를 함께 되돌려 네트워크 상태와 로컬 표시가 어긋나지 않도록 했습니다. 배치 미리보기와 Host의 최종 판정도 동일한 영역·Grid 규칙을 사용합니다.

### 게임 규칙과 Unity 연결부의 분리

Resource 배치, 건설 결과, 라운드 전환, Runner 무기, Monster 후보 선택처럼 테스트 가치가 높은 규칙을 순수 C# Logic·UseCase로 분리했습니다. Unity와 Fusion은 Adapter에서 연결하고, NUnit EditMode 테스트로 경계값과 실패 조건을 확인합니다. 기존 Scene과 직렬화 참조는 한 번에 교체하지 않고 소비자 단위로 옮겨 회귀 범위를 작게 유지했습니다.

## 씬과 시스템 구성

```text
LobbyScene
└─ Photon Fusion Session
   ├─ GameWorld          게임 규칙, NetworkObject, 월드 시스템
   ├─ GamePresentation   카메라, HUD, Fog of War, 로컬 표현
   └─ GameRoot           StageBootstrapper와 초기화 조립
```

Host가 게임 시작과 Scene Authority를 담당하고 `GameWorld`를 기준으로 `GamePresentation`, `GameRoot`를 Additive 로드합니다. `StageBootstrapper`는 월드와 표현 계층의 준비 상태를 확인한 뒤 플레이어, 카메라, UI, Laboratory, 장애물과 Sanctuary를 순서대로 연결합니다.

## 기술 스택

| 영역 | 기술 |
|---|---|
| Engine / Render | Unity 6, Universal Render Pipeline, Shader Graph |
| Multiplayer | Photon Fusion 2, NetworkObject, RPC, AOI, Networked state |
| Gameplay | Unity Input System, AI Navigation, Animation Rigging |
| Presentation | Cinemachine 3, uGUI, GPU 기반 Fog of War mask |
| Verification | Unity Test Framework, NUnit EditMode tests, Profiler markers |
| Local multiplayer | ParrelSync |

## 2인 팀과 AI 협업 방식

이 프로젝트는 **김동민**, **유현우** 두 명이 공동 개발합니다. 기능을 사람별로 완전히 분리하기보다, 최근 작업에서는 다음 영역을 중심으로 구현과 리뷰를 교차했습니다.

| 개발자 | 주요 기여 영역 |
|---|---|
| 김동민 | Territory 구조와 공간 인덱스, 네트워크 복구, 성능 계측, 공용 아키텍처 |
| 유현우 | Runner 무기와 전투, Monster 동작과 투사체, Prefab·Scene 표현 연결 |
| 공동 | Builder·Tower·Resource·Stage 흐름, Host/Client 플레이 검증, 기능 통합과 회귀 확인 |

AI는 코드베이스 탐색, 변경 영향 분석, 테스트 케이스 초안, 문서와 구현의 정합성 점검에 활용합니다. 기능 범위와 게임 규칙, 네트워크 권위 모델, Scene·Prefab 변경, 최종 런타임 판단은 두 개발자가 직접 결정하고 검증합니다.

동시에 같은 공용 파일을 수정하는 문제를 줄이기 위해 다음 흐름을 저장소 안에 남깁니다.

1. [Project Map](Docs/PROJECT_MAP.md)과 기능 문서에서 진입점과 공용 연결부를 확인한다.
2. [Active Work](Docs/Work/Active/)에 예상 변경 파일과 충돌 지점을 먼저 예약한다.
3. 구현 후 집중 테스트, `git diff --check`, Host/Client 수동 검증을 수행한다.
4. 사람이 결과를 확인한 뒤 작업 기록과 실제 변경 파일만 선택해 커밋한다.
5. 완료된 근거는 [Completed Work](Docs/Work/Completed/)에 보존한다.

이 과정은 AI가 만든 결과를 그대로 채택하기 위한 절차가 아니라, 작은 팀에서도 변경 이유와 검증 책임을 추적할 수 있게 하기 위한 개발 방식입니다.

## 프로젝트 실행

1. Unity Hub에서 프로젝트를 Unity `6000.0.69f1`로 연다.
2. Photon Fusion 2 App ID를 `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`에 설정한다.
3. `Assets/01_Scenes/LobbyScene.unity`를 열고 Play Mode를 실행한다.
4. 한 인스턴스는 Host, 다른 인스턴스는 Client로 같은 세션에 참가한다.
5. 로컬 2인 테스트는 프로젝트에 포함된 ParrelSync clone을 사용할 수 있다.

> Photon App ID와 외부 Asset 라이선스는 각 개발 환경에서 별도로 준비해야 합니다.

## 저장소 안내

```text
Assets/01_Scenes/             Lobby와 Additive 게임 Scene
Assets/02_Scripts/Features/   순수 Logic, UseCase, Adapter, Tests
Assets/02_Scripts/            기존 gameplay와 Unity 연결 코드
Assets/03_Prefabs/            Player, Monster, Tower, Resource prefab
Docs/Features/                기능별 책임과 네트워크 계약
Docs/Work/                    진행 중 예약과 완료된 검증 기록
```

세부 구조를 확인하려면 [Project Map](Docs/PROJECT_MAP.md)에서 시작할 수 있습니다. AI-assisted 작업 환경과 협업 규칙은 [빠른 시작 문서](Docs/AI_COLLABORATION_QUICKSTART.md), [AGENTS.md](AGENTS.md), [문서 안내](Docs/README.md)에 정리되어 있습니다.
