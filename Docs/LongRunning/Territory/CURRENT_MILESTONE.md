# Current Milestone

Status: Complete

## Objective

영역 확장 계산을 제외하고, Runner가 영역 밖에서 매우 오래 이동할 때 exact Trail의
기록·전송·로컬 표시 비용이 누적 point 전체 길이 때문에 커지지 않게 한다.

이번 milestone은 C012 bounded exact long Trail runtime만 소유한다. 기존 Legacy 영역
확장, C006-C011 shadow, mesh, vertex RPC와 consumer는 동작을 바꾸지 않는다.

## Prerequisites

- Active reservation `W-20260823-001-bounded-exact-long-trail`
- implementation base `84d51293fc0dac5d42b3a440289a1aea9874a113`
- `CONTRACTS.md`의 C001-C005와 C012가 Approved일 것

## Read first

- `Docs/Work/Active/W-20260823-001-bounded-exact-long-trail.md`
- `Docs/Features/Territory.md`
- `Docs/Features/PlayerRunner.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`

## Allowed files

- 예약 문서의 exact Trail Domain, Fusion adapter, segmented renderer와 `TerritorySystem`
- 대응 ChunkDomain tests
- Territory 기능·장기 작업 문서

## Reserved Scene·Prefab·Data Asset

없음.

## Prohibited changes

- 영역 확장 polygon, C006-C011 계산과 compact state 의미 변경
- Territory mesh, vertex RPC, Chunk Territory delta와 consumer cutover
- Trail point 단순화, 이동·삭제와 손실 압축
- Runner Item·Slash 전체 경로 공격 판정 최적화
- GPU, Burst, Job System과 CPU/GPU fallback
- Scene, Prefab, Inspector, ScriptableObject, asmdef, Package와 ProjectSettings
- Late Join, reconnect, AOI, 다수 Peer와 보안 확장

## Required behavior

- confirmed sample과 local owner point는 fixed-size block에 append하며 기존 point 배열을
  새 큰 배열로 복사하지 않는다.
- 같은 Chunk에 계속 머물러도 fragment 하나는 최대 256 point이며 다음 fragment와
  exact endpoint 하나를 공유한다.
- 입력 fixed point의 순서와 모양은 fragment 분할 전후가 완전히 같다.
- confirmed packet은 최대 24 sample을 유지하고 pending packet 추출 시 남은 전체
  backlog를 앞으로 이동하지 않는다.
- LineRenderer 하나는 기존 최대 256 point를 유지하고 닫힌 renderer 구간은 append 중
  다시 만들지 않는다.
- Input Authority는 local line을 즉시 표시하고 State Authority만 authoritative exact
  sample/session을 소유한다.

## Acceptance criteria

- 같은 Chunk 안의 100,000 point와 여러 Chunk 경로가 손실 없이 round-trip된다.
- 모든 fragment와 packet이 각각 256 point, 24 sample 상한을 지킨다.
- sample, fragment, pending packet과 owner renderer pool의 append가 이전 전체 point를
  복사하거나 순회하지 않는다.
- 기존 Trail ordering, gap, Abort, Commit, Chunk traversal 회귀가 통과한다.
- Domain/test compile, 직접 NUnit 회귀와 `git diff --check`가 통과한다.
- 작업자가 Unity import/compile, Territory EditMode와 Host-local/Client Runner 장시간
  걷기·달리기·속도 전환·Abort·정상 종료를 검증한다.

## Current evidence

- 신규 block storage와 100,000 point same-Chunk fragment 및 packet stress를 포함한
  parameterless NUnit assertion 직접 실행 74/74 통과.
- 100,000 point를 최대 256 point fragment로 분할한 뒤 재조립 결과가 입력과 point별로
  동일하며 모든 인접 fragment가 exact endpoint를 공유했다.
- 100,000 pending sample이 최대 24 sample packet으로 연속 sequence를 유지했다.
- 신규 source를 명시한 ChunkDomain/tests compile은 error 0, 기존 Unity generated
  reference warning 4개다.
- 전체 Assembly-CSharp CLI build는 Unity batch licensing 실패 뒤 생성 reference가
  불완전해 Fusion UI reference 오류로 중단됐다. 변경 Domain compile 오류는 없으며
  실제 Unity import/compile은 작업자 검증이 필요하다.
- 작업자가 Client Input Authority Runner로 실제 플레이하고 긴 Trail의 선 연속성과
  플레이 체감이 좋음을 확인했다.
- Host-local Runner 장기 실행과 Profiler 수치 검증은 수행하지 않았으며 handoff의
  known risk로 유지한다.

## Rollback

신규 block list와 Trail session/packetizer/renderer 연결 diff를 되돌리면 W-008 이후
상태로 복원된다. Scene, serialized data와 network wire format migration은 없다.

## Out of scope

영역 확장 계산·표시, compact delta, consumer migration, Legacy 제거, GPU/Burst,
Runner Item·Slash, Late Join/reconnect/AOI/security.

## Next bounded milestone

W-001 완료 후 추가 기능을 자동으로 시작하지 않는다. Profiler에서
이번 목표와 직접 관련된 장기 Trail 병목이 재현될 때만 그 측정 지점 하나를 다음 작은
milestone으로 예약한다.
