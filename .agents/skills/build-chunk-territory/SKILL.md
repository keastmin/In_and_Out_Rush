---
name: build-chunk-territory
description: Design, implement, verify, and incrementally cut over ProjectIO from the legacy polygon territory pipeline to the chunk-based territory and trail pipeline. Use for chunk storage, territory expansion, trail processing, revision contracts, Fusion replication, shadow validation, consumer migration, legacy cutover, rollback, or long-running Territory handoff work.
---

# Build Chunk Territory

Complete one approved Territory milestone per task. Keep the legacy pipeline authoritative until an approved cutover milestone changes it.

## Load minimal context

1. Read root `AGENTS.md`.
2. Read `Docs/PROJECT_MAP.md`, `Docs/Features/Territory.md`, and conflicting active work files.
3. Read `references/work-session-protocol.md`.
4. For a long-running migration, read only its `CURRENT_MILESTONE.md`, `HANDOFF.md`, and files listed under `Read first`.
5. Inspect actual code and serialized references when they disagree with a document.

Apply `photon-fusion-feature` for authority, replication, Late Join, Spawn, RPC, or Fusion lifecycle changes. Use this Skill's consumer migration rules instead of also loading the general migration Skill.

## Code boundary

- Add new Chunk Territory runtime code and tests under `Assets/02_Scripts/Territory Refactor/` until an approved decision changes the location.
- Use `ProjectIO.Territory` and `ProjectIO.Territory.Tests` by default.
- Separate domain state, Trail processing, networking, presentation, consumers, and integration.
- Create only the domain subfolders required by the milestone.
- Put one top-level class, struct, enum, or interface in each file.
- Introduce an asmdef only in a milestone that maps dependencies and proves no backward dependency on `Assembly-CSharp`.

## Execute a milestone

1. Confirm status, objective, prerequisite, allowed files, prohibited changes, acceptance criteria, and rollback.
2. Reserve shared code and assets in `Docs/Work/Active/`.
3. Keep approved coordinate, revision, ownership, and network contracts fixed.
4. Implement the smallest coherent stage.
5. Migrate one consumer or one composition-root seam at a time.
6. Prevent legacy and Chunk paths from producing the same state change, Spawn, event, or presentation twice.
7. Run focused tests, compilation, serialized checks, and relevant Host·Client verification.
8. Update the roadmap status, long-running handoff, current milestone, Territory feature document, and active work evidence as applicable.

Do not remove legacy code without an approved cutover milestone. Stop when an approved contract is insufficient and record a decision request instead of inventing a replacement.

## Completion gate

- acceptance criteria passed or exact failure recorded;
- authority and Late Join restoration documented for affected state;
- duplicate legacy/new execution excluded;
- changed code and serialized setup listed;
- rollback and remaining consumers recorded;
- next milestone prepared without being implemented.
