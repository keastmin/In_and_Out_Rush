# Handoff

## Completed outcome

Input Authority owner의 fixed Trail prediction, State Authority confirmed sample stream,
Proxy confirmed path와 latest-wins live head를 연결했다. 확정 sample은 최대 24개씩
Reliable 전송되고 수신자가 동일 traversal로 Chunk fragment를 재구성한다.
Renderer는 Chunk 전환과 256 point page에서 segment를 나눠 한 LineRenderer의
무제한 성장을 피한다.

Legacy Territory polygon, 확장 판정, 자기 교차, Kill, Lifeline, vertex sync와
consumer callback은 계속 authoritative하다.

## Changed files

- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailShadowRecorder.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryTrailReplicationStream.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailPacket.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailPacketizer.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailReceiver.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailPacketizerTests.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain.Tests/TerritoryTrailReceiverTests.cs`
- `Assets/02_Scripts/Territory Refactor/Trail/TerritoryTrailChunkRenderer.cs`
- `Docs/Features/PlayerRunner.md`, `Docs/Features/Territory.md`
- `Docs/LongRunning/Territory/`의 W-012 milestone/contract/handoff/test 문서
- `Docs/Work/Completed/W-20260821-012-chunk-territory-predicted-confirmed-trail.md`

## Decisions used

- `CONTRACTS.md` C001-C005
- lifecycle/confirmed payload는 Reliable
- live head는 Unreliable latest-wins presentation 전용
- owner prediction은 결과를 결정하지 않는 local-only presentation

## Verification evidence

- ChunkDomain runtime/test project compile 0 errors
- Unity 6000.0.69f1 EditMode 25/25 통과: 신규 7개와 기존 회귀
- 최대 24 sample packet의 24/24/2 분할, wire round-trip, packet/sample gap,
  Abort stale payload, 다중 Chunk 재구성 검증
- 최종 Unity 6000.0.69f1 compile과 Fusion IL post-process 0 errors
- Client 달리기 Trail과 정상 영역 확장은 작업자 확인
- Client 걷기 Trail은 마지막 표시 point 기준으로 이동 거리를 누적하도록 회귀
  수정했으며 걷기와 걷기/달리기 전환 재확인 통과
- 수정 후 `dotnet build ProjectIO.slnx` 0 errors, 기존 warning 25개
- 작업자가 안내된 Host 로컬/Client Input Authority 정상·장거리 Trail,
  자기 교차/Lifeline, SandTomb pause/resume 런타임 절차 완료를 보고

## Serialized or manual setup

Scene·Prefab·Inspector 연결 변경은 없다. Unity를 열어 import와 compile이 끝난 뒤
다음 절차를 수행한다.

1. Host가 Runner Input Authority인 세션에서 영역을 벗어나 여러 Chunk를 지나고,
   같은 Chunk 안에서도 길게 방향을 바꾼 뒤 재진입한다. 선이 Runner와 즉시 붙어
   움직이고 중복되지 않으며 재진입 때 한 번 지워지고 기존 영역 결과가 나오는지
   확인한다.
2. Client가 Runner Input Authority인 세션에서 같은 절차를 수행한다. Client의
   자기 선은 왕복 지연 없이 붙어야 하고 Host에서는 확정 선과 최신 머리점이
   끊김 없이 이어져야 한다. 영역 밖에서 천천히 걷기만 해도 선이 일정 간격으로
   계속 추가되는지 먼저 확인하고, 걷기와 달리기를 번갈아도 빈 구간이 없어야 한다.
3. Host/Client Runner 각각 자기 선 교차와 Lifeline을 실행한다. 모든 peer의 선이
   지워지고 다음 정상 session에 이전 정점이 섞이지 않는지 확인한다.
4. 영역 밖에서 SandTomb에 잡혔다가 풀려난다. pause 중 강제 이동을 잇는 긴
   사선이 생기지 않고 resume 지점부터 이어지는지 확인한다.
5. 가능하면 Fusion network simulation으로 지연을 추가한다. owner 선은 즉시
   유지되고 observer만 지연된 확정/live 표현을 보며 최종 path와 결과는 같은지
   확인한다.
6. Console에 `confirmed Trail ... failed/rejected`가 없어야 하며 Host 로컬에서
   서버/owner 역할 때문에 같은 선이 두 겹으로 생성되면 안 된다.

진행 중 Trail 상태로 Late Join한 peer가 그 선을 복원하지 않는 것은 이번 범위의
의도된 제한이다.

## Known risks and failures

- 영역 확장 hitch와 단일 polygon 모양 문제는 milestone 4 이후 대상이다.
- 진행 중 Trail Late Join snapshot은 milestone 5 대상이다.

## Remaining legacy consumers

모든 Territory consumer와 authoritative expansion. 이번에는 Trail presentation
transport만 전환했고 기존 Legacy path RPC 메서드는 rollback용 비활성 fallback으로
남아 있다.

## Next bounded milestone

W-012가 Complete됐으므로 revisioned Empty/Full/Boundary Chunk Territory state와
expansion commit을 별도 Active reservation으로 설계한다. GPU와 consumer
migration은 포함하지 않는다.

## Exact next starting files

- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailSession.cs`
- `Assets/02_Scripts/Territory Refactor/ChunkDomain/TerritoryTrailFragment.cs`
- 신규 Chunk Territory state/fill 순수 도메인 파일
- `Assets/02_Scripts/Territory/TerritorySystem.cs`
- `Docs/LongRunning/Territory/CONTRACTS.md`
