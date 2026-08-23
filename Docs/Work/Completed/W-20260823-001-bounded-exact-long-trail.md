# W-20260823-001 정밀 장거리 Trail 비용 상한

Status: Complete

## 동기화 기준

- Base Commit: 92762c09006f5e216d5ee962ea0b75b551fbc5ba
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Runner가 영역 밖에서 그리는 Trail의 정밀 기록, bounded Fusion 전송과 분할 로컬 표시.

## 목표

이번 작업의 목표는 하나다. Runner가 영역 밖에서 매우 오래 이동해도 이미 기록된
Trail 길이 때문에 이동 중 frame 비용이나 네트워크 전송량이 계속 커지지 않게 한다.

- 한 번 확정된 fixed Trail point는 단순화, 병합, 이동 또는 삭제하지 않는다.
- Input Authority는 자신의 선을 즉시 로컬로 그리고, State Authority는 같은 ordered
  point를 실제 계산용 권위 데이터로 보관한다.
- Reliable confirmed packet 한 개의 크기는 현재 상한을 유지하고 긴 이력을 한 RPC로
  합치지 않는다.
- LineRenderer 하나의 point 수는 현재 256개 상한을 유지하고 닫힌 구간은 다시 만들지
  않는다.
- 누적 이력 전체를 이동 frame마다 복사·재구축하거나, 단일 Chunk 안의 매우 긴 경로를
  terminal 시점에 하나의 거대한 fragment로 복사하는 경로를 제거한다.

영역 확장 계산과 결과 표시는 이번 목표가 아니다. 기존 Legacy 확장과 C006-C011
shadow는 동작을 바꾸지 않으며, 이번 Trail 작업을 위해 새 영역 계산을 추가하지 않는다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/Features/PlayerRunner.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- `.agents/skills/manage-feature-work/SKILL.md`
- `.agents/skills/build-chunk-territory/SKILL.md`
- `.agents/skills/build-chunk-territory/references/work-session-protocol.md`
- `.agents/skills/photon-fusion-feature/SKILL.md`

## 예상 수정 코드

Trail 이력을 전체 재할당 없이 fixed-size block으로 append하기 위한 신규 순수 collection과
대응 `.meta`:

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryAppendOnlyBlockList.cs`

정밀 sample·fragment를 bounded append 구조로 저장하고 같은 Chunk 안의 긴 fragment도
공유 endpoint를 가진 고정 크기 fragment로 나누기 위한 기존 파일:

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailSession.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailFragment.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailReceiver.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailShadowRecorder.cs`

local owner의 표시용 이력과 segmented renderer가 누적 길이 전체를 재할당·재구축하지 않게
연결할 기존 파일:

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Trail/TerritoryTrailChunkRenderer.cs`

bounded packet 계약의 회귀가 발견된 경우에만 수정할 파일:

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailPacketizer.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailReplicationStream.cs`

신규·기존 집중 테스트와 대응 `.meta`:

- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryAppendOnlyBlockListTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailSessionTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailPacketizerTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailReceiverTests.cs`

문서:

- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- 이 작업 문서

조사 중 위 경계 밖의 코드나 Asset 변경이 필요하면 먼저 이 예약을 갱신하고 작업자의
다음 진행 요청과 원격 예약 검증을 다시 거친다.

## 예약 Scene·Prefab·Data Asset

없음. Scene, Prefab, Material, Shader, Compute Shader, ScriptableObject, asmdef, Package,
ProjectSettings와 Inspector 참조를 변경하지 않는다. Unity가 자동 갱신하는 solution과
생성 `.csproj`도 예약·커밋하지 않는다.

## 공용 계약 또는 Bootstrapper 변경

- 기존 1 world unit = 256 fixed unit과 ordered session/sample/packet sequence를 유지한다.
- 기록된 point의 모양을 바꾸는 tolerance 증가, 간소화와 point budget을 도입하지 않는다.
- 정확한 전체 이력은 메모리에 선형으로 남지만, append 한 번의 작업과 재할당 크기는
  전체 point 수와 무관하게 제한한다.
- 하나의 Chunk 안에서 fragment point 상한에 도달하면 마지막 point를 다음 fragment의
  첫 point로 공유하여 정확한 선형 경로를 보존한다.
- 현재 최대 24 sample의 confirmed packet과 최대 256 point의 LineRenderer 구간 상한을
  늘리지 않는다.
- Bootstrapper와 Territory 공개 결과 계약은 바꾸지 않는다.

## 네트워크·Peer 동등성

- Host Runner와 Client Runner 모두 Input Authority peer에서 local line을 즉시 append한다.
- State Authority만 authoritative sample sequence를 만들고 자기 교차 등 실제 판정에
  사용될 exact Trail 이력을 소유한다.
- Proxy는 State Authority의 bounded Reliable packet과 Unreliable live head를 표시하며,
  local owner prediction과 confirmed stream이 같은 point 정밀도를 사용한다.
- Host가 local owner와 State Authority 역할을 함께 가져도 같은 point를 renderer나
  authoritative store에 두 번 append하지 않는다.
- Begin, 장시간 append, suspension, Commit, Abort와 teardown에서 Host-local Runner와
  Client Input Authority Runner의 선 시작·연속성·정리 체감이 같아야 한다.
- 제품 범위에 없는 Late Join, reconnect, AOI 재진입, 다수 Peer 보안과 recovery 전송은
  추가하지 않는다.

## 다른 활성 작업과 겹치는 부분

`CheckStart` 결과 local/upstream은 `92762c0`에서 동기화됐고
`Docs/Work/Active/`에는 안내용 README 외 예약이 없어 겹침이 없다.

## 범위 밖

- 영역 확장 polygon, C008-C011 계산, compact state, mesh와 consumer 전환
- Territory vertex RPC, Chunk Territory delta와 visible Territory 변경
- Runner Item·Slash의 전체 경로 공격 판정 최적화
- GPU, Burst, Job System과 별도 CPU/GPU fallback
- Trail 모양 단순화, 거리 기반 과거 point 삭제와 데이터 손실 압축
- Scene, Prefab, Inspector, Bootstrapper, asmdef, Package와 ProjectSettings 변경
- Late Join, reconnect, AOI, 다수 Peer와 보안 확장

## 완료 조건

- 같은 Chunk 안의 100,000개 이상 ordered point와 여러 Chunk를 오가는 장거리 Trail을
  point 손실 없이 append하고 다시 읽었을 때 입력과 순서가 정확히 일치한다.
- sample·fragment·local presentation 이력의 append가 이전 전체 이력을 복사하거나
  길이에 비례해 순회하지 않는다.
- fragment 하나와 LineRenderer 하나가 각각 승인된 point 상한을 넘지 않으며 인접
  구간은 정확히 같은 endpoint를 공유한다.
- confirmed RPC payload는 현재 packet 상한을 넘지 않고 긴 경로를 terminal RPC 하나로
  합치지 않는다.
- 이동 중 이미 닫힌 renderer 구간을 Rebuild하거나 모든 confirmed point를 매 frame
  복사하지 않는다.
- Host-local Runner와 Client Input Authority Runner가 걷기·달리기·속도 전환과 장거리
  Trail에서 즉시 연속된 선을 보고, 다른 Peer도 같은 confirmed 선을 순서대로 본다.
- 자기 교차 Abort, suspension/resume, 정상 Commit과 teardown이 양쪽 Peer의 Trail
  이력과 표시를 정리하고 다음 session에 stale point를 남기지 않는다.
- 집중 테스트, 기존 Trail 회귀, 프로젝트 compile, `git diff --check`가 통과하고
  Scene·Prefab·설정·Shader·asmdef·Package diff가 없다.

## 실제 변경

- `TerritoryAppendOnlyBlockList<T>`를 추가해 장기 sample, fragment, owner prediction과
  renderer pool을 256개 단위 block에 append하고 기존 point payload를 큰 새 배열로
  복사하지 않게 했다. Clear 뒤 할당 block을 재사용한다.
- `TerritoryTrailSession`의 sample, fragment와 열린 fragment 저장을 block list로
  바꿨다. 같은 Chunk에서도 256 point에 도달하면 마지막 point를 다음 fragment의 첫
  point로 exact 공유하며 분할한다.
- `TerritoryTrailFragment`가 2..256 point만 수용하도록 hard limit을 추가했다.
- `TerritoryTrailPacketizer`가 packet을 꺼낼 때 `RemoveRange`로 남은 backlog 전체를
  이동하지 않고 read cursor만 전진하도록 바꿨다. wire payload와 최대 24 sample 계약은
  변경하지 않았다.
- local owner prediction, State Authority shadow 비교와 segmented renderer pool의
  append storage를 같은 block list로 연결했다.
- block 재사용, same-Chunk 100,000 point exact round-trip/fragment 상한과 100,000
  pending sample packet 상한을 검증하는 테스트를 추가했다.
- C012 계약과 현재 milestone, handoff, roadmap, test matrix를 Trail 전용 목표로 갱신하고
  영역 확장 후속 milestone을 paused 처리했다.

## 검증 결과

- 신규 source를 명시한 `ProjectIO.Territory.ChunkDomain.Tests.csproj` compile은 오류 0,
  Unity generated reference warning 4개로 통과했다.
- parameterless NUnit assertion 직접 실행 74/74 통과.
- 같은 Chunk의 100,000 point를 최대 256 point fragment로 나눈 뒤 재조립 결과가 입력과
  point별로 같고 모든 인접 endpoint가 일치했다.
- 100,000 pending sample이 최대 24 sample packet으로 연속 sequence를 유지했다.
- Unity batch import는 licensing 재연결과 `com.unity.editor.headless` 오류로 완료하지
  못했다. 이후 전체 CLI build도 생성 Fusion UI reference가 불완전해 실패했으며, 이는
  변경 Domain compile 오류와 분리해 기록했다.
- Unity import/Console compile, Territory EditMode와 실제 Host·Client runtime/Profiler는
  작업자 검증 항목이었다. 작업자는 Client Input Authority Runner 실제 플레이에서
  선 연속성과 좋은 체감을 확인했다. Host-local 장기 실행과 Profiler 수치 검증은
  수행하지 않았다.

## 남은 위험

정밀 전체 경로를 삭제하지 않는 요구 때문에 총 메모리는 point 수에 비례해 증가한다.
이번 작업은 그 데이터를 블록 단위로 보관해 큰 재할당과 frame별 전체 작업을 없애는
것이지, 유한한 메모리에서 문자 그대로 무한한 경로를 저장한다고 약속하지 않는다.

LineRenderer 하나는 256 point를 넘지 않지만 활성 renderer의 총수는 경로 길이에 따라
증가한다. 실제 장기 runtime에서 draw/culling 비용이 병목으로 확인될 때만 exact storage와
분리된 presentation virtualization을 별도 최소 작업으로 다룬다. suspension reconciliation과
Runner Item·Slash의 요청 시 전체 경로 읽기도 이동 hot path 밖에 유지했으며, 실제 spike가
재현되면 해당 경계만 별도 측정한다.
