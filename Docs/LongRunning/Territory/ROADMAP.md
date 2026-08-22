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
5.2. **Superseded** — 32×32 GPU mask/SDF presentation experiment; fixed 경계 손실과
   invalid kernel 경로 때문에 제거
6. **Complete** — exact fixed Boundary index and incremental Trail expansion plan
6.1. **Complete** — exact changed-region Boundary edit와
   compressed Full row run을 만드는 bounded pure-domain materialization
6.2. **Complete** — compact materialization을
   직접 보존하는 persistent store apply. C006 개별 Full/전역 sequence로 재전개하지
   않고 100회 연속 확장 stress와 Unity 검증을 통과했다.
6.3. **Planned** — persistent planner/materializer/apply를 frame budget과 Job/Burst
   worker에 연결하고 State Authority shadow에서 profile한다.
6.4. **Planned** — exact Chunk presentation; GPU는 정밀도를 바꾸지 않는 표시 가속이
   실제 profile로 필요한 경우에만 사용
7. **Planned** — one consumer migration per milestone: containment, Grid, Fog, Resource,
   Monster and remaining consumers
8. **Planned** — authoritative cutover, serialized presentation cutover and Legacy removal

각 task는 위 milestone 하나 또는 한 consumer만 구현한다. 다음 milestone은 현재
handoff에 시작 파일과 rollback을 준비한 뒤 별도 Active reservation으로 시작한다.
