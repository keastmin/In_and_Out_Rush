# Handoff

## Completed outcome

State Authority의 C006 sparse Chunk Territory shadow snapshot을 Proxy에 복제하는
마일스톤 5 코드를 구현했다. 정상 Commit은 ordered changed-Chunk delta이며,
`[Networked]` authoritative revision과 Proxy replica가 수렴하지 않으면 요청
`PlayerRef`에만 전체 immutable snapshot을 전송한다.

Full/Empty 행 run과 Boundary fixed local segment는 최대 48-word Reliable packet으로
분할되고 한 TerritorySystem은 simulation tick마다 data packet을 최대 2개만 보낸다.
수신자는 transaction 전체를 검증한 뒤에만 replica를 원자적으로 교체한다. Legacy
polygon, mesh, vertex RPC, consumer callback과 W-012 Trail은 계속 유일한 게임
경로다.

자동 검증은 통과했다. 실제 Unity import/Fusion Weaver와 Host·Client·Late Join
runtime은 작업자가 수행하지 않은 상태로 Commit하며, 현재 제품에 Late Join이 없다는
결정에 따라 다음 slice에서 관련 복잡성을 축소한다.

## Changed files

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferKind.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferPacket.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferPacketizer.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkReplica.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryChunkReplicationStream.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkTransferPacketizerTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkReplicaTests.cs`
- 위 신규 Unity script의 `.meta` 7개
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/`의 milestone/contract/handoff/roadmap/test 문서
- `Docs/Work/Active/W-20260822-002-chunk-territory-replication-recovery.md`

## Decisions used

- `CONTRACTS.md` C001-C007
- State Authority snapshot 원본 + Networked revision 수렴 신호
- delta는 `BaseRevision -> Revision`, recovery snapshot은 `0 -> Revision`
- data packet 최대 48 signed int word, simulation tick당 최대 2 packet
- Full/Empty same-row run, Boundary 좌표·방향성 segment 무손실 전송
- Peer별 outstanding recovery 하나, `RpcInfo.Source` 기반 targeted response
- Proxy replica는 shadow only; Legacy gameplay authority 유지

## Verification evidence

- 신규 packetizer/replica assertion 7개와 기존 ChunkDomain 회귀를 포함한 독립
  validation 35/35 통과
- 빈 delta revision 전진, Full/Empty run, packet 경계 Boundary round-trip 통과
- stale base, packet gap, malformed terminal, snapshot revision 역행이 이전 replica를
  보존함을 확인
- adapter validation에서 30-segment Boundary delta 4 packet, tick당 최대 2 packet,
  총 2 tick 종료와 recovery request 지연/중복 억제 확인
- 신규 source 직접 ChunkDomain/adapter compile 0 errors
- 생성 csproj를 변경하지 않고 신규 source를 임시 target으로 주입한
  `Assembly-CSharp` 통합 compile 0 errors, 기존 warning 13개
- 1000×1000 사각형: 15,876 Chunk, 118 packet, 5,641 word, packetize 7.384ms
- 256점·반경 500 원형: 12,532 Chunk, 144 packet, 6,881 word, packetize 3.282ms
- 위 시간과 packet 수는 로컬 .NET validation이며 Unity Profiler/Fusion traffic
  측정이 아님

## Serialized or manual setup

Scene·Prefab·Inspector 연결 변경은 없다. Unity가 신규 script/meta를 import한 뒤
다음 절차를 수행한다.

1. Console compile error가 없고 EditMode `ProjectIO.Territory.Tests` 전체가
   통과하는지 확인한다.
2. Host와 기존 Client를 시작한다. State Authority가 initial revision 1을 한 번
   Commit하고 Client에 `Chunk Territory replica applied. Revision: 1` 로그가 한 번
   나타나는지 확인한다.
3. Host Runner가 정상 확장한다. revision이 정확히 1 증가하고 Client replica가 같은
   revision으로 수렴하며 양쪽 Legacy 영역 모양·Trail·consumer 결과가 기존과 같은지
   확인한다.
4. Client Input Authority Runner도 정상 확장한다. State Authority Commit과 delta가
   한 번만 발생하고 Host/Client 플레이 결과와 replica revision이 같은지 확인한다.
5. 최소 revision 2 이후 세 번째 Peer를 Late Join시킨다. 약간의 전송 시간 뒤 Late
   Join Peer에 현재 revision의 `replica applied` 로그가 나타나고 begin/terminal reject
   경고가 반복되지 않는지 확인한다.
6. 가능하면 Late Join snapshot 전송 중 한 번 더 정상 확장한다. Late Join Peer가
   partial/stale 상태를 공개하지 않고 최종 최신 revision까지 수렴하는지 확인한다.
7. 자기 교차, Lifeline/Abort와 확장 거부에는 Chunk revision/delta가 생기지 않으며
   다음 정상 확장만 정확히 1 증가하는지 확인한다.
8. Client disconnect/reconnect 또는 Scene 종료 시 stale transfer 경고가 반복되지
   않고 재참가 Peer가 현재 snapshot으로 다시 수렴하는지 확인한다.
9. Unity Profiler와 Fusion traffic에서 `TerritorySystem.FixedUpdateNetwork`의 data RPC가
   한 tick에 2개를 넘지 않는지, 대형 snapshot의 Host packetize hitch와 총 수렴 시간을
   기록한다.

## Known risks and failures

- 실제 Unity import/Fusion Weaver와 Host·Client·Late Join runtime은 아직 미검증이다.
- snapshot/delta packetize는 동기식이다. synthetic snapshot에서 3.282~7.384ms가
  측정되어 실제 Host frame에서는 Profiler 결과에 따라 incremental packetization이나
  cache가 필요할 수 있다.
- 60Hz 가정에서 synthetic 전체 snapshot data는 약 0.98~1.2초에 걸쳐 발송된다.
  RPC framing, 복수 Territory 합산 bandwidth와 AOI 비용은 측정하지 않았다.
- Legacy 256 vertex 보정, 단일 polygon union과 synchronous shadow rebuild hitch는
  아직 제거되지 않았다.

## Remaining legacy consumers

모든 Territory containment, authoritative expansion, mesh, Grid, Fog, Resource,
Monster와 vertex replication. Chunk replica는 아직 표시나 게임 판정에 사용되지 않는
shadow 상태다.

## Next bounded milestone

별도 Active reservation에서 고정 2인 세션에 필요 없는 Late Join·재접속 recovery,
반복 retry와 다수 요청자 방어를 제거한다. 두 Peer가 시작부터 존재한다는 조건 아래
최소 readiness와 bounded Reliable delta만 유지하고, Host Runner와 Client Input
Authority Runner의 사선 표시·걷기/달리기·정상 확장 frame time과 체감 동등성을
우선 검증한다. GPU presentation과 consumer 권위 전환은 이 단순화 뒤 별도
milestone으로 유지한다.

## Exact starting files

- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryChunkReplicationStream.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkReplica.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkSnapshot.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailReplicationStream.cs`
- `Assets/02_Scripts/Territory/TerritoryVisible.cs`
- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Trail/TerritoryTrailChunkRenderer.cs`
- `Docs/LongRunning/Territory/CONTRACTS.md`의 C005/C007
