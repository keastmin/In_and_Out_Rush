# Chunk Territory Test Matrix

| 영역 | 이번 마일스톤 | 후속 검증 |
|---|---|---|
| fixed 변환과 음수 floor | EditMode 통과 | Unity adapter 테스트 |
| 단일/다중 Chunk 선분 | EditMode 통과 | State Authority shadow 비교 |
| 정확한 모서리 통과 | EditMode 통과 | 실제 Runner 고속 이동 |
| 같은 Chunk 재방문 | EditMode 통과 | Host/Client fragment stream |
| sample/fragment gap | EditMode 통과 | packet delay/loss 복원 |
| Abort stale payload | EditMode 통과 | 자기 교차·Lifeline Host/Client |
| Commit 순서 복원 | EditMode 통과 | Territory expansion commit |
| Host 로컬 Runner | runtime diff 없음 확인 | 실제 Peer 테스트 |
| Client Input Authority Runner | runtime diff 없음 확인 | 실제 Peer 테스트 |
| Late Join | 범위 밖 | snapshot milestone |
| GPU/CPU fallback | 범위 밖 | presentation milestone |

이번 마일스톤 자동 검증은 Unity 6000.0.69f1 EditMode 15/15 passed다.
