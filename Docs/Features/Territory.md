# Territory

Status: Background authoritative polygon expansion active; chunk pipeline paused

Last reviewed: 2026-08-26

## 책임

플레이어 영역 상태와 확장을 관리하고 Grid, Fog, Resource Spawn, Monster 등 소비자에게 영역 변경을 제공한다.

## 현재 기준과 목표

- 현재 authoritative 경로: `Assets/02_Scripts/Territory/TerritorySystem.cs`와 Legacy `Territory`
- 목표 경로: `Assets/02_Scripts/Territory Refactor/` 아래 Chunk·Trail 기반 구현
- 승인된 cutover 전에는 Legacy가 기준이며 새 경로가 같은 부작용을 중복 실행하면 안 된다.
- 현재 제품 세션은 최대 2인이 스테이지 시작부터 함께하고 한 Peer가 이탈하면
  스테이지가 종료된다. Late Join·재접속 복구·다수 Peer 보안 경계는 요구사항이
  아니며, 후속 네트워크 작업은 Host/Client Runner 체감 동등성과 frame 안정성에
  필요한 최소 계약만 유지한다.

## 주요 진입점

- `TerritorySystem`, `Territory`, `TerritoryExpansion`, `TerritoryVisible`
- `TerritoryTrailSegmentIndex`, `TerritoryTrailChunkRenderer`
- `TerritoryBoundsIndex`, `TerritoryMeshData`
- 신규 순수 기반: `ProjectIO.Territory.ChunkDomain` assembly의
  `FixedTerritoryPoint`, `TerritoryChunkCoordinate`,
  `TerritoryAppendOnlyBlockList`,
  `TerritorySegmentChunkTraversal`, `TerritoryTrailSession`,
  `TerritoryTrailPacket`, `TerritoryTrailPacketizer`,
  `TerritoryTrailReceiver`, `TerritoryTrailShadowComparer`
- Chunk state 기반: `TerritoryChunkFill`, `TerritoryChunkCoverage`,
  `TerritoryChunkSnapshot`, `TerritoryChunkStateBuilder`, `TerritoryChunkStore`
- Chunk 복제 기반: `TerritoryChunkTransferPacket`,
  `TerritoryChunkTransferPacketizer`, `TerritoryChunkReplica`
- 정밀 확장 계획 기반: `TerritoryBoundaryLoopIndex`,
  `TerritoryChunkExpansionSession`, `TerritoryChunkExpansionPlan`,
  `TerritoryChunkExpansionMetrics`
- 압축 변경 영역 기반: `TerritoryChunkExpansionMaterializationSession`,
  `TerritoryChunkExpansionMaterialization`, `TerritoryChunkBoundaryEdit`,
  `TerritoryChunkFillRun`, `TerritoryChunkExpansionMaterializationMetrics`
- 영속 압축 상태 기반: `TerritoryCompactSnapshot`,
  `TerritoryPersistentBoundaryTree`, `TerritoryCompactRowCoverage`,
  `TerritoryCompactApplySession`, `TerritoryCompactStore`,
  `TerritoryBoundarySplice`
- 백그라운드 정밀 확장 기반: `TerritoryCompactExpansionWorkItem`,
  `TerritoryCompactExpansionWorker`, `TerritoryCompactExpansionWorkResult`,
  `TerritoryCompactExpansionShadow`
- 현재 gameplay 확장 기반: `TerritoryBackgroundExpansionWorkItem`,
  `TerritoryBackgroundExpansionWorker`, `TerritoryBackgroundExpansionResult`,
  `TerritoryExpansionPresentationData`, `TerritoryExpansionResultPacketizer`,
  `TerritoryExpansionResultReplica`, `TerritoryExpansionReplication`
- State Authority adapter: `TerritoryTrailShadowRecorder`,
  `TerritoryTrailReplicationStream`, `TerritoryChunkReplicationStream`
- `.agents/skills/build-chunk-territory/`

