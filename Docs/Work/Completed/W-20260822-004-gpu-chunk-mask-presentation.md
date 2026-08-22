# W-20260822-004 GPU Chunk Mask/SDF 표시

Status: Superseded

## 동기화 기준

- Base Commit: 7aed9f4d9c4045f724e143ca836463255141c1f0
- 공용 Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Territory Chunk presentation, GPU mask/SDF와 CPU fallback, 고정 2인 Host·Client 표시
동등성.

## 목표

C006/C007의 revisioned sparse Chunk snapshot을 실제 표시용 page table과 Boundary
signed-distance tile로 변환한다. Compute Shader가 가능한 환경에서는 changed-Chunk만
GPU로 갱신하고, Compute Shader가 없거나 지원되지 않으면 같은 pixel-center 규칙의
CPU rasterizer로 같은 표시 texture를 만든다.

Chunk presentation이 해당 revision을 완전히 준비한 뒤에만 Legacy polygon
MeshRenderer를 숨기며, 자원 생성·용량·Shader·update 실패 시 즉시 Legacy mesh를
유지하거나 복원한다. 이는 표시 전환일 뿐이며 영역 판정, 확장, vertex RPC와 모든
consumer에는 계속 Legacy polygon이 유일한 gameplay authority다.

## 읽을 문서와 Skill

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
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

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory/TerritoryVisible.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryChunkReplicationStream.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkReplica.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkMaskLayout.cs` 신규 및 `.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryChunkMaskTileRasterizer.cs` 신규 및 `.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkReplicaTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkMaskLayoutTests.cs` 신규 및 `.meta`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryChunkMaskTileRasterizerTests.cs` 신규 및 `.meta`
- `Assets/02_Scripts/Territory Refactor/Presentation.meta` 신규
- `Assets/02_Scripts/Territory Refactor/Presentation/TerritoryChunkMaskPresenter.cs` 신규 및 `.meta`
- `Assets/Resources/Territory.meta` 신규
- `Assets/Resources/Territory/TerritoryChunkMask.compute` 신규 및 `.meta`
- `Assets/Resources/Territory/TerritoryChunkMask.shader` 신규 및 `.meta`
- `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/CURRENT_MILESTONE.md`
- `Docs/LongRunning/Territory/HANDOFF.md`
- `Docs/LongRunning/Territory/CONTRACTS.md`
- `Docs/LongRunning/Territory/ROADMAP.md`
- `Docs/LongRunning/Territory/TEST_MATRIX.md`
- 이 작업 문서

## 예약 Scene·Prefab·Data Asset

없음. Scene, Prefab, 기존 Material, ScriptableObject, asmdef, Package, URP/Fusion 설정과
Inspector 참조를 변경하지 않는다. Compute/표시 Shader는 `Resources/Territory`에서
런타임 로드하고 표시용 child, mesh, material과 texture는 코드가 생성·소각한다.

## 공용 계약 또는 Bootstrapper 변경

- C001-C007은 변경하지 않는다. C008 GPU/CPU Chunk presentation 계약을 먼저
  `Approved`로 추가한다.
- page table은 한 texel당 한 Chunk이며 `Empty`, `Full`, `Boundary tile index`만
  저장한다. Boundary는 fixed local segment와 `CenterInside`로 32×32 pixel-center의
  signed distance를 계산한다. 이 해상도는 표시 전용이고 C006 좌표나 모양 데이터를
  단순화·이동·삭제하지 않는다.
- page table bounds는 음수 Chunk를 포함하고 overflow 없이 page 단위로 확장한다.
  layout이 유지되면 changed-Chunk만 갱신하고, bounds/Boundary capacity가 바뀌면 현재
  immutable snapshot으로 전체 표시 자원을 다시 만든다.
- `TerritoryChunkReplica`는 검증 완료된 delta의 changed coverage를 snapshot과 함께
  presentation seam에 제공한다. RPC payload, 순서, packet budget과 C007 실패 원자성은
  변경하지 않는다.
- Chunk revision의 GPU/CPU 자원이 완전히 준비된 경우에만 Legacy MeshRenderer에서
  Chunk renderer로 표시를 전환한다. 어느 단계든 실패하면 Legacy mesh를 유지한다.
- Bootstrapper와 public consumer callback은 변경하지 않는다.

## 네트워크·Peer 동등성

- Input Authority의 Trail prediction과 State Authority의 확장 판정·Commit 흐름은
  변경하지 않는다. State Authority C006 store가 계속 Chunk 원본이고 Client는 기존
  C007 delta를 검증·적용한다.
- Host는 정상 Commit의 `ChangedChunks`, Client는 replica가 terminal 검증 후 공개한
  동일 changed coverage를 presentation에 전달한다. Host가 State/Input Authority를
  함께 가져도 Legacy mesh와 Chunk renderer를 동시에 표시하거나 Commit을 두 번
  실행하지 않는다.
- Chunk renderer는 로컬 presentation이며 GPU 지원 여부에 따라 각 Peer가 GPU 또는
  CPU backend를 독립 선택할 수 있다. backend가 달라도 같은 revision, page state와
  pixel-center mask를 표시해야 하며, 실패한 Peer만 Legacy mesh fallback을 유지해
  gameplay 결과를 바꾸지 않는다.
- Additive 준비가 늦으면 snapshot을 보관했다가 `TerritoryVisible` 준비 후 한 번
  적용한다. Stage 종료, 한 Peer 이탈, TearDown/Dispose에서는 ComputeBuffer,
  RenderTexture/Texture2DArray, runtime material/mesh와 pending revision을 소각한다.
