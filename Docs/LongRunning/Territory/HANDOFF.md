# Handoff

## Completed outcome

현재 제품 계약인 고정 2인 세션에 맞춰 Chunk Territory shadow 복제를 delta-only로
단순화했다. State Authority의 C006 store가 최초 `0 -> 1`과 이후 연속
changed-Chunk delta를 기존 Client Proxy에 Reliable RPC로 보내고, packet당 최대
48 word와 TerritorySystem당 simulation tick 최대 2 data packet 예산을 유지한다.

Snapshot transfer kind와 전체 snapshot packetizer, `[Networked]` recovery revision,
mismatch/retry timer, 요청자 중복 억제, `RpcInfo.Source` 검증과 targeted PlayerRef RPC는
제거했다. Proxy는 transaction 전체를 검증한 뒤에만 replica를 원자적으로 공개한다.
Additive `SetUp`보다 먼저 RPC가 도착해도 Proxy replica를 다시 초기화하지 않으며,
TearDown/Dispose에서는 outbound, inbound와 published replica를 소각한다.

Legacy polygon, mesh, vertex RPC, consumer callback과 W-012 Trail prediction/confirmed
transport는 변경하지 않았다. Chunk replica는 계속 shadow 상태이며 게임 결과를
변경하지 않는다.

## Changed files

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryChunkReplicationStream.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferPacket.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferPacketizer.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkReplica.cs`
- 위 packetizer/replica 테스트 2개
- `TerritoryChunkTransferKind.cs`와 `.meta` 삭제
- Territory 기능 문서, C007, milestone/handoff/roadmap/test 문서와 Active 작업 문서

## Decisions used

- `CONTRACTS.md` C001-C006은 그대로 유지하고 C007만 고정 2인 delta-only로 축소
- 두 Peer는 스테이지 시작부터 존재하고 한 Peer 이탈 시 스테이지 종료
- Late Join, reconnect, AOI recovery와 다수 요청자 보안 경계는 지원하지 않음
- State Authority C006 store 원본 + 기존 Client Proxy shadow replica
- 최대 48-word Reliable data packet, simulation tick당 최대 2개
- stale base, packet sequence와 malformed payload 검증은 current-session 손상 방지를
  위한 최소 안정성으로 유지
- Legacy gameplay authority와 owner-predicted/confirmed Trail 경로 유지

## Verification evidence

- 신규 delta-only assertion과 기존 ChunkDomain 회귀를 포함한 독립 validation 35/35
  통과
- 최초 `0 -> 1`, 연속 delta, 빈 delta revision 전진, Full/Empty run과 packet 경계
  Boundary round-trip 통과
- stale base, packet gap, malformed terminal은 이전 replica를 보존하고 Reset은 inbound와
  published replica를 소각함을 확인
- 30-segment Boundary delta 4 packet, tick당 최대 2 data packet, 총 2 tick 종료
- 신규 source 직접 compile 및 임시 source 주입 `Assembly-CSharp` 통합 compile 오류
  0개, 기존 프로젝트 warning 16개
- Snapshot/recovery/targeted 경로의 source 참조가 남지 않았고 script와 `.meta` 삭제가
  pairing됨
- 작업자가 안내된 Unity import/compile, Host·Client 걷기·달리기·속도 전환, 긴 외부
  경로·정상 확장, 자기 교차/Abort와 Profiler runtime 절차 완료를 보고함

## Serialized or manual setup

Scene·Prefab·Inspector 연결 변경은 없다. Unity가 삭제·변경 script를 import한 뒤 아래
순서로 고정 2인 Host·Client runtime을 확인한다.

1. Console compile error가 없고 EditMode `ProjectIO.Territory.Tests` 전체가 통과하는지
   확인한다.
2. Host와 Client를 스테이지 시작부터 함께 실행한다. Client Console에 initial
   `Chunk Territory replica applied. Revision: 1`이 한 번 나타나고 transfer reject
   경고가 없는지 확인한다.
3. Host Runner로 영역 밖을 걷기, 달리기, 걷기↔달리기 전환 순서로 이동한다. owner
   선이 러너보다 구조적으로 앞서거나 뒤처지지 않고 끊기지 않는지 확인한다.
4. Client Runner에서도 같은 순서를 반복한다. 특히 낮은 속도 걷기에서 선이
   점선처럼 끊기지 않고 Host Runner와 같은 체감인지 확인한다.
5. Host Runner와 Client Runner 각각 영역 확장 전에 긴 외부 경로를 만든 뒤 정상
   재진입한다. 양쪽 화면의 Trail, 최종 Legacy 영역 모양, mesh와 consumer 결과가
   같고 Chunk revision이 성공당 정확히 1 증가하는지 확인한다.
6. 양쪽 Runner에서 자기 교차 또는 Lifeline/Abort를 실행한다. 선과 pending 상태가
   즉시 정리되고 Chunk revision/delta가 발생하지 않으며 다음 정상 확장이 가능한지
   확인한다.
7. Unity Profiler에서 Host/Client 각각 Trail 이동 frame과 정상 확장 frame을
   기록한다. `TerritorySystem.ChunkDeltaPacketize`와
   `TerritorySystem.ChunkTransferFlush` marker의 시간과 spike 여부를 기록하고 Fusion
   traffic에서 한 tick의 Chunk data RPC가 2개를 넘지 않는지 확인한다.
8. Stage 종료 또는 한 Peer 이탈 시 반복 transfer warning이나 이전 session replica
   흔적 없이 기존 게임 흐름대로 종료되는지 확인한다.

## Known risks and failures

- Reliable delta가 current-session에서 유실되거나 initial delta 전에 Proxy가 존재하지
  않는 spawn ordering이면 recovery 없이 shadow replica가 뒤처질 수 있다. 이번 고정
  2인 runtime에서는 작업자가 안내 절차 완료를 보고했지만, spawn 흐름 변경 시 initial
  revision 1 수신을 회귀 검증해야 한다.
- Legacy polygon union, 256 vertex 보정과 synchronous C006 shadow build는 이번
  단순화 범위 밖이다. 정상 확장 hitch의 주원인이면 다음 CPU/GPU milestone에서
  별도로 처리한다.

## Remaining legacy consumers

모든 Territory containment, authoritative expansion, mesh, Grid, Fog, Resource,
Monster와 vertex replication. Chunk replica는 아직 표시나 게임 판정에 사용되지 않는
shadow 상태다.

## Next bounded milestone

위 runtime/Profiler 결과를 먼저 완료한다. Host·Client 체감과 current-session delta가
통과하면 별도 Active 예약에서 GPU Chunk mask/SDF presentation과 CPU fallback을
구현한다. Trail이나 expansion frame spike가 관측되면 marker 근거로 해당 CPU 작업만
먼저 분리한다. Consumer 권위 전환과 Legacy polygon 제거는 그 뒤 consumer별
milestone으로 유지한다.

## Exact starting files

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryChunkReplicationStream.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailReplicationStream.cs`
- `Assets/02_Scripts/Territory Refactor/Trail/TerritoryTrailChunkRenderer.cs`
- `Assets/02_Scripts/Territory/TerritoryVisible.cs`
- `Docs/LongRunning/Territory/CONTRACTS.md`의 C005/C007
