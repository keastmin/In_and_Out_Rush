# Chunk Territory Roadmap

## Milestone status

1. **Complete** — fixed coordinate, Chunk traversal and ordered Trail session domain
2. **Complete** — State Authority shadow Trail integration and deterministic comparison
3. **Complete** — owner-predicted/confirmed Trail presentation and Fusion fragment
   transport
3.1. **Complete** — exact long Trail의
   sample·fragment·local renderer pool을 fixed block append로 유지하고, 같은 Chunk의
   100,000 point도 최대 256 point fragment와 최대 24 sample packet으로 손실 없이
   분할한다. Client Input Authority 실제 플레이 체감을 확인했으며 영역 확장 계산은
   포함하지 않는다. Host-local 장기 실행과 Profiler 수치는 미검증이다.
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
6.3. **Complete** — confirmed Trail을 증분 수집하고 persistent
   planner/materializer/apply를 단일 background CPU worker와 State Authority shadow에
   연결했다. main thread는 schedule과 frame당 한 result publish만 수행하며
   Host-local/Client Runner runtime과 Profiler 검증을 완료했다.
6.3.2. **Complete** — active gameplay 확장을 C008-C011
   compact shadow에서 분리하고 State Authority의 단일 background polygon worker로
   교체했다. 계산·검증·triangulation·packetization은 worker에서 완료하고 main thread는
   revision 검증 뒤 결과만 적용한다. 48-word packet과 tick당 2 data packet으로 기존
   Proxy에 동일 float bit와 triangle index를 전달한다. 작업자가 Host-local/Client Runner
   양방향 확장과 계산 중 플레이 연속성을 확인했다.
6.3.1. **Paused** — C010 changed Boundary/Full row의 고정 2인 compact delta 복제
6.4. **Paused** — exact Chunk presentation; GPU는 정밀도를 바꾸지 않는 표시 가속이
   실제 profile로 필요한 경우에만 사용
7. **Paused** — one consumer migration per milestone: containment, Grid, Fog, Resource,
   Monster and remaining consumers
8. **Paused** — authoritative cutover, serialized presentation cutover and Legacy removal

현재 제품 우선순위인 3.1 장거리 Trail 안정성과 6.3.2 계산 중 frame 연속성 구현을
완료했다. 추가 영역 확장 기능을 자동으로 시작하지 않는다.

각 task는 위 milestone 하나 또는 한 consumer만 구현한다. 다음 milestone은 현재
handoff에 시작 파일과 rollback을 준비한 뒤 별도 Active reservation으로 시작한다.