Input Authority owner는 local fixed Trail을 즉시 표시한다. State Authority는
confirmed sample을 bounded Reliable packet으로 보내고 Proxy는 같은 ordered Chunk
path와 Unreliable live head를 표시한다. 현재 확장 결과와 모든 consumer에는 계속
Legacy polygon이 authoritative하다. 단, 재진입 frame에서 동기 계산하지 않고 State
Authority가 source polygon과 confirmed Trail snapshot을 단일 background CPU worker에
넘긴다. worker가 polygon 확장·검증·triangulation·bounded packet 준비를 완료한 뒤
main thread가 같은 결과를 Host와 Proxy에 원자 적용한다.

장기 Trail sample, fragment, owner prediction과 renderer pool은 256개 단위 fixed block에
append한다. 같은 Chunk에 계속 머무르는 경로도 fragment당 최대 256 point로 나누고
인접 fragment가 exact endpoint를 공유하므로 기록된 모양은 바뀌지 않는다. confirmed
packet은 최대 24 sample이며 이미 전송한 앞부분을 제거하기 위해 남은 전체 backlog를
이동하지 않는다. 이 경계는 Trail 이동 hot path만 다루며 영역 확장 계산은 변경하지 않는다.

State Authority는 초기 Territory와 Legacy 정상 확장 결과를 revisioned sparse
Chunk shadow snapshot으로도 Commit한다. snapshot은 전역 polygon을 보관하지 않고
미저장 `Empty`, payload 없는 `Full`, fixed local 경계 선분을 가진 `Boundary`로
나뉜다.

고정 2인 세션의 State Authority는 정상 Commit의 changed-Chunk를 최대 48-word
Reliable packet, simulation tick당 최대 2 data packet 예산으로 기존 Client Proxy에
보낸다. 최초 `0 -> 1`과 이후 연속 revision delta만 사용하고 Proxy는 transaction
전체를 검증한 뒤에만 shadow replica를 원자적으로 교체한다. 제품에 없는 Late Join,
재접속 snapshot, recovery retry와 targeted PlayerRef 전송은 지원하지 않는다. 이
replica는 아직 표시와 consumer에 연결되지 않으며 전송 실패도 Legacy 성공 결과를
되돌리지 않는다.

C008 순수 planner는 C006 Boundary sequence를 revision당 한 번 exact fixed loop와
Chunk-local 후보로 색인한다. C003/C004 fragment는 Runner 이동 중 한 번만 append하며
진출·재진입 접점, 외부 Trail, 자기 교차와 면적을 증분 누적한다. terminal은 전체
Boundary나 긴 Trail을 재탐색·복사하지 않고 두 arc 후보를 평가해 현재 게임 규칙의
큰 확장 후보를 immutable plan으로 만든다. fixed 정밀도 뒤 모양을 바꾸는 단순화는
없다. 이 plan은 아직 Legacy 확장이나 C006 Commit에 연결되지 않아 현재 runtime
결과와 Inspector 구성은 바뀌지 않는다.

C009 순수 materializer는 C008 plan의 exact Trail과 실제로 교체되는 기존 Boundary
arc만 읽는다. 긴 선분의 Chunk 분할, scanline 교차, Boundary Chunk 제외와 결과
생성을 `TryStep(maxWorkUnits)` budget으로 나누며 완료 전 candidate를 공개하지 않는다.
Boundary Chunk에는 Trail과 교체 arc의 fixed local segment를 그대로 보존하고, 넓은
추가 영역 내부는 개별 Full Chunk가 아니라 `(Y, MinX..MaxX)` run으로 압축한다.
source snapshot의 Full Chunk나 전체 Boundary를 순회·복사·재번호화하지 않는다. 이
결과도 아직 C006 store, Legacy 확장, Fusion 또는 renderer에 연결되지 않아 runtime과
Inspector 구성은 바뀌지 않는다.

