---
name: migrate-feature-slice
description: Incrementally move one bounded ProjectIO feature flow from legacy code to a new domain structure while preserving current behavior and rollback. Use for consumer-by-consumer migration, extracting logic from managers or bootstrappers, introducing adapters or asmdefs, changing serialized references in stages, or running a shadow/cutover migration outside the dedicated chunk-territory workflow.
---

# Migrate Feature Slice

Move one observable flow per task. Keep the legacy path authoritative until the active work file explicitly authorizes cutover.

## Load minimal context

1. Read root `AGENTS.md`.
2. Read `Docs/PROJECT_MAP.md`.
3. Read `Docs/Work/Active/` and confirm the task reservation.
4. Read the target `Docs/Features/<Feature>.md` and directly connected feature documents only.
5. Inspect actual callers, events, serialized references, data assets, network state, and side effects named by the task.

Apply `photon-fusion-feature` when the slice changes authority, replication, Spawn, RPC, Late Join, or Fusion lifecycle. Apply `replace-existing-feature` only when the task authorizes complete replacement rather than staged migration.

## Bound the slice

Record one objective, allowed files, reserved assets, legacy and new entry points, contract changes, acceptance criteria, rollback, and deferred consumers in the active work file. Split a shared contract or Bootstrapper seam into a prerequisite task when multiple active tasks need it.

## Migrate safely

1. Characterize current behavior with a focused test or repeatable manual procedure.
2. Create the smallest domain boundary needed by the slice.
3. Preserve Unity serialization unless an Asset change is reserved.
4. Connect one caller, consumer, or composition-root seam to the new path.
5. Prevent legacy and new paths from producing the same state change, Spawn, event, or presentation twice.
6. Verify the slice before migrating another consumer.
7. Remove legacy code only when the task explicitly authorizes cutover and no caller or serialized reference remains.

For asmdef work, move only code that does not depend backward on `Assembly-CSharp`. Start with pure contracts or logic and keep adapters on the legacy side until dependencies point in one direction.

## Verify and record

Run focused tests, project compilation, and relevant serialized, initialization, authority, Late Join, event cleanup, and duplicate-execution checks. Record evidence, manual setup, rollback, and risks in the active work file.

Move a completed work file to `Docs/Work/Completed/`. Update the feature document when entry points, contracts, assets, or verification changed. Update `Docs/PROJECT_MAP.md` only when routing changed. Use a handoff only for a long migration across multiple tasks.

Stop after the approved slice.
