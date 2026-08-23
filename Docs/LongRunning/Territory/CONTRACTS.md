# Chunk Territory Contracts

## C001 Fixed coordinate

Status: Approved

- authoritative Chunk-domain 좌표는 signed 32-bit 정수다.
- 월드 1 unit은 256 fixed unit이다.
- world-to-fixed 변환은 midpoint를 0에서 멀어지는 방향으로 반올림한다.
- 이번 마일스톤의 순수 로직은 Unity `Vector2`를 공개 계약으로 사용하지 않는다.
- fixed 정밀도 이후의 추가 단순화는 허용하지 않는다.

이 계약의 양자화 최대 오차는 축당 1/512 world unit이며 Territory 크기에 따라
커지지 않는다.

## C002 Chunk ownership

Status: Approved

- Chunk 한 변은 8 world unit, 즉 2048 fixed unit이다.
- Chunk `(x, y)`는 각 축의 `[x*2048, (x+1)*2048)` 범위를 소유한다.
- 음수 좌표는 0을 향한 정수 나눗셈이 아니라 mathematical floor로 변환한다.
- 경계점은 half-open 규칙에 따라 양의 방향 Chunk가 소유한다.

## C003 Segment traversal

Status: Approved

- 선분은 시작점에서 끝점으로 이동하는 순서대로 방문 Chunk와 연속 부분 선분을
  생성한다.
- 한 sample 사이에 여러 Chunk를 통과할 수 있다.
- Chunk 모서리를 정확히 통과하면 X/Y를 동시에 전진하고 측면 Chunk를 방문하지
  않는다.
- 분할점은 fixed 격자로 반올림되며 인접 fragment가 같은 분할점을 공유한다.
- 분할은 fixed 계약의 최대 오차 외에 tolerance 증가나 vertex budget 단순화를
  적용하지 않는다.

## C004 Trail session ordering and lifetime

Status: Approved

- `SessionId`는 0보다 큰 단조 증가 `ulong`이다.
- sample과 fragment sequence는 session마다 0에서 시작하는 연속 `uint`다.
- 같은 Chunk의 연속 구간만 한 fragment로 합치며, 나갔다 돌아오면 새 fragment다.
- 수신자는 sequence gap, 다른 SessionId와 종료된 session의 payload를 거부한다.
- Abort는 보관 sample, fragment와 열린 fragment를 즉시 소각한다.
- Commit은 열린 fragment를 확정하고 결과를 읽기 전용으로 유지한다.

## C005 Fusion Trail presentation transport

Status: Approved

- Input Authority owner prediction은 local presentation 전용이며 Territory 판정,
  자기 교차, Kill, Lifeline과 확장 결과를 변경하지 않는다.
- State Authority가 생성한 session lifecycle과 confirmed sample packet만 확정
  stream이다.
- Start, confirmed packet, pause/resume, Commit과 Abort는 Reliable이며 latest live
  head는 Unreliable latest-wins다.
- confirmed packet은 `SessionId + packet sequence + first sample sequence`를 가지며
  packet당 sample 수는 최대 24개다.
- wire sample은 `simulation tick + fixed X + fixed Y` 정수 3개로 전송한다.
- 수신자는 packet/sample gap, 중복, 다른 SessionId, stale terminal과 종료 뒤
  payload를 거부하며 accepted sample만 C003으로 fragment화한다.
- fixed 양자화 뒤 모양 단순화는 없다. renderer page 제한은 같은 끝점을 공유하는
  표시 segment만 나누며 path 좌표를 제거하거나 이동하지 않는다.
- Abort는 outbound pending, inbound confirmed, prediction과 live head를 소각한다.
- 진행 중 Trail Late Join 복원은 C005에 포함하지 않는다.

## C006 Chunk Territory state and atomic commit

Status: Approved

- Chunk Territory snapshot은 전역 polygon을 보관하지 않는 sparse map이다.
- map에 없는 Chunk는 `Empty`다. `Full`은 Chunk 전체가 내부이며 경계 payload가
  없다. `Boundary`는 polygon 진행 방향을 보존하는 fixed Chunk-local 선분과
  결정적인 내부 기준을 가진다.