C010 순수 store는 C006 shadow snapshot을 최초 한 번 counter-clockwise stable
Boundary와 Y별 Full run으로 바꾼다. 이후 C008/C009는 이 compact snapshot을 직접
입력으로 사용하고, 확장은 제거 arc의 stable identity와 exact endpoint residual/Trail
part만 splice한다. 변경된 Boundary 경로와 Full row만 persistent balanced node로
교체하며 나머지 branch는 이전 immutable revision과 공유한다. 정상 apply는 source
전체 scan, 전역 sequence 재번호화와 Full Chunk 전개를 하지 않고 terminal에서만 새
revision을 공개한다. 이 store 역시 아직 runtime, Fusion, renderer와 consumer에
연결되지 않아 현재 플레이에는 변화가 없다.

C011 State Authority shadow는 confirmed Trail fragment를 이동 중 증분 수집하고,
재진입 시 긴 목록을 다시 복사하지 않고 단일 백그라운드 CPU queue에 소유권을 넘긴다.
worker는 immutable compact revision 순서대로 C008 plan, C009 materialization과 C010
apply를 실행하며 완료 결과는 `Render`에서 한 건씩 stale source 검증 뒤 원자 publish한다.
worker는 Unity/Fusion API에 접근하지 않고 실패·취소·teardown은 마지막 compact
revision과 Legacy gameplay를 보존한다. exact 도메인의 `decimal`과 persistent managed
tree를 복제하는 Burst/GPU 이중 경로는 만들지 않았으며 profile 결과로 독립 배열 병목이
확인될 때만 후속 가속 후보를 정한다. 이 compact shadow는 현재 gameplay 확장에서
연결 해제됐으며 진단·연구 코드로만 남아 있다.

현재 active 확장은 C013 단순 background polygon 경로 하나다. 계산이 진행되는 동안
마지막 완료 Territory로 판정·표시하고 Runner와 Fusion simulation은 계속 진행한다.
입력 Trail에는 prototype vertex budget이나 tolerance 단순화를 적용하지 않는다.
완료 vertex의 float bit와 worker가 만든 triangle index를 최대 48-word Reliable packet,
tick당 최대 2 data packet으로 Proxy에 보내며 terminal 이전에는 보이는 Territory를
바꾸지 않는다. 계산 중에는 다음 획득 Trail을 시작하지 않고 완료 뒤 현재 위치에서 다시
시작한다. Mesh upload와 consumer event만 Unity main thread에서 실행한다.

완료 result packet은 이전 polygon patch가 아니라 전체 vertex float bit와 triangle index
snapshot이다. State Authority는 현재 완료 revision과 pending을 Networked 지속 상태로
advertise하고 마지막 완료 snapshot을 보관한다. Client replica는 현재 revision보다 앞선
검증된 전체 result를 원자 적용할 수 있으며, 이미 적용한 replay는 no-op이고 같은 inbound
transfer의 Begin/Data/Complete replay는 progress를 손상하지 않는다. sequence gap,
conflicting future transfer와 malformed payload는 공개 상태를 바꾸지 않는다. Client가
advertised revision보다 뒤처지면 한 번의 outstanding targeted Reliable recovery request를
보내며, 2초 뒤에도 수렴하지 않으면 다시 요청한다. State Authority는 요청 peer에 최신
완료 full snapshot을 packet당 48 word, simulation tick당 전체 normal/recovery 합산 최대
2 data packet으로 재전송한다. 이 계약은 2인 세션의 presentation 복구만 다루고 Late
Join/reconnect 일반 지원은 추가하지 않는다.

`GameWorld`의 Scene Territory NetworkObject는 AOI 대상이 아니라 global interest다.
Territory는 월드 전역 판정·표시 상태이고 stage world boundary 1000이 player AOI 반경
128보다 크므로, global 지속 상태 하나의 작은 Networked revision/pending과 recovery
packet 비용을 감수해 AOI 밖 Client의 revision 고착을 막는다. AOI grid, 다른 object의
interest, Chunk shadow의 presentation/consumer 전환은 이 경로에 포함하지 않는다.

## 주요 소비자

Grid 표시, Fog of War, Resource 수집·Spawn, Track·World Monster, Sacred Zone, PlayerRunner 보호 판정.

