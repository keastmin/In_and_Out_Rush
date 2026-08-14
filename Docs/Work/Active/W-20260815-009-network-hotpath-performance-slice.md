# W-20260815-009 Network hot-path performance slice

Status: Reserved

## Synchronization baseline

- Base Commit: d756c2a28339b7378b238f2e53b4bc932166e01c
- Upstream: origin/rebuild-development-environment

## Feature

Territory, Monster and Projectile, Stage initialization, and shared Fusion AOI.

## Objective

Reduce the measured authoritative CPU spikes in world-monster streaming,
per-monster simulation, territory expansion, and player AOI updates while
preserving the current game rules and network ownership. Extract only the
smallest performance-focused domain logic needed by this slice; the legacy
polygon territory path remains authoritative.

## Read documents and skills

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/Features/MonstersAndProjectiles.md`
- `Docs/Work/Completed/W-20260815-007-host-peer-territory-performance-refactor.md`
- `Docs/Work/Completed/W-20260815-008-fixed-update-profiler-markers.md`
- `manage-feature-work`
- `build-chunk-territory`
- `photon-fusion-feature`

## Expected code changes

- `Assets/02_Scripts/System/WorldMonsterSpawnSystem.cs`
- `Assets/02_Scripts/Monster/Monster.cs`
- `Assets/02_Scripts/System/TerritorySystem.cs`
- `Assets/02_Scripts/Stage/Network/StageBootstrapper.cs`
- New focused performance/domain types only under
  `Assets/02_Scripts/Territory Refactor/` or
  `Assets/02_Scripts/Features/Monster/` if direct extraction is required.
- `Docs/Features/Territory.md` and `Docs/Features/MonstersAndProjectiles.md`
  only if an entry point, responsibility, contract, or verification record changes.

## Reserved Scene, Prefab, and data assets

None. No Scene, Prefab, ScriptableObject, or serialized-field migration is
authorized in this slice.

## Shared contracts and initialization seams

- Keep `TerritorySystem` as the serialized facade and retain the existing
  `PlayerRunner.OnPositionChanged -> TerritorySystem` seam.
- State Authority remains the sole writer for territory expansion, monster
  spawn/despawn decisions, and AOI assignment. RPCs must not become persistent
  state storage.
- Preserve `OnTerritoryExpandedEvent`, `TryGetCurrentExpansionPath`, current
  monster spawn behavior, and the current AOI radius/visibility outcome.
- Fusion proxy, Late Join, and AOI behavior must remain compatible; no Networked
  property, RPC shape, NetworkObject ownership, Scene, or Bootstrapper wiring
  change is allowed without a new published reservation.

## Other active-work conflicts

No conflict found. `Docs/Work/Active/` contains only its README.

## Boundaries

- Optimize only the four user-profiled paths: world-monster streaming refresh,
  monster `FixedUpdateNetwork`, terminal territory expansion, and per-player AOI
  updates.
- Prefer bounded work, cached spatial decisions, and cadence-based refreshes;
  do not alter gameplay thresholds or the visible territory/monster/AOI result.
- Do not cut over to Chunk Territory or remove the legacy polygon path.
- Do not modify Grid, Fog, Resource, Sacred Zone, player input/combat, scenes,
  prefabs, ScriptableObjects, or third-party Fusion sources.
- Stop and update this reservation before touching any unlisted shared contract
  or serialized asset.

## Completion criteria

- Each changed hot path avoids unbounded repeated work or avoidable allocations
  at its existing cadence, while keeping equivalent gameplay output.
- Territory expansion remains authoritative and fires one consumer event; Host,
  Client, and Late Join restoration preserve the existing replicated territory
  result.
- World monsters preserve spawn/despawn, obstacle avoidance, safe-zone, and
  authority behavior; individual monster simulation still handles stun and
  movement correctly.
- AOI preserves the current active-player coverage and radius while avoiding
  redundant assignment work.
- Focused compile/test or repeatable Host/Client checks, `git diff --check`,
  profiler-marker comparison instructions, actual file list, rollback, and
  remaining risks are recorded before final handoff.

## Rollback

Keep the serialized facades and legacy authority paths. Revert only this slice's
code changes to restore the prior per-tick and per-refresh behavior without a
Scene or Prefab migration.

## Actual changes

Pending published reservation and implementation.

## Verification results

Pending implementation.

## Remaining risks

Profiler percentages are inclusive and may contain Fusion, physics, rendering,
or consumer work. The implementation must retain nested markers or add only
focused markers when needed to distinguish the direct cause.