- local 좌표는 Chunk minimum을 0으로 한 closed `[0, 2048]` fixed 범위다.
  인접 Chunk 경계 선분은 같은 global fixed 끝점을 공유한다.
- fixed 양자화 뒤 tolerance 증가, vertex budget 단순화, 점 이동이나 제거를
  적용하지 않는다. 연속 중복점과 정확히 같은 직선 위 중간점 제거만 모양을
  바꾸지 않는 정규화로 허용한다.
- snapshot은 외부에서 변경할 수 없다. revision은 0이 아닌 단조 증가 `ulong`이고
  최초 성공 snapshot은 1이다.
- Commit은 expected base revision이 현재 revision과 같을 때만 새 snapshot을
  공개한다. stale base, invalid/self-intersecting/degenerate polygon, overflow와
  빌드 실패는 기존 snapshot을 변경하지 않는다.
- changed-Chunk는 생성·변경 coverage와 `Empty` tombstone을 중복 없이 Chunk
  `(Y, X)` 오름차순으로 제공한다. 동일 coverage의 재Commit은 revision은
  증가하지만 delta는 비어 있다.
- 이번 마일스톤에서는 State Authority shadow store만 Commit한다. Legacy polygon,
  vertex RPC와 consumer callback이 계속 게임 결과를 소유한다.
- changed-Chunk 복제, recovery와 Late Join snapshot은 C006에 포함하지 않는다.

## C007 Fixed two-peer Chunk Territory delta replication

Status: Approved

- State Authority의 C006 immutable sparse store가 권위 원본이다. RPC는 bounded
  transfer이고 Proxy replica는 shadow 상태다. 두 Peer는 스테이지 시작부터 존재하며
  한 Peer 이탈 시 스테이지가 종료된다.
- 최초 changed-Chunk delta는 빈 replica의 `0 -> 1`, 이후 delta는
  `BaseRevision -> BaseRevision + 1` transaction으로 적용한다. transaction, packet과
  record 순서가 연속이고 terminal 검증이 끝난 뒤에만 새 replica를 원자적으로
  공개한다.
- 한 data packet은 최대 48개의 signed 32-bit word를 보유한다. `Full`과 `Empty`
  tombstone은 같은 Y와 fill의 연속 X를 run으로 인코딩할 수 있다. `Boundary`는
  Chunk 좌표, center-inside, 전체 segment 수와 각 fixed local 방향성 segment의
  sequence/start/end를 그대로 전달한다.
- packetizer는 coverage를 `(Y, X)` 순서로 정규화하고 packet sequence를 0부터
  연속으로 만든다. 한 Boundary가 packet 경계를 넘어도 segment를 제거·이동하거나
  tolerance를 늘리지 않는다.
- 수신자는 base/revision, packet count, packet sequence, record 구조, run 범위,
  중복 Chunk, Boundary segment count/sequence/local 범위를 검증한다. gap, stale base,
  malformed payload, 중복 terminal과 중단된 transaction은 이전 replica를 보존한다.
- State Authority는 simulation tick마다 최대 2 data packet만 발송한다. begin/end
  제어 RPC는 data packet 예산과 별개지만 transaction 순서를 앞지르지 않는다.
- Proxy는 NetworkObject가 RPC를 받을 수 있는 시점부터 delta를 수용한다. Additive
  `SetUp`이 늦어도 먼저 받은 inbound transaction/replica를 초기화하지 않고,
  Despawn/TearDown은 outbound queue, inbound candidate와 replica를 소각한다.
- Late Join, reconnect, AOI 재진입 recovery, snapshot transfer kind, retry timer와
  targeted `PlayerRef` 전송은 지원하지 않는다. C007은 Legacy polygon, 확장 판정,
  vertex RPC, mesh나 consumer event의 권위를 전환하지 않는다.

## C008 Exact incremental expansion plan

Status: Approved

