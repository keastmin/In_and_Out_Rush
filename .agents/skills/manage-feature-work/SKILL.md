---
name: manage-feature-work
description: Check synchronization, detect overlap with existing ProjectIO Active reservations, create a task reservation, and verify that the worker pushed it before any implementation begins. Use before every request that may modify code, scenes, prefabs, ScriptableObjects, project settings, packages, tests, or implementation documentation, including migration, replacement, Territory, Fusion, and ordinary feature work.
---

# Manage Feature Work

Run this gate before every implementation Skill. Read root `AGENTS.md` and `Docs/Work/README.md`. Use `scripts/FeatureWork.ps1` for concise Git checks.

The worker performs Pull, Commit, and Push. Do not ask either worker to approve the other worker's reservation. Do not use automatic stash, reset, merge, rebase, force push, or broad staging.

## 1. Check synchronization and conflicts

Run:

```powershell
& .agents/skills/manage-feature-work/scripts/FeatureWork.ps1 -Action CheckStart
```

If the result is `PULL_REQUIRED`, tell the worker to Pull and stop. Resume only after the worker confirms the Pull.

If the result is blocked for local changes, unpublished commits, divergence, missing upstream, or fetch failure, report it and stop.

When synchronized, inspect the target row in `Docs/PROJECT_MAP.md`, the target feature document, and the reservation fields of every reported `ACTIVE_FILE`. Compare:

- exact code and Asset paths;
- Scene, Prefab, and ScriptableObject ownership;
- Bootstrapper and initialization seams;
- public contracts, shared data, Spawn flows, and Fusion NetworkObjects.

If the requested work overlaps an existing Active reservation, tell the worker which reservation and boundary overlap, then stop. Do not create a second reservation.

## 2. Create the reservation

When no overlap exists, create `Docs/Work/Active/W-YYYYMMDD-NNN-short-name.md` from `Docs/Work/TEMPLATE.md` before changing implementation files.

Set `Status: Reserved`, record the `BASE_COMMIT` and `UPSTREAM` reported by `CheckStart`, and list exact expected code, Asset, shared-contract, scope, and completion fields.

Run:

```powershell
& .agents/skills/manage-feature-work/scripts/FeatureWork.ps1 -Action CheckReservation -WorkFile <active-document>
```

If another Pull became necessary, tell the worker and stop. Otherwise tell the worker to Commit and Push the Active document. Do not implement yet.

## 3. Verify the pushed reservation

After the worker says the Active document was pushed, run:

```powershell
& .agents/skills/manage-feature-work/scripts/FeatureWork.ps1 -Action VerifyReservation -WorkFile <active-document>
```

The command verifies a clean synchronized branch and the `Reserved` document on the upstream branch. Review only newly reported `CHANGED_FILE` and `ACTIVE_FILE` entries. If a new semantic overlap appeared, report it and stop. Otherwise begin the relevant implementation Skill automatically without another approval step.

## 4. Implement and update documents

Implement only the published reservation. If an unreserved shared file or Asset becomes necessary, stop, update the Active reservation, ask the worker to Push it, and verify it again.

After verification:

1. record actual files, tests, results, and remaining risks;
2. update the feature document when entry points, contracts, Assets, setup, or verification changed;
3. update `Docs/PROJECT_MAP.md` only when routing or responsibility changed;
4. update a Skill or `AGENTS.md` only when a reusable workflow or repository-wide rule changed;
5. move the work document from `Active` to `Completed`.

Do not require workers to read or approve these routine documentation updates. Ask only when a product decision, overlapping reservation, unsafe Git state, or meaningful scope expansion needs coordination.
