# Track and Rounds

Status: Current

Last reviewed: 2026-09-10

## Track transformation contract

- `Track` owns one or more `TrackPath` values and exposes only explicit `TrackSegment` values to spatial consumers. Closed paths add their final-to-first segment; open paths never do, and separate paths are never connected implicitly.
- Stage 1 is the existing deterministic noisy ellipse (16m horizontal radius, 8m vertical radius, 30 vertices, 0.1m per-axis noise). Stage 2 replaces it with one open line centered on the runtime map center. Its length is one eighth of the map diameter, and its axis is chosen uniformly from horizontal, vertical, and the two diagonals; either endpoint can be the start. Stage 3 preserves that line and adds an equal-length perpendicular line with an independently reversed start direction.
- `TrackSystem.ExpandTrack()` is a State Authority-only stage transition. The authority replicates ellipse seed, stage, primary axis, both reversal flags, center, line length, and revision. Every peer reconstructs the same local `Track` when the revision changes, including Late Join peers. Vertex RPC synchronization is no longer part of the active path.
- `OnTrackChanged` delivers the completed `Track`. `TrackVisible` renders at most two paths and loops only closed paths. Grid, tower blocking, and obstacle cleanup consume `Track.Segments`, so open endpoints and the two Stage 3 paths do not create phantom connections.

## Round ordering

- Round-end settlement stops pending track-monster spawn routines and settles every live track monster on State Authority in spawn order.
- After settlement finishes, rounds 3 and 7 transition to Stages 2 and 3 respectively. Round 9 applies the permanent track-monster movement multiplier once. If the next round starts while settlement is active, its track spawn and queued internalized spawn are deferred until settlement and the post-round change finish.
- The existing strengthening schedule remains rounds 5, 8, and every round from 11 onward.

## Rollback and verification

Rollback restores the prior single `Track.Vertices` loop, vertex RPCs, and immediate round-end expansion together; partial rollback is unsafe because all segment consumers and track-monster traversal now use the multi-path contract. Pure EditMode coverage lives in `ProjectIO.Tracks.Tests`. Host/Client stage parity, authority rejection, Late Join restoration, teleport replication, despawn, and single Runner damage still require a two-peer runtime pass whenever automated Fusion execution is unavailable.

## 책임

트랙의 상태와 확장, 라운드 시간, 웨이브 시작·종료와 버서크 시점을 관리한다.

## 주요 진입점

- `Assets/02_Scripts/System/TrackSystem.cs`
- `Assets/02_Scripts/Time/Network/TimeSystem.cs`
- `Assets/02_Scripts/Features/MatchProgression/Logic/RoundProgressionTransitionPolicy.cs`
- `Track`, `TrackVisible`, `RangeIndexMapper`
- `StageBootstrapper.YOU`의 라운드 이벤트 연결

`RoundProgressionTransitionPolicy`는 현재 Phase와 Phase 경과 시간이 duration 경계를 넘었는지만 순수하게 판정한다. Host 권위 검사, Networked 상태 변경, Round·Berserk 갱신과 기존 이벤트 발생은 `TimeSystem`이 계속 소유한다.

## 주요 소비자

Track Monster Spawn, Grid 차단 셀, Tower 정리, Stage UI timer, Resource·Obstacle 정리.

## 관련 Asset

- `GameWorld.unity`의 Track과 TimeSystem
- Track monster wave table

## 변경 시 확인

- Host만 라운드와 Track 상태를 변경하는지
- Client timer와 Track 표시가 복원되는지
- 이벤트 중복 구독과 Scene unload 정리
- Track 확장 시 Grid·Obstacle·Tower 소비자가 한 번씩 반응하는지

## 기술 부채

라운드 규칙 일부가 StageBootstrapper에 있어 Time·Track 도메인과 Stage 조립 책임의 경계가 불명확하다.