- C006 snapshot의 방향성 Boundary segment는 revision마다 한 번 전역 fixed loop로
  검증·색인한다. sequence는 `0..N-1`로 연속이고 인접 endpoint와 마지막-첫 endpoint가
  정확히 이어져야 하며, gap, duplicate, 열린/복수 loop와 overflow는 index를
  공개하지 않는다.
- index는 Chunk별 Boundary 후보와 loop prefix 면적을 보관한다. ordered Trail
  fragment append와 terminal은 전체 Boundary를 다시 순회하지 않는다.
- C003/C004 fragment는 Runner 이동 중 순서대로 한 번만 처리한다. session은 정확한
  진출·재진입 접점, 외부 Trail, Trail 면적, 자기 교차와 작업량을 증분 누적한다.
  fragment/session sequence gap, 경계 overlap, 자기 교차와 추가 Boundary crossing은
  plan을 공개하지 않는다.
- fixed 정밀도 뒤에는 tolerance 증가, vertex budget, 곡선 단순화와 점 이동·삭제를
  적용하지 않는다. 연속 중복점과 정확히 같은 직선 위 중간점만 모양 보존
  정규화로 제거할 수 있다.
- terminal은 보관 Trail을 다시 순회하거나 복사하지 않고 prefix 면적으로 두 기존
  Boundary arc 후보를 평가한다. 기존 영역보다 실제 면적이 커지는 후보 중 현재
  게임 규칙과 같은 큰 후보를 결정적으로 선택한다.
- immutable plan은 source snapshot revision, session id, 진출·재진입 접점과
  Boundary sequence, 선택 arc 방향, exact fixed Trail과 결정적 work metrics를
  제공한다. plan 생성 자체는 snapshot, mesh, renderer, RPC와 consumer 상태를
  변경하지 않는다.
- 같은 snapshot과 ordered fragment는 Host·Client 역할이나 호출 Peer와 무관하게
  같은 fixed plan 또는 같은 실패 결과를 만든다. 권위 적용과 네트워크 공개는 별도
  계약이다.

## C009 Compact changed-region materialization

Status: Approved

- 입력은 같은 source revision의 C006 snapshot, C008 Boundary index와 expansion
  plan이다. revision, session, 접점 또는 Boundary sequence가 맞지 않으면 candidate를
  시작하지 않는다.
- 추가 영역은 exact Trail과 선택된 새 경계에서 제외되어 실제로 교체되는 기존
  Boundary arc만으로 닫는다. 유지되는 기존 Boundary 전체와 source의 Full Chunk는
  복사·순회·재번호화하지 않는다.
- 추가 영역 Boundary가 통과하는 Chunk에는 captured-region의 fixed local segment,
  새 Trail segment와 제거 대상 source sequence를 보존한다. fixed 입력을 이동하거나
  삭제하지 않으며 연속 중복점과 exact collinear 중간점 외 단순화는 없다.
- Boundary가 통과하지 않는 추가 영역 내부 Chunk는 `(Y, MinX..MaxX)` inclusive Full
  run으로 정렬·병합한다. 내부 면적만큼 개별 Full coverage 객체를 생성하지 않는다.
- materialization session의 `TryStep(maxWorkUnits)`는 양수 budget 이하의 결정적
  work만 수행한다. 완료 전 결과를 공개하지 않고 Abort, overflow, malformed arc와
  중간 실패는 candidate 전체를 소각하며 source snapshot은 바꾸지 않는다.
- 동일 입력은 step budget을 나누는 방식과 Host·Client 역할에 무관하게 같은 immutable
  Boundary edit, Full run과 algorithmic metrics를 만든다.
- C009는 순수 정수 CPU 도메인 계약이다. GPU/CPU fallback을 두지 않는다. Job
  System/Burst는 이 계약의 배열과 cursor를 바꾸지 않고 실행할 후속 adapter이며,
  authoritative store 적용·Fusion 복제·표시는 별도 계약이다.

## C010 Persistent compact Territory state and atomic splice apply

Status: Approved

