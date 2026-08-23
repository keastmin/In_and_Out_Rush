# Current Milestone

Status: Complete

## Objective

State Authority의 영역 재진입 확장을 동기 main-thread 계산에서 단일 background CPU
작업으로 교체한다. 계산 중에는 마지막 완료 Territory로 게임을 계속하고, 완료 polygon과
triangle을 Host와 기존 Client Proxy에 같은 결과로 원자 적용한다.

장거리 exact Trail의 C012 기록·전송·표시 계약은 유지한다. C008-C011 compact shadow는
active gameplay 확장에서 연결 해제하며 GPU/Burst fallback, consumer의 chunk-native 전환,
Late Join과 reconnect는 포함하지 않는다.

## Prerequisites

- Active reservation `W-20260823-002-background-authoritative-territory-expansion`
- reservation commit `1e4469621ed4ba7f28c6876b503640b4ac4eda55`
- `CONTRACTS.md`의 C001-C005, C012와 C013

## Required behavior

- 재진입 frame에는 source Territory와 authoritative confirmed Trail을 snapshot하고 worker를
  예약하는 짧은 작업만 수행한다.
- worker-local 계산에서 polygon 확장, finite/simple 검증, triangulation과 bounded packet
  준비를 모두 끝낸다.
- prototype 256 vertex budget, RDP와 tolerance 기반 단순화를 사용하지 않는다.
- 계산 중 simulation, 입력, Runner 이동, Render와 Fusion tick을 차단하지 않는다. 마지막
  완료 Territory를 계속 사용하고 동시에 두 확장 mutation을 시작하지 않는다.
- 성공 결과는 main thread에서 source revision을 확인하고 Mesh, Territory와 consumer event에
  한 번 적용한다. 실패·취소·stale 결과는 이전 Territory를 보존하고 다음 정상 요청을 막지
  않는다.
- 완료 vertex float bit와 triangle index를 최대 48-word Reliable packet, tick당 최대 2
  data packet으로 Proxy에 보낸다. Proxy는 terminal에서만 동일 결과를 적용하며 재계산이나
  재삼각분할을 하지 않는다.
- Host-local Runner와 Client Runner가 같은 State Authority schedule/result 경로를 사용한다.

## Implemented files

- `TerritorySystem`, `Territory`, `TerritoryVisible`
- `TerritoryBackgroundExpansionWorkItem`, `TerritoryBackgroundExpansionWorker`,
  `TerritoryBackgroundExpansionResult`, `TerritoryExpansionPresentationData`
- `TerritoryExpansionResultPacket`, `TerritoryExpansionResultPacketizer`,
  `TerritoryExpansionResultReplica`, `TerritoryExpansionReplication`
- `TerritoryBackgroundExpansionWorkerTests`

Scene, Prefab, Inspector와 serialized data 변경은 없다.

## Current evidence

- Unity가 재생성한 `Assembly-CSharp`와 `Assembly-CSharp-Editor` build가 모두 오류 0개다.
  기존 runtime warning 13개, Editor warning 8개가 남는다.
- 300개 이상의 유효 Trail 꺾임을 main thread와 다른 worker thread에서 계산하고 결과
  vertex가 256개를 넘는 것을 확인했다.
- 모든 결과 vertex float와 triangle index가 최대 48-word packet 분할·재조립 뒤 동일했다.
- invalid 작업 실패 뒤 같은 worker의 다음 정상 확장이 성공했다.
- outbound result stream이 simulation tick당 data packet 2개 상한을 지키는 테스트가
  통과했다. 신규 background/codec 테스트는 3/3 통과했다.
- 작업자가 안내한 Host-local/Client Runner 양방향 정상 확장, 계산 중 플레이 연속성,
  양쪽 Peer 결과 모양, 연속 확장과 compact 오류 미발생을 수동 확인했다.

## Worker/Unity boundary

worker는 managed snapshot과 worker-local geometry만 처리한다. Unity `Mesh`, Scene object,
Fusion RPC와 consumer event는 main thread에서만 실행한다. 최종 Mesh upload 비용은 Unity
제약상 main thread에 남으므로 Profiler에서 별도로 확인한다.

## Manual acceptance

1. Unity import와 Console compile error 없음
2. Territory EditMode의 신규 background worker 테스트 통과
3. Host-local Runner와 Client Runner 각각 정상 재진입 확장 성공
4. 양쪽 Peer가 같은 모양을 terminal 이후 표시하고 이전 compact schedule 오류가 없음
5. 긴 Trail 확장 계산 중 이동·카메라·렌더링·Fusion이 끊기지 않음
6. 연속 확장 성공. invalid 작업 뒤 worker 재사용은 집중 테스트로 검증
7. 정량 Profiler 수치는 별도로 측정하지 않았다. worker thread 실행과 tick당 2 data
   packet 상한은 집중 테스트로 검증했다.

## Known limits

- 정확한 전체 입력을 유지하므로 총 계산 시간과 메모리는 vertex 수에 따라 증가하며 무한
  입력을 유한 시간·메모리로 보장하지 않는다. 이번 교체는 그 계산이 frame을 멈추지 않게
  분리하는 작업이다.
- 계산 중에는 다음 획득 Trail을 기록하지 않는다. Runner와 다른 gameplay는 계속되고 완료
  적용 뒤 현재 위치에서 새 Trail을 시작할 수 있다.
- 완료 Mesh upload와 consumer event는 main thread 작업이다.

## Rollback

W-20260823-002 구현 commit을 되돌리면 reservation 기준의 기존 동기 polygon/compact shadow
상태로 복귀한다. Scene·Prefab 복구는 필요 없다.
