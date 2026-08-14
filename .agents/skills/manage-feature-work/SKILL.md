---
name: manage-feature-work
description: Check synchronization, detect overlap with existing ProjectIO Active reservations, wait for worker confirmation, and verify the agent pushed the reservation before implementation begins. Use before every request that may modify code, scenes, prefabs, ScriptableObjects, project settings, packages, tests, or implementation documentation, including migration, replacement, Territory, Fusion, and ordinary feature work.
---

# Manage Feature Work

Run this gate before every implementation Skill. Read root `AGENTS.md` and `Docs/Work/README.md`. Use `scripts/FeatureWork.ps1` for concise Git checks.

The agent performs scoped Commit and Push only after the worker confirms the Active reservation. Pull remains user-controlled when the gate reports `PULL_REQUIRED`. Do not require feature or implementation approval. Never include pre-existing user changes, use automatic stash, reset, merge, rebase, force push, `git add .`, or `git add -A`.

## 1. Check synchronization and conflicts

Run:

```powershell
& .agents/skills/manage-feature-work/scripts/FeatureWork.ps1 -Action CheckStart
```

If the result is `PULL_REQUIRED`, tell the user to Pull and stop. Resume only after the user confirms the Pull.

If the result is blocked for local changes, unpublished commits, divergence, missing upstream, or fetch failure, report it and stop.

When synchronized, inspect the target row in `Docs/PROJECT_MAP.md`, the target feature document, and the reservation fields of every reported `ACTIVE_FILE`. Compare:

- exact code and Asset paths;
- Scene, Prefab, and ScriptableObject ownership;
- Bootstrapper and initialization seams;
- public contracts, shared data, Spawn flows, and Fusion NetworkObjects.

If the requested work overlaps an existing Active reservation, tell the user which reservation and boundary overlap, then stop. Do not create a second reservation.

## 2. Create the reservation

When no overlap exists, create `Docs/Work/Active/W-YYYYMMDD-NNN-short-name.md` from `Docs/Work/TEMPLATE.md` before changing implementation files.

Set `Status: Reserved`, record the `BASE_COMMIT` and `UPSTREAM` reported by `CheckStart`, and list exact expected code, Asset, shared-contract, scope, and completion fields.

Run:

```powershell
& .agents/skills/manage-feature-work/scripts/FeatureWork.ps1 -Action CheckReservation -WorkFile <active-document>
```

If another Pull became necessary, tell the user and stop. Otherwise wait for the worker to confirm the Active document. After confirmation, stage only the exact Active document path with `git add -- <active-document>`, Commit, and Push it. Do not use broad staging and do not implement yet.

## 3. Verify the pushed reservation

After the agent's reservation Push, run:

```powershell
& .agents/skills/manage-feature-work/scripts/FeatureWork.ps1 -Action VerifyReservation -WorkFile <active-document>
```

The command verifies a clean synchronized branch and the `Reserved` document on the upstream branch. Review only newly reported `CHANGED_FILE` and `ACTIVE_FILE` entries. If a new semantic overlap appeared, report it and stop. Otherwise begin the relevant implementation Skill automatically without another approval step.

## 4. Implement and update documents

Implement only the published reservation. If an unreserved shared file or Asset becomes necessary, stop before changing it, update the Active reservation, wait for worker confirmation, then run `CheckReservation`, stage only that reservation document, Commit and Push it, and verify it again.

After verification:

1. record actual files, tests, results, and remaining risks;
2. update the feature document when entry points, contracts, Assets, setup, or verification changed;
3. update `Docs/PROJECT_MAP.md` only when routing or responsibility changed;
4. update a Skill or `AGENTS.md` only when a reusable workflow or repository-wide rule changed;
5. move the work document from `Active` to `Completed`;
6. stage only the exact files recorded in the reservation and the agent's resulting documentation changes, then Commit and Push.

If Commit or Push fails, report the exact Git state and stop; do not recover with broad or destructive Git commands. Do not require workers to approve implementation or routine documentation updates; the Active reservation confirmation is the required handoff. Ask only when a product decision, overlapping reservation, unsafe Git state, or meaningful scope expansion needs coordination.