- C006 sparse snapshot은 persistent compact state로 최초 한 번 변환할 수 있다. 이
  변환만 source Chunk와 Boundary sequence를 전부 읽으며 정상 C010 apply metrics와
  분리한다.
- compact Boundary는 counter-clockwise canonical loop, exact fixed local endpoint와
  `ulong` stable segment identity를 가진다. 저장 순서는 전역 연속 sequence가 아니며
  유지되는 segment identity를 확장마다 재번호화하지 않는다.
- Boundary 순서·면적 prefix와 stable identity lookup은 persistent balanced tree다.
  Chunk-local candidate index도 persistent map이며 다음 C008 session은 compact
  snapshot에서 전체 loop 재구축 없이 직접 시작한다.
- 내부 Full state는 Y별 정렬·비중첩·최대 병합 inclusive run이다. C009 Full run은
  해당 Y row에 union하며 면적만큼 Full coverage나 Chunk 객체로 전개하지 않는다.
- C009 stable splice는 제거되는 contiguous forward arc identity, 접점에서 보존되는
  exact source residual과 exact Trail part를 한 transaction으로 검증한다. 변경되지
  않은 Boundary와 Full row branch는 이전 immutable snapshot과 구조적으로 공유한다.
- apply `TryStep(maxWorkUnits)`는 양수 budget 이하의 결정적 단계만 수행하고 terminal
  이전에는 candidate를 공개하지 않는다. Store는 expected source object/revision이
  현재 상태와 같은 완료 candidate만 다음 revision으로 원자 공개한다.
- stale revision/identity, duplicate·non-contiguous splice, endpoint 불연속, area
  mismatch, overflow, Abort와 중간 실패는 candidate를 소각하고 source snapshot을
  보존한다.
- 정상 apply의 source-wide Boundary scan, source-wide Full scan, global Boundary
  renumber, Full Chunk 전개와 unchanged-node copy metric은 0이다. 질의는 expansion
  parent delta chain을 따라가지 않고 현재 balanced root에서 직접 수행한다.
- C010은 순수 정수 CPU 저장 계약이며 GPU/CPU fallback을 두지 않는다. Job/Burst
  scheduling, State Authority runtime 연결, compact delta 복제와 exact presentation은
  후속 계약이다.

## C011 Ordered background compact expansion shadow

Status: Approved

- State Authority는 C006 최초 snapshot을 C010 compact state로 한 번만 변환한다.
  이후 정상 확장은 C006 개별 Full Chunk나 전역 sequence로 되돌아가 재구축하지 않는다.
- confirmed `TerritoryTrailFragment`는 Runner 이동 중 새로 닫힌 fragment만 증분
  수집한다. terminal에서 전체 긴 Trail을 다시 복사하지 않고 수집 list의 소유권을
  immutable work item으로 넘긴다.
- 단일 background CPU worker는 work item을 expected source revision 순서대로 처리한다.
  각 item은 같은 pure C008 planner, C009 materializer와 C010 apply를 사용하며 다음
  item은 직전 성공 candidate를 source로 사용한다.
- worker는 Unity API, Fusion API, Scene object, Legacy `Territory`와 renderer에 접근하지
  않는다. 완료 result는 thread-safe queue를 거쳐 State Authority main thread가 한
  frame에 최대 한 건씩 poll하고 같은 source object/revision일 때만 Store에 publish한다.
- failure, cancellation, stale publish와 teardown은 pending request/result를 소각하고
  마지막 published compact snapshot과 Legacy gameplay를 보존한다.
- C011 exact geometry에는 CPU/GPU fallback이나 별도 Burst geometry를 두지 않는다.
  현재 `decimal` 및 persistent managed tree를 Native 알고리즘으로 복제하지 않으며,
  profile로 독립 배열 병목이 확인될 때만 후속 계약에서 Burst 후보를 분리한다.
- 이번 계약은 runtime shadow와 profile까지만 소유한다. Legacy polygon, C006/C007
  replication, vertex RPC, mesh와 consumer가 계속 gameplay 및 Peer 표시를 소유한다.

## C012 Bounded exact long Trail runtime

Status: Approved

