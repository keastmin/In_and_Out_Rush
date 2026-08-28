# W-20260828-001 Territory 미사용 프로토타입 정리

Status: Reserved

## 동기화 기준

- Base Commit: 3a52c457523b2dfbfa93b13822041562aba96c28
- 공용 Upstream: origin/rebuild-development-environment
- CheckStart: `READY_TO_CHECK_CONFLICTS` (`AHEAD=0`, `BEHIND=0`, 2026-08-28)

## 담당자

Codex `/root`

## 기능

Territory의 현재 gameplay에서 소비되지 않는 C006-C011 Chunk shadow, delta replication,
exact expansion plan/materialization, persistent compact store와 background compact shadow를
제거하고 현재 활성 C013 polygon 확장과 C001-C005/C012 Trail 기준으로 문서와 Skill을
정리한다.

## 목표

- gameplay query, 표시와 consumer에 연결되지 않은 C006-C011 runtime, Fusion adapter,
  Unity adapter와 전용 테스트를 제거한다.
- `TerritorySystem`에서 초기 Chunk shadow commit, 전송 queue와 Chunk delta RPC 처리만
  제거하고 현재 C013 polygon expansion result replication과 Trail lifecycle은 유지한다.
- 제거된 prototype을 현재·미래 전제처럼 서술하는 장기 Territory 문서를 제거하고
  `Docs/Features/Territory.md`를 실제 활성 코드와 일치시킨다.
- `build-chunk-territory` Skill에서 폐기된 C006-C011 계약, 고정 milestone 문서 세트와
  과거 코드 위치를 구현 전제로 강제하는 규칙을 제거한다. 이후 사용자가 별도로 요청할
  설계·구현 작업은 새 예약과 새 설계 문서에서 결정한다.
