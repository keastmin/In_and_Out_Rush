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

## Future contracts not approved here

- Territory revision and changed-Chunk replication
- GPU buffer and mask format
- Late Join snapshot framing
