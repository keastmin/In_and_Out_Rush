# W-20260814-003 Random network obstacles

Status: Completed

## Synchronization baseline

- Base Commit: 8dfcdecbb67b14402070f0ec49ec5aeeb1529ac9
- Upstream: origin/rebuild-development-environment

## Scope

Grid and Obstacles

## Goal

Replace the visible grid-pattern obstacle placement with scattered random placement outside the configured central exclusion radius. Keep obstacle lifecycle authoritative on the host and replicated to clients through Fusion NetworkObject spawning.

## Read documents and skills

- Docs/PROJECT_MAP.md
- Docs/Features/GridAndObstacles.md
- manage-feature-work
- photon-fusion-feature

## Expected code changes

- Assets/02_Scripts/Grid/InfiniteGridObstacleSpawner.cs

## Reserved Scene, Prefab, and data assets

None. Existing serialized distribution settings remain compatible.

## Shared contracts or Bootstrapper changes

None. The existing StageBootstrapper host-only call and Fusion NetworkObject Spawn flow are retained.

## Overlap with other active work

None.

## Scope boundaries

- No Scene, Prefab, or obstacle asset changes.
- No changes to obstacle despawn behavior, Territory, Track, Resource Spawn, or StageBootstrapper.
- Do not introduce client-side spawning or RPC-only persistence.

## Completion criteria

- Every obstacle position is outside the configured center exclusion radius.
- Placement samples the usable world area randomly and enforces configured minimum spacing.
- Only State Authority spawns obstacles with Fusion; connected clients receive the same NetworkObjects.
- Focused checks and git diff validation are recorded.

## Actual changes

- Replaced row-and-column cell iteration with direct random samples across the usable ground bounds.
- Preserved the central exclusion-radius and minimum-spacing checks.
- Retained State Authority-only spawning through `NetworkRunner.Spawn`; no client spawning path was added.
- Kept the legacy cell-jitter serialized field for existing scene compatibility and marked it as unused by random placement.

## Verification results

- `dotnet build Assembly-CSharp.csproj --no-restore`: passed with 0 errors and 16 warnings. The warnings are existing project warnings plus the retained legacy cell-jitter field being unused.
- `git diff --check`: passed.
- Host and Client runtime session: not run in this environment. The code path was inspected: `StageBootstrapper` and `InfiniteGridObstacleSpawner` gate spawning on State Authority, then Fusion replicates each spawned `NetworkObject` to clients.

## Remaining risks

- Run a Host and Client session in Unity to visually confirm scattered placement, central exclusion, client replication, and late-join visibility with the scene's configured spawn count and spacing.
