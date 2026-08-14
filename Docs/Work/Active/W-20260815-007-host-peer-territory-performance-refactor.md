# W-20260815-007 Host Peer Territory Performance Refactor

Status: Reserved

## Synchronization baseline

- Base Commit: 1049966e9bfe3129b758a1a9f9c9296f965be321
- Upstream: origin/rebuild-development-environment

## Scope

Profile and remove the highest-impact sustained host-peer costs that emerge as
territory and monster populations grow. The work covers the authoritative
territory-expansion path, proxy trail presentation, and monster spawning and
simulation pressure. It is an incremental domain-oriented refactor; the legacy
polygon territory path remains authoritative unless a separately approved
Chunk Territory cutover changes that decision.

## Objective

- Reproduce and measure the host hitching for both Builder and Runner play as
  the match progresses, separating territory cost from monster cost.
- Prevent an overloaded Host from causing an unbounded delay between a
  client-owned Runner and its locally presented expansion trail.
- Move performance-critical territory/trail and monster-spawn responsibilities
  behind focused domain, Fusion-adapter, and Unity-presentation boundaries so
  they can be measured and optimized without changing gameplay ownership.
- Keep Fusion authority, existing serialized scene entry points, current
  Territory consumer events, and the established Host/client gameplay behavior
  compatible.

## Read documents and skills

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/Territory.md`
- `Docs/Features/MonstersAndProjectiles.md`
- `Docs/Features/PlayerRunner.md`
- `Docs/Work/Completed/W-20260814-006-runner-territory-expansion-domain-refactor.md`
- `manage-feature-work`
- `build-chunk-territory`
- `photon-fusion-feature` when replication, authority, or NetworkObject
  lifecycle is changed

## Expected code changes

- `Assets/02_Scripts/System/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Domain/TerritoryExpansionSession.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryExpansionReplication.cs`
- `Assets/02_Scripts/Territory Refactor/Trail/TerritoryTrailSegmentIndex.cs`
- `Assets/02_Scripts/Territory Refactor/Trail/TerritoryTrailChunkRenderer.cs`
- New, focused types only under `Assets/02_Scripts/Territory Refactor/` for
  territory performance domain state, Fusion replication, or Unity trail
  presentation.
- `Assets/02_Scripts/System/WorldMonsterSpawnSystem.cs`
- `Assets/02_Scripts/System/TrackMonsterSpawnSystem.cs`
- `Assets/02_Scripts/Monster/Monster.cs`
- New, focused monster performance types only under
  `Assets/02_Scripts/Features/Monster/` if a domain boundary is required.
- `Docs/Features/Territory.md` and `Docs/Features/MonstersAndProjectiles.md`
  only when a measured responsibility, entry point, setup, or verification
  contract changes.

## Reserved Scene, Prefab, and data assets

None. `GameWorld.unity`, `GamePresentation.unity`, `GameRoot.unity`,
`Core.prefab`, monster prefabs, and ScriptableObjects are not in this initial
reservation. Any need to change them requires a reservation update and a new
published verification before the change.

## Shared contracts and initialization seams

- Preserve the serialized `TerritorySystem` facade and the
  `StageBootstrapper -> PlayerRunner.OnPositionChanged -> TerritorySystem`
  integration seam. `StageBootstrapper` is not reserved.
- Preserve `TerritorySystem.OnTerritoryExpandedEvent`, `Territory`,
  `TerritoryVisible`, and `TryGetCurrentExpansionPath` consumer contracts.
- State Authority remains the only writer of territory state, polygon mutation,
  expansion outcome, spawning decisions, and consumer side effects. Input
  Authority and proxies may supply input or render replicated state only.
- Do not use RPC traffic as persistent authoritative territory state. Any
  revision to replicated data, Late Join restoration, Spawn/Despawn policy, or
  NetworkObject ownership requires the Fusion skill and an explicit update to
  this reservation before implementation.

## Other active-work conflicts

No conflict found when this reservation was created. `Docs/Work/Active/`
contained only its README.

## Boundaries

- Do not cut over to the Chunk Territory pipeline or remove the legacy polygon
  authority path.
- Do not modify Bootstrapper wiring, scenes, prefabs, ScriptableObjects,
  player movement/combat/input, Grid, Fog, Resource, Sacred Zone, or other
  Territory consumers without first expanding this reservation.
- Do not make speculative optimization changes before collecting a repeatable
  Host and client performance baseline that distinguishes territory, trail,
  network replication, spawning, and monster simulation costs.
- Bound the implementation to the first measured bottleneck or a small set of
  directly coupled bottlenecks; record later findings as a follow-up milestone.

## Acceptance criteria

- A repeatable Host/Client profiling scenario covers Host-as-Builder,
  Host-as-Runner, and client-owned Runner expansion after territory and monster
  counts have grown.
- Evidence identifies the dominant sustained Host cost and the main cause of
  delayed proxy trail presentation before the selected optimization is made.
- The selected optimization has a measurable before/after result, avoids
  per-frame allocation or unbounded work in the changed hot path, and preserves
  territory expansion and monster gameplay behavior.
- Host and client observe a single authoritative expansion; proxy trail
  presentation remains ordered and bounded without generating duplicate
  territory events, spawns, or lifeline outcomes.
- Any extracted type has a single domain/Fusion/Unity responsibility and does
  not introduce a new singleton, global search, or service-locator dependency.
- Focused tests or repeatable Host/client checks, project compilation,
  `git diff --check`, and worktree status are recorded before completion.

## Rollback

Keep `TerritorySystem` as the serialized facade and retain the legacy polygon
territory path as the authority. Each refactor step must be independently
revertible without scene or prefab migration. If measured improvements are not
reliable, retain the baseline evidence and revert only the bounded optimization
slice.

## Actual changes

Pending implementation after this reservation is reviewed, committed, pushed,
and verified on the upstream branch.

## Verification results

- `CheckStart` passed: branch and `origin/rebuild-development-environment`
  were synchronized at `1049966e9bfe3129b758a1a9f9c9296f965be321`.
- Active-reservation scan found no existing feature reservation to overlap.
- Runtime profiling and Host/client verification are pending implementation.

## Remaining risks

- The reported hitching may be dominated by more than one subsystem; territory
  geometry, trail replication/rendering, spawning, and live monster simulation
  must be isolated before assigning ownership to a fix.
- Current expansion vertex delivery uses RPCs. Improving it could affect Late
  Join behavior and needs an explicit contract update before any replication
  change.
