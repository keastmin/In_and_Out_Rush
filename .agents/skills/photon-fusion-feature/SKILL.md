---
name: photon-fusion-feature
description: Design, implement, or verify ProjectIO Photon Fusion 2 gameplay and lifecycle changes. Use for Networked state, RPCs, State/Input Authority, NetworkObject Spawn or Despawn, Host and Client synchronization, AOI, Late Join restoration, scene loading, reconnect, or networked interaction changes.
---

# Photon Fusion Feature

## Load context

Read root `AGENTS.md`, `Docs/PROJECT_MAP.md`, the active work file, and the target feature document. Inspect a similar existing network flow before introducing a new pattern.

## Decide the network boundary

Treat state as network-relevant when it affects game results, must be observed by another player, must survive Late Join, or controls NetworkObject lifecycle. Treat camera, local HUD, post-processing, hover, and player-only guidance as local presentation unless design says otherwise.

Record when it is not already clear:

- authoritative state and State Authority;
- input origin and Input Authority;
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
- Make initialization tolerant of Network Spawn and Additive Scene readiness occurring on different frames.

## Verify

- Host behavior
- Client behavior
- unauthorized client request
- Late Join restoration when relevant
- AOI enter and exit when relevant
- Despawn, disconnect, and Scene unload cleanup
- duplicate events, Spawn, and local presentation

Record any environment limitation that prevents a real Host·Client run. Compilation alone is not runtime network verification.
