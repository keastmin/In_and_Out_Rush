# W-20260821-012 Chunk Territory 예측·확정 Trail 동기화

Status: Reserved

## 동기화 기준

- Base Commit: 890fa5c48b29f7f89ab7d4e8a46e198ff25e33e9
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Territory, Player Runner Trail, Fusion 동기화, 장기 Chunk Territory 전환.

## 목표

Input Authority의 Runner는 자신의 이동 Trail을 네트워크 왕복을 기다리지 않고
fixed 좌표로 즉시 예측 표시한다. State Authority는 현재 Legacy 판정과 나란히
기록하는 ordered Chunk Trail을 유일한 확정 원본으로 사용해 session 수명,
확정 point/fragment와 최신 live head를 제한된 크기의 Fusion payload로 전송한다.

Host 로컬 Runner와 Client Input Authority Runner는 같은 예측·확정 presentation
계약을 사용하며, Proxy는 누적 전체 경로 재전송 없이 확정 경로와 최신 머리점을
이어 표시한다. 이번 작업은 선의 표시와 전송만 변경하며 Territory polygon,
확장 계산, 성공·실패 판정과 consumer 결과는 계속 Legacy State Authority 경로가
결정한다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/Features/PlayerRunner.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/build-chunk-territory/references/work-session-protocol.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailShadowRecorder.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailReplicationStream.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailReplicationStream.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailPacket.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailPacket.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailPacketizer.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailPacketizer.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailReceiver.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailReceiver.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailPacketizerTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailPacketizerTests.cs.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailReceiverTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailReceiverTests.cs.meta`
- `Assets/02_Scripts/Territory Refactor/Trail/TerritoryTrailChunkRenderer.cs`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- `Docs/Features/Territory.md`
- `Docs/Features/PlayerRunner.md`
- 이 작업 문서

신규 파일 이름은 구현 중 책임이 더 명확한 동등 파일로 바꿀 수 있으나,
`Territory Refactor`의 위 세 폴더와 예약된 문서 밖으로 범위를 넓히지 않는다.

## 예약 Scene·Prefab·Data Asset

없음. `GameWorld.unity`, `GamePresentation.unity`, `GameRoot.unity`, `Core.prefab`,
`Player Runner.prefab`, `Territory.prefab`, Material, Shader, Compute Shader,
ScriptableObject와 Fusion 설정은 수정하지 않는다. 기존 `TerritorySystem`의
`lineRenderer` 참조를 template로 재사용하므로 작업자 인스펙터 연결도 추가하지
않는다.

## 공용 계약 또는 Bootstrapper 변경

- C001-C004 fixed/Chunk/traversal/session 계약은 변경하지 않는다.
- C005 Trail transport 계약을 추가한다. lifecycle과 확정 payload는 Reliable,
  최신 live head는 Unreliable/latest-wins이며 어느 RPC도 Territory 권위 상태를
  소유하지 않는다.
- wire 좌표는 C001 fixed `int` 쌍만 사용한다. 전송 이후 tolerance 증가,
  vertex budget 단순화나 모양 보정을 적용하지 않는다.
- 확정 경로 payload는 packet당 최대 24 point로 분할하고 session, point/fragment,
  packet sequence로 재조립한다. 한 network tick의 reliable 발행량도 제한해 Chunk
  이탈 때 큰 fragment가 생겨도 한 프레임에 전체를 처리하지 않는다.
- Input Authority 예측은 presentation 전용이다. State Authority 확정 Start,
  Commit, Abort와 충돌하면 확정 수명을 따르되 Territory 판정·Kill·Lifeline을
  로컬에서 결정하지 않는다.
- Bootstrapper, `PlayerRunner.OnPositionChanged`, 공개 Territory expanded event와
  consumer 계약은 변경하지 않는다.

## 네트워크·Peer 동등성

- 입력 원점은 기존 `PlayerRunner.Render`의 `OnPositionChanged`다. 해당 Runner의
  Input Authority peer만 fixed 좌표 예측 Trail을 즉시 표시한다.
- State Authority는 기존 Legacy containment, 자기 교차, Lifeline과 확장 성공을
  계속 검증한다. 동시에 ordered Trail session을 기록하고 확정 stream을 만든다.
- Start/확정 packet/Commit/Abort는 State Authority에서 Reliable로 보낸다.
  아직 확정 packet에 포함되지 않은 최신 머리점은 제한된 주기의 Unreliable
  latest-wins 메시지로 보내며 게임 상태나 확정 path에 사용하지 않는다.
- Client owner는 자신의 예측 선을 표시하므로 RPC 왕복 때문에 Runner보다 선이
  뒤처지지 않는다. Host 로컬 owner도 별도 직행 표시가 아닌 같은 예측 계약을
  한 번만 사용해 서버/로컬 역할의 중복 선을 만들지 않는다.
- Proxy는 순서가 검증된 확정 point/fragment에 live head만 임시로 잇는다.
  오래되거나 다른 session payload, 종료 뒤 payload와 sequence gap은 거부하고
  진단한다. Reliable packet은 bounded 재조립 후에만 확정 표시한다.
- 정상 재진입은 마지막 확정 payload를 먼저 flush한 뒤 Commit하고 표시 session을
  정리한다. 자기 교차, Lifeline reset, Stop, teardown과 despawn은 Abort로 예측,
  수신, 전송 대기 payload와 live head를 소각한다.
- 진행 중 Trail의 Late Join 복원은 milestone 5 범위이므로 이번에는 지원하지
  않는다. Late Join peer는 다음 session부터 표시하며 기존 committed Territory는
  계속 Legacy 복원 경로를 사용한다.
- Host 로컬과 Client Input Authority 각각 정상 이탈·장거리 이동·재진입,
  자기 교차/Lifeline, SandTomb pause/resume를 실행해 입력 가능 여부, 즉시 선,
  최종 정리와 게임 결과가 Peer 종류에 따라 달라지지 않는지 확인한다.

## 다른 활성 작업과 겹치는 부분

`CheckStart`와 `Docs/Work/Active/` 확인 결과 README 외 활성 예약이 없어 겹침이
없다.

## 범위 밖

- Territory Chunk Empty/Full/Boundary 저장, fill, revision과 확장 commit
- changed-Chunk replication, recovery와 Late Join 진행 Trail snapshot
- GPU mask/SDF, Material, Compute Shader와 CPU fallback
- Territory containment/Grid/Fog/Resource/Monster consumer migration
- authoritative cutover, Legacy polygon·RPC·계산용 path 삭제
- Scene, Prefab, Bootstrapper, Fusion 설정과 직렬화 참조 변경
- Legacy sampling 간격, 자기 교차, Lifeline, 영역 모양과 확장 결과 변경
- owner의 Territory 확장 결과 예측 표시

## 완료 조건

- Host/Client Input Authority owner가 모두 네트워크 왕복 전 자신의 Trail을
  즉시 표시하고 State Authority 확정 수명으로 정리한다.
- Proxy는 누적 전체 path 재전송 없이 bounded fixed packet과 live head로 선을
  이어 표시한다.
- 최대 24 point packet, packet/point/fragment 순서, 중복·gap·stale 거부와
  다중 packet 재조립이 순수 EditMode 테스트로 고정된다.
- 매우 긴 같은-Chunk fragment와 한 sample의 다중-Chunk 통과도 한 RPC나 한
  프레임의 무제한 배열·처리로 바뀌지 않고 최종 fixed 경로가 원본과 일치한다.
- Abort, Lifeline, Stop, teardown 뒤 예측·확정·live payload와 LineRenderer가
  남지 않으며 다음 단조 SessionId에 stale data가 섞이지 않는다.
- Host가 State/Input Authority를 함께 가져도 같은 point와 선을 중복 추가하거나
  RPC를 로컬 중복 실행하지 않는다.
- Legacy Territory, 확장 결과, consumer callback과 기존 성공·실패 게임 결과가
  변경되지 않는다.
- ChunkDomain EditMode 테스트, 프로젝트 컴파일, Host·Client 수동 절차 또는
  정확한 미검증 기록, `.meta` pairing과 `git diff --check`가 완료된다.
- Scene·Prefab·Material·Shader·Compute·ScriptableObject·Fusion 설정 diff가 없다.

## 롤백

`TerritorySystem`의 예측/확정 presentation hook과 신규 transport/receiver,
renderer 변경 및 C005 문서만 제거한다. 기존 Legacy path RPC와 Territory 확장
경로는 authoritative fallback으로 유지해 이전 표시 방식으로 되돌릴 수 있다.
