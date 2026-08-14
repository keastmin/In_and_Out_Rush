---
name: manage-feature-work
description: Synchronize, reserve, publish, and safely begin every ProjectIO implementation task before feature-specific coding. Use whenever a request may modify code, scenes, prefabs, ScriptableObjects, project settings, packages, tests, or implementation documentation. This gate runs before migration, replacement, Territory, Fusion, or ordinary feature work.
---

# Manage Feature Work

Run this gate before loading a feature-specific implementation Skill. Read root `AGENTS.md` and `Docs/Work/README.md` first.

Use `scripts/FeatureWork.ps1` for Git state transitions. Treat a nonzero exit as a hard stop. Never compensate with stash, reset, merge commits, force push, or broad staging.

## 1. Synchronize

Run:

```powershell
& .agents/skills/manage-feature-work/scripts/FeatureWork.ps1 -Action Preflight
```

The command fetches and uses `pull --ff-only` only from a clean, non-diverged branch. If it blocks, report the reason and stop without creating an Active document or changing implementation files.

When ready, read only:

1. the relevant row in `Docs/PROJECT_MAP.md`;
2. filenames and reservation fields in `Docs/Work/Active/`;
3. the target feature document;
4. actual callers and serialized assets needed to name the reservation.

Do not scan the whole project when these sources are sufficient.

## 2. Create and refresh the reservation

Create one `Docs/Work/Active/W-YYYYMMDD-NNN-short-name.md` from `Docs/Work/TEMPLATE.md`. Set:

- `Status: Awaiting Approval`;
- the preflight `BASE_COMMIT`;
- exact expected code paths and reserved Scene, Prefab, or data assets;
- shared contracts, conflict risks, scope exclusions, and acceptance criteria.

Do not change implementation files. Then run:

```powershell
& .agents/skills/manage-feature-work/scripts/FeatureWork.ps1 -Action RefreshReservation -WorkFile <active-document>
```

Review only `CHANGED_FILE` and `ACTIVE_FILE` output. Compare reservations semantically, including shared initialization, public contracts, Spawn flows, Fusion NetworkObjects, Scene, Prefab, and ScriptableObject ownership. If overlap exists, revise scope or stop for coordination.

Show the reservation to the user and stop for explicit approval. Ask for one approval that authorizes publishing only this Active document and continuing implementation after the final gate passes.

## 3. Publish after approval

After explicit approval, change the document to `Status: Reserved` and record the approval. Run:

```powershell
& .agents/skills/manage-feature-work/scripts/FeatureWork.ps1 -Action PublishReservation -WorkFile <active-document> -CommitMessage "chore(work): reserve <short-name>"
```

The command rejects any staged, modified, or untracked path other than the Active document. It commits and pushes only that document. If the remote moved or push verification fails, stop; do not implement.

## 4. Verify and implement

Run:

```powershell
& .agents/skills/manage-feature-work/scripts/FeatureWork.ps1 -Action VerifyImplementation -WorkFile <active-document>
```

If new `CHANGED_FILE` or `ACTIVE_FILE` entries appear, inspect only those changes and repeat semantic conflict review. Continue automatically when the remote reservation exists, the branch is synchronized, and no overlap remains.

Load the required feature-specific Skill and implement only the published reservation. If a new shared file or asset becomes necessary, update the Active document and repeat Refresh, user approval, Publish, and Verify before touching the expanded scope.

## Complete

Verify the feature, record exact evidence and remaining risks, update only affected feature or Project Map documents, and move the Active document to `Docs/Work/Completed/`.

Do not automatically commit or push implementation changes unless the user authorized that separately. Until the completed implementation is published, the remote Active reservation remains the conservative lock.
