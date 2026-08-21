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

## Future contracts not approved here

- Fusion live-head/confirmed-fragment transport
- Territory revision and changed-Chunk replication
- Chunk interior/boundary storage and fill
- GPU buffer and mask format
- Late Join snapshot framing
