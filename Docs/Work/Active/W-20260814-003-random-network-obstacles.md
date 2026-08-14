# W-20260814-003 Random Network Obstacles

Status: Awaiting Approval

## Automation Status

- Base Commit: fee8547af1f026b7cfbb04d1bc6b424f115456a2
- Shared Upstream: origin/rebuild-development-environment
- User Approval: Pending
- Reservation Published: No
- Implementation Start Commit:

## Scope

Grid and obstacle generation.

## Goal

Replace the regular cell-based rock placement with random samples across the usable map area, while excluding the configured center radius and preserving minimum spacing. Keep the host as the sole spawn authority so Fusion replicates the spawned NetworkObjects, including their selected positions and rotations, to clients and late joiners.

## Read Documents and Skills

- `Docs/Features/GridAndObstacles.md`
- `manage-feature-work`
- `photon-fusion-feature`

## Expected Code Changes

- `Assets/02_Scripts/Grid/InfiniteGridObstacleSpawner.cs`

## Reserved Scene, Prefab, and Data Assets

None. Existing serialized distribution settings on `Assets/03_Prefabs/Grid/Infinite Grid Ground.prefab` remain unchanged.

## Shared Contracts or Bootstrapper Changes

None. `StageBootstrapper` already invokes `SpawnObstacles` only with State Authority, and `NetworkRunner.Spawn` is the persistent replication source for clients and late joiners.

## Overlap with Other Active Work

No Active work files reserve the obstacle spawner, its prefab, or the Stage bootstrapper.

## Scope Exclusions

- Do not change obstacle prefab registration, AOI configuration, or spawn counts.
- Do not modify track/territory obstacle despawning.
- Do not modify Scene or Prefab serialization.

## Acceptance Criteria

- Spawn positions are sampled randomly rather than one per regular grid cell.
- No obstacle is inside `_centerExclusionRadius` of the ground center.
- Spawned obstacles honor `_minimumSpacing` when a valid position is found.
- Only State Authority creates obstacles using `NetworkRunner.Spawn`; clients observe the same spawned NetworkObjects.
- Existing despawn behavior remains State Authority-only.

## Actual Changes

Pending approval.

## Verification Results

Pending implementation.

## Remaining Risks

Host/client and late-join behavior requires a manual multi-peer Fusion run after compilation.
