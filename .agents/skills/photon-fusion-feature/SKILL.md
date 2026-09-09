---
name: photon-fusion-feature
description: Design, implement, or verify ProjectIO Photon Fusion 2 gameplay, lifecycle, and Host-client player-experience parity. Use for Networked state, RPCs, State/Input Authority, NetworkObject Spawn or Despawn, Host and Client synchronization, AOI, Late Join restoration, scene loading, reconnect, or networked interaction changes.
---

# Photon Fusion Feature

Begin only after `manage-feature-work` verifies the pushed Active reservation and finds no overlap.

## Load context

Read root `AGENTS.md`, `Docs/PROJECT_MAP.md`, the active work file, and the target feature document. Inspect a similar existing network flow before introducing a new pattern.

## Preserve authority and Peer experience

Shared game results are Host·State Authority decisions. Player experience is a separate invariant: unless the feature explicitly differs by role, a Player Builder or Player Runner controlled by the Host and one controlled by a Client must have equivalent input availability, read-only preview or HUD information, success results, and rejection or failure feedback. Apply this invariant even when the worker does not mention Host·Client parity.

Trace every player action end to end:

1. Input Authority captures the intent and any local-only affordance.
2. The request reaches the State Authority through Fusion input, an RPC, or an existing authoritative command seam.
3. State Authority validates the requesting player and performs shared mutation, Spawn, or Despawn exactly once.
4. Persistent success is represented by replicated state or NetworkObject lifetime. A rejection or failure that cannot be inferred from replicated state has an explicit result path back to the requester.
5. Host-local and Client-peer presentation consume equivalent outcomes. The Host path must not bypass the request/result contract in a way that hides Client-only failures or causes the Host's server and local-client roles to execute twice.

Authority guards protect writes; they are not blanket presentation guards. Do not require State Authority merely to read a valid replicated value, collect local input, show a placement preview, update a HUD, or present a request result. Treat `IsInSimulation`, singleton availability, Scene readiness, and NetworkObject spawn timing as Peer-specific readiness conditions instead of assuming the Host's readiness proves the Client's.

## Decide the network boundary

Treat state as network-relevant when it affects game results, must be observed by another player, must survive Late Join, or controls NetworkObject lifecycle. Treat camera, local HUD, post-processing, hover, and player-only guidance as local presentation unless design says otherwise.

Record when it is not already clear:

- authoritative state and State Authority;
- input origin and Input Authority;
- request transport and requester validation;
- success and rejection/failure return paths;
- Host-local and Client-peer preview, HUD, and feedback behavior;
- observer and AOI requirements;
- Late Join restoration source;
- Host-only and per-client execution;
- Spawn, Despawn, and event-cleanup owner.

## Implement

- Do not use RPC as the only storage for persistent state.
- Keep network state separate from local presentation.
- Reject unauthorized state mutation.
- Use Fusion Spawn and Despawn for NetworkObjects.
- Put simulation state changes in the appropriate Fusion simulation path and local interpolation or display in Render/local callbacks.
- Avoid duplicate execution when Host is both server and local client.
- Avoid Host-only direct calls that skip the Client request or result path. If the Host uses a fast path, route both paths through the same validation and result contract.
- Return actionable denial or failure to the requesting Player when replicated persistent state alone cannot explain the outcome; do not leave a Client waiting on a Host-only callback or local flag.
- Make initialization tolerant of Network Spawn and Additive Scene readiness occurring on different frames.

## Verify

For Player Builder and Player Runner behavior, verify both a Host-local player and a Client Input Authority player. Cover the slice's success path and at least one meaningful rejection or failure path, including input availability, preview or HUD when relevant, authoritative result, and local feedback. Also verify:

- unauthorized Client requests cannot mutate shared state;
- Host-local execution does not duplicate events, Spawn, state changes, or presentation;
- Late Join restoration when relevant;
- AOI enter and exit when relevant;
- Despawn, disconnect, and Scene unload cleanup;
- delayed NetworkObject, singleton, or Additive Scene readiness on the Client when relevant.

Record Host and Client evidence separately. Compilation, pure tests, or static authority inspection do not prove runtime Peer parity. When the environment cannot run a real Host and Client, record the limitation and exact manual steps, and leave runtime parity explicitly unverified until the worker supplies that evidence.