## 관련 Asset

- `GameWorld.unity`
- Territory·Track 표시 오브젝트와 Core 연결
- 장기 개발 문서는 Territory Skill이 지정한 경로를 따른다.

## 변경 시 확인

- State Authority와 시작부터 함께한 Client Proxy의 연속 delta
- 좌표·revision·변경 Chunk 계약
- Legacy와 새 경로의 중복 이벤트·표현
- 모든 소비자 전환 여부와 롤백
- `Territory.Vertices`를 갱신하는 현재 seam이 `ReplaceVertices`를 통해 bounds
  cache를 같은 시점에 갱신하는지
- 확장 결과의 검증된 triangle data를 Host mesh에 재사용하고 같은 polygon을
  중복 triangulation하지 않는지
- Chunk domain의 1 world unit = 256 fixed unit, 8 world-unit Chunk,
  half-open 소유권과 Trail sequence 계약을 변경하지 않는지. 변경이 필요하면
  `Docs/LongRunning/Territory/CONTRACTS.md`와 migration을 먼저 갱신한다.
- C008 planner 입력의 첫 fragment가 마지막 영역 내부점 또는 정확한 경계 anchor를
  포함하고, 이후 fragment가 sequence와 shared endpoint를 유지하는지 확인한다.
- C009 materialization은 source/index/plan revision과 session이 일치하는지, 호출별
  work 사용량이 budget 이하인지, Full run과 Boundary edit가 `(Y, X)` 결정 순서를
  유지하는지 확인한다.
- C010 apply는 stable splice가 contiguous한지, endpoint가 retained Boundary에 exact로
  연결되는지, source와 candidate root가 terminal 전후에도 immutable한지, 정상
  metrics의 전체 scan/renumber/Full 전개가 0인지 확인한다.
- C011 worker는 State Authority에서만 session당 한 번 enqueue되는지, confirmed
  fragment drain이 이동 중 증분인지, queued source revision과 main-thread publish가
  같은 순서인지, teardown 뒤 완료 결과가 공개되지 않는지 확인한다.
- C013 worker는 재진입 frame에서 polygon 검증·triangulation을 실행하지 않는지,
  source revision당 한 결과만 적용되는지, 256개가 넘는 유효 Trail 꺾임이 그대로
  유지되는지, packet당 48 word와 tick당 2 data packet 상한을 지키는지 확인한다.
- C013 계산 실패는 마지막 완료 Territory를 보존하고 다음 정상 확장을 다시 예약할 수
  있어야 한다. Host-local/Client Runner 양쪽에서 계산 중 이동·렌더링·Fusion tick과
  terminal 동시 적용 체감을 실제 runtime과 Profiler로 확인한다.

## 외부 강제 이동 사선 중단

- `TerritorySystem`은 State Authority에서 진행 중인 사선을 외부 강제 이동 동안
  일시 정지하고 Reliable suspension lifecycle을 보낸다.
- owner prediction은 마지막 confirmed point까지 reconcile하고 pause 중 강제 이동을
  선으로 추가하지 않는다. 재개 시 현재 위치의 강제 경로점부터 이어지며 기존
  자기 교차·Lifeline·영토 확장 규칙을 그대로 적용한다.

## 기술 부채

현재 기능 문서와 장기 milestone 문서가 분리되어 있지 않은 부분이 있다. Territory Skill은 장기 마이그레이션 절차를 계속 소유한다.
Legacy polygon은 계속 authoritative지만 확장 계산은 C013 background worker에서만
실행된다. 현재 계산은 정확한 결과를 우선한 기존 polygon 알고리즘이므로 총 latency와
메모리는 입력 vertex 수에 따라 증가한다. main-thread frame 중단을 제거하는 것과 총
계산 시간을 0으로 만드는 것은 구분하며, 실제 병목이 확인될 때만 알고리즘 교체를 별도
예약한다. 공개 mutable `Vertices`를 직접 쓰는 새 소비자를 추가하지 않는다.