- 동기화, Active 충돌, 정확한 파일 예약, Authority, Host/Client 동등성, 중복 부작용
  방지, 직렬화와 검증 안전장치는 유지한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/*`
- `manage-feature-work`
- `build-chunk-territory`
- `replace-existing-feature`
- `photon-fusion-feature`
- `skill-creator`

## 예상 수정 코드

### 활성 연결 정리

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryChunkReplicationStream.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryChunkReplicationStream.cs.meta`
- `Assets/02_Scripts/Territory Refactor/Adapters/Unity/TerritoryCompactExpansionShadow.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Unity/TerritoryCompactExpansionShadow.cs.meta`
- `Assets/02_Scripts/Territory Refactor/Adapters/Unity.meta`

### 제거할 C006-C011 순수 runtime

아래 `.cs`와 각 파일의 동일 경로 `.cs.meta`를 함께 제거한다.

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryBoundaryLoopIndex.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryBoundarySegmentId.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryBoundarySplice.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkBoundaryEdit.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkBoundarySegment.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkCommitResult.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkCoverage.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionMaterialization.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionMaterializationMetrics.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionMaterializationSession.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionMetrics.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionPlan.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkExpansionSession.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkFill.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkFillRun.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkLocalPoint.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkReplica.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkSnapshot.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkStateBuilder.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkStore.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferPacket.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkTransferPacketizer.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactApplyMetrics.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactApplySession.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactBoundarySegment.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactCommitResult.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactExpansionWorker.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactExpansionWorkerMetrics.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactExpansionWorkItem.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactExpansionWorkResult.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactRowCoverage.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactSnapshot.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactSnapshotBuilder.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryCompactStore.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryPersistentAvlMap.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryPersistentBoundaryTree.cs`

### 제거할 전용 테스트

아래 `.cs`와 각 파일의 동일 경로 `.cs.meta`를 함께 제거한다.

- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryBoundaryLoopIndexTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkExpansionMaterializationSessionTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkExpansionSessionTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkFillRunTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkReplicaTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkStateBuilderTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkStoreTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkTransferPacketizerTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryCompactApplySessionTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryCompactExpansionWorkerTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryCompactSnapshotTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryCompactStoreTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryPersistentBoundaryTreeTests.cs`

### 문서와 지침

- `AGENTS.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/build-chunk-territory/references/work-session-protocol.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- `Docs/Work/Active/W-20260828-001-territory-prototype-cleanup.md`

기존 `Docs/Work/Completed/` 기록은 당시 수행 증거인 변경 이력으로 보존한다.

## 예약 Scene·Prefab·Data Asset

없음. Scene, Prefab, ScriptableObject, Inspector 설정과 `.meta` GUID 참조는 변경하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- 현재 활성 `TerritorySystem`에서 소비되지 않는 초기 C006/C007 shadow 상태와 Chunk delta
  RPC만 제거한다.
- 현재 C013 `AdvertisedTerritoryExpansionRevision/Pending`, expansion result/recovery RPC,
  C001-C005/C012 Trail replication과 외부 `OnTerritoryExpandedEvent` 계약은 유지한다.
- `StageBootstrapper`, `TerritoryVisible`, Grid, Fog, Resource, Monster와 Sanctuary의 공개
  연결은 변경하지 않는다.
- 장기 Territory 문서와 Skill은 현재 활성 구현을 정확히 설명하고, 다음 설계가 과거
  prototype 계약에 묶이지 않도록 정리한다. 미래 공개 query, Chunk/Quadtree 구조,
  consumer migration 계약은 이번 작업에서 정의하지 않는다.

## 네트워크·Peer 동등성

- 입력 원점과 gameplay 변경 owner는 바꾸지 않는다. Input Authority owner prediction,
  State Authority confirmed Trail과 C013 authoritative polygon expansion을 그대로 유지한다.
- State Authority가 초기 설정 때 gameplay에서 소비되지 않는 Chunk shadow를 만들고
  C007 delta RPC를 보내던 진단 경로만 제거한다.
- Host-local과 Client는 계속 같은 confirmed Trail 및 C013 full result/recovery 계약으로
  영역 확장 결과와 표시를 관찰한다. Host 중복 적용, Client presentation, 실패 복구,
  teardown 정리 동작은 바뀌면 안 된다.
- Late Join·reconnect의 제품 요구사항은 변경하지 않는다. C013의 기존 advertised revision과
  recovery retry를 보존한다.
- Unity/Fusion Weaver compile 후 Host-local/Client Runner의 Trail 시작·확장·Abort,
  연속 확장, 양쪽 최종 모양과 teardown을 확인한다. 실제 다중 Peer 실행을 자동화하지
  못하면 작업자 수동 절차와 미검증 상태를 정확히 기록한다.

## 다른 활성 작업과 겹치는 부분

- `W-20260827-001-runner-dual-pistols`는 `PlayerRunner`, `NetworkInputSystem`,
  `NetworkInputData`, Runner weapon 코드와 Player Runner/Projectile prefab을 예약한다.
- 이번 작업은 해당 파일·Prefab을 수정하지 않는다. `AGENTS.md`의 Territory 전환 문구와
  Territory 전용 Skill만 좁게 바꾸며 Runner weapon의 계약·검증 절차에는 영향을 주지 않는다.

## 범위 밖

- 대화에서 논의한 Chunk-local grid/Quadtree, 새 Territory query와 새 expansion 구현
- 미래 구현 예약, 설계 문서, migration/cutover 문서 추가
- 현재 Legacy polygon, C013 background expansion, containment index 또는 consumer 제거
- C001-C005/C012 fixed Trail 기록·전송·표시와 자기 교차 동작 변경
- Grid, Fog, Resource, Monster, Sacred Zone, Sanctuary와 PlayerRunner 호출자 이식
- Scene, Prefab, ScriptableObject, ProjectSettings, Package 변경
- `Docs/Work/Completed/` 과거 작업 기록 삭제 또는 재작성

## 완료 조건

- C006-C011 runtime/adapter/test 타입과 대응 `.meta`가 제거되고 `rg`에 활성 참조가 없다.
- `TerritorySystem`에 Chunk shadow store, delta queue/RPC/receiver가 남지 않으며 C013/Trail
  경로는 동일하게 컴파일된다.
- `Docs/Features/Territory.md`와 `Docs/PROJECT_MAP.md`가 실제 활성 코드, 소비자와 네트워크
  경계를 설명한다.
- 폐기된 long-running plan 문서와 이를 강제하는 Skill reference를 제거하고,
  `build-chunk-territory` Skill은 다음 사용자 요청이 정한 설계와 예약 범위를 안전하게
  오케스트레이션할 수 있게 좁고 현재적인 지침만 유지한다.
- 변경된 Skill이 `skill-creator` validator를 통과한다.
- 유지 대상 ChunkDomain/Trail 테스트, Territory containment/background expansion 테스트,
  프로젝트/Fusion compile이 통과한다.
- Host-local/Client runtime을 수행하지 못하면 정확한 수동 검증 절차와 미검증 항목을 남긴다.
- `git diff --check`와 `git status`로 예약 밖 변경과 공백 오류가 없다.

## 실제 변경

예약 진행과 원격 검증 뒤 기록한다.

## 검증 결과

- `CheckStart`: `READY_TO_CHECK_CONFLICTS` (`AHEAD=0`, `BEHIND=0`)
- 기존 Active 의미 충돌: 없음. Runner weapon 예약 파일과 Asset을 제외했다.
- 구현·compile·runtime: 예약 진행 뒤 수행한다.

## 남은 위험

- C006/C007 RPC 제거는 gameplay 결과를 바꾸지 않아야 하지만 Fusion Weaver가 생성하는
  RPC surface가 달라지므로 Unity compile과 실제 Host/Client 연결 회귀 확인이 필요하다.
- Unity가 생성한 project file이 삭제 source를 계속 가리키면 Unity import/regeneration 후
  compile해야 한다.
- 미래 설계가 확정되기 전에는 현재 Legacy polygon과 C013 full result replication이 계속
  유일한 authoritative Territory 결과다.
