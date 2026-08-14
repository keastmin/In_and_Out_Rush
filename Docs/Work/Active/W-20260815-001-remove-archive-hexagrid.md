# W-20260815-001 Remove Archived HexaGrid

Status: Reserved

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

Only the local `StageInstance` and local `StageBootstrapper` callers are in scope. No network, Fusion, spawn, or shared public contract changes are expected.

## Other Active Work and Conflict Boundaries

`CheckStart` reported no existing Active reservation. The reserved boundary is limited to the paths listed above and their completion record.

## Scope and Rollback

Do not modify Scenes or Prefabs. Roll back by restoring the three deleted Unity assets and reverting the two Stage code edits and the Project Map row removal.

## Completion Criteria

- Archived scripts, script metadata, and folder metadata are removed.
- No active code or serialized asset reference contains `HexaTileMap`, `HexaTileSnapSystem`, or the archived GUIDs.
- Unity compilation and focused repository checks pass, or unverified checks are recorded.
- Actual changes, verification results, and remaining risks are recorded before moving this file to `Docs/Work/Completed/`.

## Actual Changes

To be recorded after implementation.

## Verification Results

To be recorded after implementation.

## Remaining Risks

To be recorded after implementation.
