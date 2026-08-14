---
name: replace-existing-feature
description: Fully replace an existing ProjectIO Unity feature or implementation and remove the superseded active path. Use when the user explicitly requests replacement, legacy removal, caller and serialized-reference cutover, or deletion of an old implementation rather than an incremental coexistence migration.
---

# Replace Existing Feature

Begin only after `manage-feature-work` verifies the pushed Active reservation and finds no overlap.

Replace the active implementation only after proving every caller and serialized reference has moved.

## Read first

1. Read root `AGENTS.md`, `Docs/PROJECT_MAP.md`, active work files, and the target feature document.
2. Map existing implementation classes, callers, events, Scene·Prefab references, ScriptableObjects, network state, persistence, and side effects.
3. Record the replacement scope, rollback, and reserved assets in the active work file.

Use `photon-fusion-feature` for network effects. Use `migrate-feature-slice` instead when legacy must remain active across multiple consumer migrations.

## Replace

1. Establish focused current-behavior verification.
2. Implement the replacement behind a clear feature boundary.
3. Move every caller and composition-root connection.
4. Replace reserved Scene, Prefab, and data references.
5. Remove old event subscriptions, static access, and duplicate side effects.
6. Search again for old type, method, guid, and asset references.
7. Delete the old path only after no active reference remains.

Do not leave the previous implementation commented out or keep two active implementations without an explicit compatibility reason.

## Complete

Run focused tests, project compilation, serialized-reference checks, and relevant Host·Client verification. Record replaced behavior, removed files, retained compatibility, manual setup, rollback, and unverified risks in the completed work file. Update the feature document and Project Map when entry points or routing changed.