- fixed 변환 뒤 accepted sample은 단순화, 병합, 이동 또는 삭제하지 않는다.
- 장기 sample·fragment와 local owner presentation index는 fixed-size block에 append한다.
  새 block 할당은 이전 point payload를 복사하지 않으며 이미 닫힌 block은 append 중
  다시 작성하지 않는다.
- 같은 Chunk의 연속 fragment도 point 수가 256에 도달하면 분할한다. 인접 fragment는
  마지막/첫 fixed point 하나를 정확히 공유하고 재조립 path가 원본 sample path와 같다.
- `TerritoryTrailFragment`는 2..256 point만 허용한다. Chunk closed bounds, 연속 중복,
  session/fragment/sample sequence 검증은 C003/C004와 동일하다.
- confirmed packet은 C005의 최대 24 sample을 유지한다. packet 추출은 이미 전송한
  sample 뒤의 전체 pending payload를 이동·복사하지 않는다.
- local owner는 Input Authority에서 즉시 append하고 State Authority는 별도의 exact
  confirmed session을 authoritative 계산 입력으로 소유한다. Host-local 역할 중첩으로
  같은 renderer 또는 권위 session에 중복 append하지 않는다.
- LineRenderer 하나의 최대 256 point와 Chunk 경계 분할을 유지한다. 이번 계약은
  renderer virtualization, GPU 표시와 영역 확장 계산을 추가하지 않는다.
- 정확한 전체 이력을 유지하므로 총 메모리는 point 수에 선형 비례한다. 계약의 목표는
  유한 메모리에서 무한 이력을 약속하는 것이 아니라 append·packetize·표시 hot path에서
  누적 전체 이력 재할당과 재구축을 제거하는 것이다.

## C013 Simple background authoritative polygon expansion

Status: Approved

- State Authority는 정상 재진입 terminal에서 현재 완료 Territory vertex와 authoritative
  confirmed Trail을 독립 배열로 snapshot하고 한 번에 하나의 background CPU 작업만
  실행한다.
- worker는 worker-local `Territory`에서 polygon 확장, finite/simple polygon 검증,
  triangulation과 결과 packetization까지 완료한다. Unity Mesh, Scene object, Fusion RPC와
  gameplay event에는 접근하지 않는다.
- prototype vertex count 상한, RDP 또는 tolerance 기반 단순화를 사용하지 않는다.
  부동소수점 연산의 기존 `0.0001` 수치 안정성 범위에서 같은 점·공선 중간점을 정리하는
  것 외에 Trail 모양을 성능 목적으로 변경하지 않는다.
- 계산 중에는 마지막 완료 Territory가 authoritative query와 presentation을 계속 소유하며
  simulation, 입력, 이동과 Render를 차단하지 않는다. 동시에 두 확장 mutation을 계산하지
  않고 다음 획득 Trail은 완료 적용 뒤 현재 위치부터 시작한다.
- main thread는 source revision이 현재 revision과 같은 성공 결과만 한 번 Mesh,
  `Territory.Vertices`와 consumer event에 적용한다. 실패·취소·stale 결과는 이전 Territory를
  보존하고 worker를 영구 fault시키지 않는다.
- Host-local Runner와 Client Runner는 같은 State Authority schedule 계약을 사용한다.
  완료 vertex는 float bit 그대로, triangle index는 worker 결과 그대로 최대 48-word Reliable
  packet과 simulation tick당 최대 2 data packet으로 Proxy에 보낸다.
- Proxy는 begin/data/terminal, revision, 전체 크기, finite vertex와 triangle index를
  검증하고 terminal에서만 같은 결과를 적용한다. Proxy에서 polygon 계산이나 재삼각분할을
  하지 않는다.
- C008-C011 compact 경로는 C013 active gameplay 확장을 계산·적용하지 않는다. GPU,
  Burst, Job System, CPU/GPU fallback, Late Join과 reconnect는 이 계약 범위 밖이다.

## Future contracts not approved here

- compact changed-state delta replication
- exact Chunk presentation and consumer cutover
