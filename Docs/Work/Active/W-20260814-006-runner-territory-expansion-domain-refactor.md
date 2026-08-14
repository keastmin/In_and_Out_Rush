# W-20260814-006 Runner Territory Expansion Domain Refactor

Status: Reserved

## Synchronization baseline

- Base Commit: 069b666ffa28770aca3b8773eaf88665a534c53c
- Upstream: origin/rebuild-development-environment

## Scope

Refactor the currently active player-runner territory-expansion flow out of the
legacy `TerritorySystem` monolith into the Territory domain structure. Preserve
the existing polygon territory as the authoritative path; this is not a Chunk
Territory cutover.

## Objective

- Separate expansion-session domain state and path processing from Fusion RPC
  transport and Unity trail/mesh presentation.
- Keep `TerritorySystem` as the existing serialized scene entry point and
  network facade so current consumers and inspector references continue to
  work.
- Preserve the same expansion, self-crossing/lifeline, territory mesh, and
  territory-expanded event behavior for a Host and a client-owned Runner.

## Read documents and skills

- `AGENTS.md`
- `Docs/PROJECT_MAP.md`
- `Docs/Features/PlayerRunner.md`
- `Docs/Features/Territory.md`
- `manage-feature-work`
- `build-chunk-territory`
- `photon-fusion-feature`

## Expected code changes

- `Assets/02_Scripts/System/TerritorySystem.cs`
- `Assets/02_Scripts/Territory Refactor/Domain/TerritoryExpansionSession.cs`
- `Assets/02_Scripts/Territory Refactor/Domain/TerritoryExpansionStep.cs`
- `Assets/02_Scripts/Territory Refactor/Adapters/Fusion/TerritoryExpansionReplication.cs`
- `Assets/02_Scripts/Player/Player Runner/Network/PlayerRunner.cs` only if
  the existing position-observation boundary needs a minimal, compatible
  authority-safe adjustment.
- `Docs/Features/Territory.md` and `Docs/Features/PlayerRunner.md` only if the
  refactor changes their documented entry point or verification guidance.

## Reserved Scene, Prefab, and data assets

None. Existing `TerritorySystem` serialized fields and `GameWorld` references
must remain compatible; do not modify scene or prefab YAML in this slice.

## Shared contracts and initialization seams

- Preserve `TerritorySystem.OnTerritoryExpandedEvent`, `Territory`,
  `TerritoryVisible`, and `TryGetCurrentExpansionPath` for current consumers.
- Preserve the `StageBootstrapper -> PlayerRunner.OnPositionChanged ->
  TerritorySystem` hookup without modifying the shared bootstrapper unless a
  separately reserved scope update is required.
- The domain session owns only pure expansion/path state. Fusion RPC methods,
  NetworkObject lifecycle, mesh updates, and line-renderer ownership remain in
  the adapter/facade layer.

## Network contract

- State Authority is the sole owner of expansion state, polygon mutation,
  self-crossing/lifeline outcomes, and `OnTerritoryExpandedEvent` side effects.
- Input Authority supplies movement through the existing Runner input path; it
  must not mutate territory state directly.
- State Authority replicates expansion start, path points, reset, stop, and
  completed territory vertices to proxies through the existing reliable RPC
  direction. RPCs are transport, not persistent authoritative storage.
- Host-as-Runner and client-owned Runner must each execute one authoritative
  expansion only. Proxy presentation must not generate a second expansion,
  event, spawn, or lifeline result.
- Late-join behavior will be characterized against the current vertex-sync
  behavior; any change beyond preserving the established contract requires a
  reservation update before implementation.

## Other active-work conflicts

None found. `Docs/Work/Active/` contains only its README.

## Boundaries

- Do not cut over to the Chunk Territory pipeline or remove legacy polygon
  code in this task.
- Do not change Territory consumers (Grid, Resource, Monster, Sacred Zone) or
  `StageBootstrapper` bindings.
- Do not alter Runner movement, combat, input ownership, scene, prefab, or
  ScriptableObject serialization except for an explicitly justified minimal
  compatibility change to the position-observation seam.

## Acceptance criteria

- The extracted domain types have no Fusion, MonoBehaviour, mesh, or
  line-renderer dependency.
- Existing serialized `TerritorySystem` fields and public consumer contracts
  compile and remain connected.
- Host Runner can leave territory, draw a trail, return, expand exactly once,
  update visuals, and notify existing host-only consumers.
- Client-owned Runner produces the same single authoritative expansion on the
  Host; the client receives the matching trail and completed territory visual
  without directly mutating territory or duplicating side effects.
- Self-intersection triggers exactly the current State-Authority lifeline or
  death result, with proxies receiving the reset/presentation state.
- Focused tests or repeatable Host/client checks, project compilation,
  `git diff --check`, and worktree status are recorded before completion.

## Rollback

Retain `TerritorySystem` as the serialized facade and keep the polygon path
authoritative. Reverting this bounded change restores the current monolithic
implementation without scene or prefab migration.

## Actual changes

Pending reservation publication and implementation.

## Verification results

Pending.

## Remaining risks

- Current territory-vertex delivery is RPC-based; a separate approved scope is
  required if runtime characterization shows Late Join needs persistent
  replicated territory state.
- The shared StageBootstrapper seam is intentionally out of scope. If the
  existing Runner position callback is insufficient during refactoring, this
  reservation must be amended before changing it.
