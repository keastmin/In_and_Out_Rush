# Chunk Territory Roadmap

## Milestone status

1. **Complete** — fixed coordinate, Chunk traversal and ordered Trail session domain
2. **Complete** — State Authority shadow Trail integration and deterministic comparison
3. **Complete** — owner-predicted/confirmed Trail presentation and Fusion fragment
   transport
4. **Complete** — revisioned Empty/Full/Boundary Chunk Territory state and expansion commit
5. **Complete, runtime unverified** — changed-Chunk replication, recovery and Late Join
   snapshot. 현재 제품에는 Late Join이 없어 다음 slice에서 관련 복잡성을 축소한다.
5.1. **Complete** — fixed two-peer delta-only replication simplification;
   reconnect/Late Join/security expansion 제거와 Host·Client Trail/expansion runtime
   검증 완료
6. **Planned** — GPU Chunk mask/SDF presentation with CPU fallback
7. **Planned** — one consumer migration per milestone: containment, Grid, Fog, Resource,
   Monster and remaining consumers
8. **Planned** — authoritative cutover, serialized presentation cutover and Legacy removal

각 task는 위 milestone 하나 또는 한 consumer만 구현한다. 다음 milestone은 현재
handoff에 시작 파일과 rollback을 준비한 뒤 별도 Active reservation으로 시작한다.