- Late Join·재접속은 고정 2인 제품 계약대로 범위 밖이다.
- Host Runner와 Client Runner 각각 초기 영역, 걷기·달리기 중 Trail, 긴 경로 정상
  확장, 연속 확장, Abort를 실행해 양쪽 영역 모양과 revision 전환이 같고 frame spike가
  증가하지 않는지 확인한다. 한 Peer는 강제 CPU backend로 실행해 GPU/CPU 결과와
  fallback을 비교한다.

## 다른 활성 작업과 겹치는 부분

`CheckStart`와 `Docs/Work/Active/` 확인 결과 안내용 README 외 활성 예약이 없어
겹침이 없다.

## 범위 밖

- Chunk 기반 authoritative expansion/union과 Legacy 256 vertex 보정 제거
- containment, Grid, Fog, Resource, Monster 등 gameplay consumer migration
- Trail transport·sampling·renderer 재설계
- Late Join, reconnect, AOI recovery와 다수 Peer 보안 확장
- 기존 Territory Material, Scene, Prefab, Bootstrapper와 직렬화 참조 변경
- Legacy polygon/mesh 생성 코드 삭제와 최종 serialized cutover

## 완료 조건

- C008이 Approved이고 page table/tile layout, fixed-to-pixel 규칙, GPU/CPU parity,
  용량 실패와 Legacy rollback이 문서화된다.
- 1000×1000 및 world boundary 지름 2000 조건에서 page table은 Chunk 단위 크기를
  유지하고 Boundary tile만 texture array slice를 소비한다. texture/array 한도 초과는
  예외나 부분 표시 대신 Legacy fallback을 선택한다.
- Full/Empty/Boundary, 음수 Chunk, Chunk 경계, center-inside true/false와 방향성
  segment를 CPU rasterizer 테스트로 검증한다. GPU readback은 같은 pixel-center의
  sign과 허용 거리 오차 안에서 CPU 결과와 일치한다.
- Host initial/changed snapshot과 Client replica delta가 같은 revision을 presenter에
  한 번만 적용한다. empty delta는 재-raster 없이 revision만 전진한다.
- layout이 유지되는 확장은 changed-Chunk만 갱신하고, layout 확장 시 atomic full
  rebuild 후 표시를 교체한다. build/update 실패 중에는 이전 Chunk 표시 또는 Legacy
  mesh가 유지되며 반쪽 revision을 공개하지 않는다.
- Compute Shader 미지원/강제 비활성 Peer에서 CPU backend가 같은 모양을 표시하고,
  둘 다 실패하면 Legacy mesh로 자동 복귀한다.
- Host와 Client 각각 초기 영역, 긴 경로 정상 확장, 연속 확장, Abort에서 영역 모양,
  Trail, consumer 결과와 revision이 동일하다. Chunk renderer와 Legacy renderer가
  동시에 겹쳐 표시되지 않는다.
- Unity Profiler에서 full rebuild, incremental update, GPU dispatch/CPU fallback과
  확장 frame을 기록한다. GPU 작업은 readback 대기로 main thread를 동기 block하지
  않는다.
- 집중 테스트, project compile/Fusion Weaver, shader compile, `git diff --check`가
  통과하고 Scene·Prefab·기존 Material·설정·asmdef diff가 없다.

## 실제 변경

- GPU/CPU 32×32 Boundary SDF prototype을 로컬에서 구현했지만 runtime 검증 실패로
  채택하지 않았다. 구현 코드와 Shader, 테스트, C008 계약 및 장기 문서 변경은
  commit하지 않고 모두 `906f79d` 기준으로 되돌렸다.
- 기존에 Push된 이 예약 문서만 실패 증거와 폐기 이유를 보존하기 위해 Completed로
  이동한다. Scene·Prefab·기존 Material·Inspector 참조는 변경하지 않았다.
- 다음 작업은 GPU 사용 자체가 아니라 정확한 이동 경로 모양, 변경 범위에 비례하는
  계산 비용, Host·Client 체감 동등성과 main-thread frame 안정성을 완료 기준으로
  새로 예약한다.

## 검증 결과

- 작업자 runtime에서 Trail과 Legacy authoritative 확장은 동작했지만 시작 영역과
  확장 영역 외곽이 8×8 Chunk 사각형 단위로 표시됐다.
- D3D11 Editor 로그에서 `TerritoryChunkMask.compute`의 `point`, `end` 식별자가 HLSL
  예약 토큰으로 해석되어 `RasterizeBoundary` kernel compile이 실패한 것을 확인했다.
  `Dispatch`는 반복 `Kernel at index (0) is invalid` 오류를 냈고 GPU tile이 작성되지
  않아 CPU 비교에서 sign mismatch 986개, 최대 거리 오차 8.0이 발생했다.
- 이 실패가 자동 CPU/Legacy rollback으로 전환되지 않아 C008 실패 원자성과 rollback
  완료 조건도 충족하지 못했다.
- 커널 오류와 별개로 32×32 SDF는 최종 외곽을 근사하므로 러너의 정밀 이동 경로를
  최종 영역 테두리로 유지한다는 제품 목표의 기본 표현 방식으로 부적합하다고
  결정했다.

## 남은 위험

- 저장소는 W-004 시작 전 상태로 복원되므로 Legacy polygon union, vertex 보정과
  synchronous C006 shadow build 비용이 그대로 남는다.
- 다음 설계가 실제 profile 없이 GPU 또는 특정 자료구조를 먼저 선택하면 같은 실패를
  반복할 수 있다. 새 milestone은 모양 오차, 경로 길이, 변경 범위, main-thread 시간과
  Host·Client runtime 기준을 먼저 고정해야 한다.
