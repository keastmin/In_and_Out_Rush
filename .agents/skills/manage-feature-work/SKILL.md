---
name: manage-feature-work
description: Check synchronization, detect overlap with existing ProjectIO Active reservations, wait for the worker's explicit reservation go-ahead, verify the pushed reservation before implementation, and wait for a second explicit go-ahead before pushing implementation results. Use before every request that may modify code, scenes, prefabs, ScriptableObjects, project settings, packages, tests, or implementation documentation, including migration, replacement, Territory, Fusion, and ordinary feature work.
---

# Manage Feature Work

Run this gate before every implementation Skill. Read root `AGENTS.md` and `Docs/Work/README.md`. Use `scripts/FeatureWork.ps1` for concise Git checks.

The agent performs scoped Commit and Push only after an explicit worker request at each Git handoff: once for the Active reservation and once after implementation testing. Pull remains user-controlled when the gate reports `PULL_REQUIRED`. Do not require feature or implementation approval beyond these handoffs. Never include pre-existing user changes, use automatic stash, reset, merge, rebase, force push, `git add .`, or `git add -A`.

Use the commit subject format `<prefix>: 한국어 커밋 내용`. Allowed prefixes are `feat`, `fix`, `refactor`, `perf`, `docs`, `test`, `chore`, `build`, and `ci`.

## 1. Check synchronization and conflicts

Run:

```powershell
& .agents/skills/manage-feature-work/scripts/FeatureWork.ps1 -Action CheckStart
```

If the result is `PULL_REQUIRED`, tell the user to Pull and stop. Resume only after the user confirms the Pull.

If the result is blocked for local changes, unpublished commits, divergence, missing upstream, or fetch failure, report it and stop.

When synchronized, inspect the target row in `Docs/PROJECT_MAP.md` when the request changes feature routing, the target feature document when a feature contract changes, and the reservation fields of every reported `ACTIVE_FILE`. Compare:

- exact code and Asset paths;
- Scene, Prefab, and ScriptableObject ownership;
- Bootstrapper and initialization seams;
- public contracts, shared data, Spawn flows, and Fusion NetworkObjects.

If the requested work overlaps an existing Active reservation, tell the user which reservation and boundary overlap, then stop. Do not create a second reservation.

## 2. Create the reservation

When no overlap exists, create `Docs/Work/Active/W-YYYYMMDD-NNN-short-name.md` from `Docs/Work/TEMPLATE.md` before changing implementation files.

Set `Status: Reserved`, record the `BASE_COMMIT` and `UPSTREAM` reported by `CheckStart`, and list exact expected code, Asset, shared-contract, scope, and completion fields.

For work that affects Fusion state, Spawn, RPC, Authority, or networked interaction, fill the template's `네트워크·Peer 동등성` section with:

- the input origin and Input Authority;
- the State Authority validation and mutation owner;
- the replicated state or explicit result path back to the requesting Peer;
- Host-local and Client-peer input, read-only preview or HUD, success, rejection, and failure behavior;
- duplicate-execution, readiness, Late Join, and cleanup checks relevant to the slice;
- planned runtime evidence or the exact manual Host·Client procedure when the environment cannot run multiple Peers.

For Player Builder or Player Runner network behavior, this section is required even when the worker did not explicitly request Host·Client parity. Do not accept an authority-only design that leaves a valid Client request, preview, HUD, or result feedback without a return path.

After writing the Active document, report its path and scope and wait for the worker's explicit reservation go-ahead. Do not run `CheckReservation`, stage, Commit, or Push before that request.

After the worker requests reservation progress, run:

```powershell
& .agents/skills/manage-feature-work/scripts/FeatureWork.ps1 -Action CheckReservation -WorkFile <active-document>
```

If another Pull became necessary, tell the user and stop. If the check fails for any other synchronization, scope, or conflict reason, report it and stop. When ready, stage only the exact Active document path with `git add -- <active-document>`, Commit, and Push it using the required commit subject format. Do not use broad staging and do not implement yet.

## 3. Verify the pushed reservation

After the agent's reservation Push, run:

```powershell
& .agents/skills/manage-feature-work/scripts/FeatureWork.ps1 -Action VerifyReservation -WorkFile <active-document>
```

The command verifies a clean synchronized branch and the `Reserved` document on the upstream branch. Review only newly reported `CHANGED_FILE` and `ACTIVE_FILE` entries. If a new semantic overlap appeared, report it and stop. Otherwise begin the relevant implementation Skill automatically without another approval step.

## 4. Implement and update documents

Implement only the published reservation. If an unreserved shared file or Asset becomes necessary, stop before changing it, update the Active reservation, wait for the worker's explicit reservation go-ahead, then run `CheckReservation`, stage only that reservation document, Commit and Push it, and verify it again.

After verification:

1. record actual files, tests, results, and remaining risks;
2. update the feature document when entry points, contracts, Assets, setup, or verification changed;
3. update `Docs/PROJECT_MAP.md` only when routing or responsibility changed;
4. update a Skill or `AGENTS.md` only when a reusable workflow or repository-wide rule changed;
5. run the focused tests and `git diff --check`, then report the implementation as ready for worker testing;
   for Player Builder or Player Runner network behavior, distinguish Host-local and Client-peer runtime evidence and do not claim Peer parity from compilation or static inspection alone;
6. keep the implementation changes and work document in the local worktree, leave the work document in `Active`, and wait for the worker's explicit final Commit·Push request. Do not stage, Commit, or Push during this wait;
7. after the final request, recheck the exact diff, move the work document from `Active` to `Completed`, stage only the exact files recorded in the reservation and the agent's resulting documentation changes, then Commit and Push using the required commit subject format;
8. if the worker requests changes instead of final Commit·Push, implement and verify them, report the new result, and wait for the final request again.

If Commit or Push fails, report the exact Git state and stop; do not recover with broad or destructive Git commands. Do not require workers to approve implementation or routine documentation updates; the reservation and final Commit·Push requests are explicit Git handoffs, not feature approvals. Ask only when a product decision, overlapping reservation, unsafe Git state, or meaningful scope expansion needs coordination.
