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
| Late Join | 범위 밖 | snapshot milestone |
| GPU/CPU fallback | 범위 밖 | presentation milestone |

Unity 6000.0.69f1 EditMode 25/25 passed다. 신규 packetizer/receiver/codec 7개와
기존 fixed/traversal/session/shadow comparer 회귀를 포함한다. Client 걷기 거리
누적 회귀 수정 후 솔루션 compile 0 errors이며 작업자가 안내된 Host·Client
정상·실패·중단/재개 runtime 절차와 걷기/달리기 전환 재검증 완료를 보고했다.
