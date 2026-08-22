# Chunk Territory Test Matrix

| 영역 | 이번 마일스톤 | 후속 검증 |
|---|---|---|
| fixed 변환과 음수 floor | EditMode 통과 | Unity adapter 테스트 |
| 단일/다중 Chunk 선분 | EditMode 통과 | 실제 Runner 고속 이동 |
| 정확한 모서리 통과 | EditMode 통과 | Host Runner shadow 로그 |
| 같은 Chunk 재방문 | EditMode, receiver 및 Host/Client runtime 통과 | 장기 부하 회귀 |
| bounded packet/codec | 최대 24개, 24/24/2와 round-trip 통과 | Fusion traffic 확인 |
| packet/sample/fragment gap | EditMode 통과 | packet delay runtime |
| Abort stale payload | EditMode, receiver와 자기 교차·Lifeline runtime 통과 | 지연·손실 회귀 |
| Commit 순서 복원 | receiver 재구성과 정상 재진입 runtime 통과 | 장기 부하 회귀 |
| Legacy/shadow point 수·첫 mismatch | EditMode 통과 | Host 진단 로그 |
| 외부 강제 이동 pause/resume | suspension RPC·reconcile 및 SandTomb runtime 통과 | 지연 회귀 |
| Host 로컬 Runner | owner prediction 수동 검증 통과 | 장기 부하 회귀 |
| Client Input Authority Runner | 달리기·확장·걷기와 속도 전환 수동 검증 통과 | 장기 부하 회귀 |
| Chunk delta codec | 최초 0→1, 48-word 상한, Full/Empty run, fragmented Boundary round-trip 독립 validation 통과 | Unity EditMode 회귀 |
| Chunk replica 원자성 | 빈 delta, stale base, gap/malformed terminal과 Reset 소각 통과 | Fusion 지연 runtime |
| Tick 전송 예산 | 30-segment Boundary 4 packet을 2/tick으로 2 tick 종료 | Fusion traffic/Profiler 확인 |
| Late Join·재접속 | 고정 2인 제품 계약에 따라 지원 경로 제거 | 기능 도입 결정 시 새 계약 필요 |
| GPU/CPU fallback | 범위 밖 | presentation milestone |
| Chunk Empty/Full/Boundary | 신규 assertion 5개 및 Unity EditMode 통과 | 장기 부하 회귀 |
| fixed local 경계 연속성 | 다중 Chunk 원본 경로 재구성과 runtime 통과 | 장기 부하 회귀 |
| revision과 동일 상태 delta | 초기 1, 연속 증가, 빈 delta 및 Host runtime 통과 | 복제 milestone 회귀 |
| stale/invalid/overflow 원자성 | 신규 assertion 4개와 실패·Abort runtime 통과 | recovery 회귀 |
| C008 Boundary loop index | sequence/연속성/음수/overflow와 exact area assertion 통과 | Unity EditMode |
| C008 exact expansion plan | 직선·대각선·오목·corner/shared edge 모양 보존 통과 | changed-coverage materialization |
| C008 invalid Trail 원자성 | gap, self-intersection, Boundary overlap, 추가 crossing, Abort 통과 | runtime integration |
| C008 1000×1000 stress | terminal Boundary/Trail scan 0, Chunk-local 후보 검사 통과 | Unity Profiler integration |

Unity 6000.0.69f1 EditMode 25/25 passed다. 신규 packetizer/receiver/codec 7개와
기존 fixed/traversal/session/shadow comparer 회귀를 포함한다. Client 걷기 거리
누적 회귀 수정 후 솔루션 compile 0 errors이며 작업자가 안내된 Host·Client
정상·실패·중단/재개 runtime 절차와 걷기/달리기 전환 재검증 완료를 보고했다.

W-013 신규 테스트 9개는 Unity가 생성한 csproj import 전이라 실제 신규 소스를
포함한 독립 validation에서 NUnit assertion을 직접 실행해 통과했다. 같은 소스로
domain과 `Assembly-CSharp` 통합 compile 0 errors를 확인했다. 작업자가 Unity
import/compile, Territory EditMode 전체와 Host·Client shadow revision runtime 절차
완료를 보고했다.

W-014 신규 packetizer/replica assertion 7개를 포함한 ChunkDomain 독립 validation은
35/35 통과했다. adapter 전송 예산 validation과 신규 source 주입 통합 compile도
통과했지만 실제 Unity import/Fusion Weaver와 Host·Client·Late Join runtime은 작업자
검증 전이므로 완료로 표시하지 않는다.

고정 2인 단순화 slice는 Snapshot/recovery 테스트를 delta-only assertion으로 교체한
뒤 기존 ChunkDomain 회귀와 함께 독립 validation 35/35를 통과했다. 30-segment
Boundary delta는 48-word packet 4개, tick당 2개씩 2 tick에 종료됐고 신규 source
주입 `Assembly-CSharp` compile은 오류 0개였다. 작업자는 안내된 Unity import/compile,
Host·Client 걷기·달리기·속도 전환·긴 경로·확장·Abort와 Profiler runtime 절차 완료를
보고했다.

W-005 신규 assertion 16개와 현재 ChunkDomain 회귀를 같은 source set으로 직접
compile·실행해 56/56 통과했다. 신규 검증은 exact fixed Trail 보존, 큰 후보 선택,
concave/corner/shared Chunk edge, Peer 역할 독립 결정성, sequence/overflow, overlap,
self-intersection, Abort와 1000×1000 world stress를 포함한다. stress terminal의 전체
Boundary/Trail scan metric은 0이다. Unity import/EditMode와 프로젝트 compile은
작업자가 Unity import/compile과 안내된 Territory EditMode 검증 완료를 보고했다.
재생성된 project file은 제거된 W-004 mask 참조를 소각하고 신규 W-005 source/test를
포함했으며 `dotnet build ProjectIO.slnx`는 오류 0개, 기존 warning 25개로 통과했다.
