# W-20260815-008 Network simulation and render profiler markers

Status: Reserved

## Synchronization baseline

- Base Commit: 3bd6c291d67632ceae1341afc18929c6ec3df88b
- Upstream: origin/rebuild-development-environment

## Feature

Monster and Projectile, Tower and Laboratory, Resource Spawn, Stage initialization.

## Objective

Add low-overhead `ProfilerMarker` scopes to selected network simulation and
Fusion `Render()` hot-path candidates so a Player Profiler capture can separate
sustained per-instance simulation cost, periodic global work, and the
`NetworkRunnerRender -> UpdateFunction.Invoke()` contribution.

## Read documents and skills

- `AGENTS.md`
- `Docs/Work/README.md`
- `Docs/PROJECT_MAP.md`
- `manage-feature-work`

## Expected code changes

- `Assets/02_Scripts/Monster/Monster.cs`
- `Assets/02_Scripts/System/WorldMonsterSpawnSystem.cs`
- `Assets/02_Scripts/Resource/Network/ResourceSpawnSystem.cs`
- `Assets/02_Scripts/Stage/Network/StageBootstrapper.cs`
- `Assets/02_Scripts/Tower/Towers/Attack Tower/SentryGunTower.cs`
- `Assets/02_Scripts/Tower/Towers/Attack Tower/MissileTower.cs`
- `Assets/02_Scripts/Tower/Towers/Attack Tower/LaserTower.cs`
- `Assets/02_Scripts/Tower/Towers/Attack Tower/BladeTower.cs`
- `Assets/02_Scripts/Tower/Missile.cs`
- `Assets/02_Scripts/Projectile/MonsterProjectile.cs`
- `Assets/02_Scripts/Player/Player Runner/RunnerProjectile.cs`
- `Assets/02_Scripts/Resource/Network/ResourceZone.cs`
- `Assets/02_Scripts/Tower/Towers/Support Tower/CellBuffSupportTower.cs`
- `Assets/02_Scripts/Player/Player Runner/Network/PlayerRunner.cs`
- `Assets/02_Scripts/Player/Player Runner/Item/BarrierWave.cs`
- `Assets/02_Scripts/Player/Player Runner/Item/IncineratorDrone.cs`
- `Assets/02_Scripts/Player/Player Runner/Ping/PlayerRunnerPingGuide.cs`
- `Assets/02_Scripts/System/TerritorySystem.cs`

## Reserved Scene, Prefab, and data assets

None.

## Shared contracts and initialization seams

- No networked state, RPC, authority, spawn/despawn policy, public API, Scene,
  Prefab, or serialized reference changes.
- Each scope uses a static marker with a stable class-qualified profiler name.

## Other active-work conflicts

No conflict found. `Docs/Work/Active/` contains only its README.

## Boundaries

- Instrument only the listed `FixedUpdateNetwork` and `Render()` methods; do
  not refactor gameplay or add markers to third-party Photon/Fusion source.
- In `PlayerRunner.Render`, scope only the `OnPositionChanged` event dispatch
  with `PlayerRunner.TerritoryPositionChanged`; do not conflate unrelated
  player render work with territory expansion.
- In `TerritorySystem`, scope `HandlePlayerPositionChanged` and
  `ExpandTerritoryFromCurrentPath` so event-dispatch cost, per-render territory
  handling, and terminal polygon expansion can be distinguished.
- Do not use Deep Profile as a prerequisite for collecting these samples.

## Completion criteria

- Each selected method appears as a distinct marker in CPU Usage while keeping
  its current control flow and authority checks unchanged.
- Each selected Fusion `Render()` method appears beneath
  `NetworkRunnerRender -> UpdateFunction.Invoke()` in CPU Usage.
- The PlayerRunner position-event dispatch and the TerritorySystem expansion
  handler are separately identifiable, including the terminal polygon
  expansion path.
- Project compilation and `git diff --check` pass.

## Actual changes

- Added a static, class-qualified `ProfilerMarker` and `Auto()` scope around
  the selected `FixedUpdateNetwork` methods.
- Markers cover monster simulation, world-monster streaming, resource AOI
  refresh, stage player AOI, attack-tower target/fire loops, and the three
  projectile simulation paths.
- No authority check, control flow, networked state, spawn/despawn behavior,
  Scene, or Prefab reference changed.

## Verification results

- `dotnet build Assembly-CSharp.csproj --no-restore` passed with 0 errors.
  The 16 warnings are existing Unity/Fusion analyzer or legacy warnings.
- `git diff --check` passed.
- Manual Player Profiler capture remains pending: confirm the new marker names
  in CPU Usage on the Host Player while reproducing the long-running hitch.

## Remaining risks

The markers reveal inclusive method cost but may require a second, narrower
marker pass if one selected aggregate contains multiple expensive operations.
