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

## Future contracts not approved here

- Territory revision and changed-Chunk replication
- Chunk interior/boundary storage and fill
- GPU buffer and mask format
- Late Join snapshot framing
