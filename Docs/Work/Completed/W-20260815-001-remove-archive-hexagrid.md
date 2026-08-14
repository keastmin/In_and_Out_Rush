# W-20260815-001 Remove Archived HexaGrid

Status: Completed

## Synchronization Baseline

- Base Commit: 1049966e9bfe3129b758a1a9f9c9296f965be321
- Shared Upstream: origin/rebuild-development-environment

## Feature

Remove the archived HexaGrid implementation and its obsolete local Stage callers.

## Goal

Delete the archived scripts and Unity metadata while keeping the remaining Stage Local code compiling and free of stale type or GUID references.

## Read Documents and Skills

- `Docs/PROJECT_MAP.md`
- `Docs/Features/StageInitialization.md`
- `manage-feature-work`
- `replace-existing-feature`

## Expected Code Changes

- `Assets/02_Scripts/Stage/Local/StageInstance.cs`: remove the public `HexaTileMap` field.
- `Assets/02_Scripts/Stage/Local/StageBootstrapper.cs`: remove the HexaGrid serialized field, header, and initialization/assignment block.
- `Docs/PROJECT_MAP.md`: remove the stale `Archive` directory entry.

## Expected Asset Changes

- Delete `Assets/02_Scripts/Archive/HexaGrid/HexaTileMap.cs` and its `.meta`.
- Delete `Assets/02_Scripts/Archive/HexaGrid/HexaTileSnapSystem.cs` and its `.meta`.
- Delete `Assets/02_Scripts/Archive/HexaGrid.meta` after the folder becomes empty.

## Reserved Scene/Prefab/Data Assets

None. The existing scan found no Scene, Prefab, ScriptableObject, or other serialized references to the archived script GUIDs.

## Shared Contracts or Bootstrapper Changes

Only the local `StageInstance` and local `StageBootstrapper` callers were changed. No network, Fusion, spawn, or shared public contract changes were made.

## Other Active Work and Conflict Boundaries

`W-20260815-007-host-peer-territory-performance-refactor.md` was published after the initial reservation. Its Territory/Monster paths and network `StageBootstrapper` boundary did not overlap this work; those paths were left unchanged.

## Scope and Rollback

No Scenes or Prefabs were modified. Roll back by restoring the three deleted Unity assets and reverting the two Stage code edits and the Project Map row removal.

## Completion Criteria

- Archived scripts, script metadata, and folder metadata are removed.
- No active code or serialized asset reference contains `HexaTileMap`, `HexaTileSnapSystem`, or the archived GUIDs.
- Unity compilation and focused repository checks pass, or unverified checks are recorded.
- Actual changes, verification results, and remaining risks are recorded.

## Actual Changes

- Deleted `HexaTileMap.cs`, `HexaTileMap.cs.meta`, `HexaTileSnapSystem.cs`, `HexaTileSnapSystem.cs.meta`, and `HexaGrid.meta`.
- Removed `StageInstance.HexaTileMap` and the local Stage HexaGrid serialized field and initialization block.
- Removed the obsolete `Archive` row from `Docs/PROJECT_MAP.md`.
- Left all Scene, Prefab, ScriptableObject, network, and Fusion assets unchanged.
- Did not update `Docs/Features/StageInitialization.md`; its documented network entry points and contracts were unchanged.

## Verification Results

- Unity `6000.0.69f1` batchmode import and script compilation: passed; log ended with return code `0`.
- `dotnet build Assembly-CSharp.csproj --no-restore`: passed with 0 errors and 13 existing warnings.
- `dotnet test ProjectIO.ResourceSpawn.Tests.csproj --no-restore --verbosity normal`: build/test command exited successfully with 0 errors and 0 warnings; no test execution output was reported by the project.
- Asset/code search: no `HexaTileMap`, `HexaTileSnapSystem`, or archived GUID references remain under `Assets`; no files remain under `Assets/02_Scripts/Archive`.
- `git diff --check`: passed without whitespace errors.
- Scene/Prefab serialization review: no serialized references to the deleted script GUIDs were found and no Scene/Prefab files changed.

## Remaining Risks

- No interactive gameplay or Host/Client playtest was run; this cleanup changes only unused local HexaGrid initialization and does not change Fusion or network behavior.
- The project retains the 13 pre-existing compiler warnings listed by `dotnet build`.
