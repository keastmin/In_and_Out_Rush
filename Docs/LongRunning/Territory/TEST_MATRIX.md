# Chunk Territory Test Matrix

| 영역 | 이번 마일스톤 | 후속 검증 |
|---|---|---|
| fixed 변환과 음수 floor | EditMode 통과 | Unity adapter 테스트 |
| 단일/다중 Chunk 선분 | EditMode 통과 | 실제 Runner 고속 이동 |
| 정확한 모서리 통과 | EditMode 통과 | Host Runner shadow 로그 |
| 같은 Chunk 재방문 | EditMode 및 recorder 통과 | Host/Client fragment stream |
| sample/fragment gap | EditMode 통과 | packet delay/loss 복원 |
| Abort stale payload | EditMode 및 recorder 통과 | 자기 교차·Lifeline Host/Client |
| Commit 순서 복원 | shadow comparer 통과 | 정상 재진입 Host/Client |
| Legacy/shadow point 수·첫 mismatch | EditMode 통과 | Host 진단 로그 |
| 외부 강제 이동 pause/resume | 정적 연결 확인 | SandTomb 실제 플레이 |
| Host 로컬 Runner | 작업자 수동 확인 | confirmed Trail milestone 회귀 |
| Client Input Authority Runner | 작업자 수동 확인 | confirmed Trail milestone 회귀 |
| Late Join | 범위 밖 | snapshot milestone |
| GPU/CPU fallback | 범위 밖 | presentation milestone |

현재 자동 검증은 Unity 6000.0.69f1 EditMode 21/21 passed다. 작업자가 안내된
Host·Client 정상·실패·중단/재개 런타임 절차 완료를 보고했다.
