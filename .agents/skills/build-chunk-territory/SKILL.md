---
name: build-chunk-territory
description: Design, implement, verify, migrate, or retire ProjectIO Territory domain, query, expansion, trail, replication, presentation, and consumer paths, including chunk-based work and legacy cutover.
---

# Build Chunk Territory

Begin only after `manage-feature-work` verifies the pushed Active reservation and finds no overlap.

This Skill does not prescribe Chunk as the final Territory representation. It governs bounded
Territory work that may introduce, migrate, replace, or retire Chunk-based elements while keeping
the current gameplay contract explicit.

## Establish current truth

1. Read root `AGENTS.md`.
2. Read `Docs/PROJECT_MAP.md`, `Docs/Features/Territory.md`, and conflicting active work files.
3. Read the current Active reservation and any design or decision document explicitly named by it.
4. Inspect actual code, callers, tests, Fusion state, and serialized references.

Code and serialized references are authoritative when they disagree with a document. Completed work
records are historical evidence, not an active implementation contract. Do not recreate a retired
prototype merely because an older record mentions it.

Apply `photon-fusion-feature` when authority, replication, recovery, RPC, or Fusion lifecycle can
change. Apply `replace-existing-feature` for a full replacement and
`migrate-feature-slice` for a coexistence migration.

## Preserve useful boundaries

- Separate logical Territory state and queries, expansion rules, Trail rules, networking,
  presentation, consumers, and composition.
- Treat logical data and visual data as separate responsibilities even when a temporary adapter
  still updates both.
- Follow the repository domain structure in `AGENTS.md`; this Skill does not reserve a permanent
  `Territory Refactor` directory or namespace.
- Create only folders and abstractions required by the current reserved phase.
- Put one top-level class, struct, enum, or interface in each file.
- Introduce or change an asmdef only after mapping dependencies and serialized impact.
- Keep active gameplay behavior authoritative until the current reservation explicitly defines a
  cutover.

## Execute a reserved phase

1. Classify each touched path as active authority, active presentation, compatibility seam,
   disconnected prototype, or historical test/document.
2. Audit code references, serialized references, and Fusion messages before removing or replacing a
   path.
3. A disconnected prototype may be deleted without a gameplay cutover when it owns no consumer,
   serialized setup, required authority state, or externally used contract. Remove its tests and
   documentation in the same phase.
4. For behavior changes, define coordinate precision, ownership, revision, publication, recovery,
   and rollback contracts in the current design or Active work file before implementation.
5. Implement the smallest coherent phase. Prefer one consumer seam at a time, but keep tightly
   coupled callers together when splitting them would create duplicate or invalid behavior.
6. Ensure old and new paths cannot produce the same mutation, Spawn, event, network publication, or
   presentation twice.
7. Keep Unity and Fusion APIs out of pure calculation code where practical. Publish background
   results on the owning main-thread/authority seam.
8. Verify focused domain tests, Unity/Fusion compilation, serialized references, Host·Client
   behavior, teardown, failure, and recovery to the extent affected.
9. Update the Territory feature document, project map, current decision/design documents, and
   Active work evidence only where responsibilities or verification actually changed.

Stop and request a decision when the current reservation or design leaves a behavior-changing
contract ambiguous. Do not treat speculative or retired documents as approval.

## Completion gate

- acceptance criteria passed or the exact unverified item is recorded;
- remaining authority, query, Trail, presentation, and consumer owners are named;
- duplicate old/new execution is excluded;
- code, tests, RPCs, serialized setup, and matching Unity `.meta` files are consistent;
- rollback and remaining migration seams are recorded when behavior changed;
- no future phase is implemented or documented as approved unless the user requested it.
