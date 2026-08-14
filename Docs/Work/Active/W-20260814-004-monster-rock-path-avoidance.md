# W-20260814-004 Monster Rock Path Avoidance

Status: Reserved

## Synchronization baseline

- Base Commit: ee35c534160ff2016c13ac856637e46dc99629bc
- Upstream: origin/rebuild-development-environment

## 담당자

Codex

## 기능

Monsters and Projectiles

## 목표

Host-authoritative world monsters must reject newly selected movement destinations whose path intersects a spawned rock obstacle, while preserving Fusion movement replication and the existing patrol behavior.

## 읽을 문서와 Skill

- AGENTS.md
- Docs/PROJECT_MAP.md
- Docs/Features/MonstersAndProjectiles.md
- Docs/Features/GridAndObstacles.md
- manage-feature-work
- photon-fusion-feature

## 예상 수정 코드

- Assets/02_Scripts/Monster/WorldMonster.cs
- Assets/02_Scripts/Monster/Centipede.cs
- Assets/02_Scripts/Monster/Strider.cs
- Assets/02_Scripts/Monster/Stalker.cs
- Assets/02_Scripts/System/WorldMonsterSpawnSystem.cs
- Assets/02_Scripts/Stage/Network/StageBootstrapper.YOU.cs
- Docs/Features/MonstersAndProjectiles.md

## 예약 Scene·Prefab·Data Asset

없음. Existing obstacle and world-monster assets remain serialized-compatible; no Inspector reassignment is expected.

## 공용 계약 또는 Bootstrapper 변경

Use the existing `IWorldObstacleConsumer` seam to provide the live obstacle list to `WorldMonsterSpawnSystem` after authoritative obstacle spawning and before world-monster spawning. No new public network contract, RPC, Networked property, or Scene reference is planned.

## 다른 활성 작업과 겹치는 부분

CheckStart reported no active work file. The branch is synchronized with upstream.

## 범위 밖

- Do not migrate or replace the existing monster hierarchy.
- Do not modify Local monsters, Track monsters, obstacle placement/despawn rules, or rock prefabs.
- Do not add a general navigation/pathfinding system; candidate rejection and movement-time safety checks are sufficient for this fix.
- Do not change Fusion Spawn, Despawn, State Authority, AOI, or client presentation ownership.

## 완료 조건

- State Authority receives the live `WorldObstacle` list before world-monster records are spawned.
- World-monster patrol destinations and Strider slide destinations are rejected when the center path overlaps an obstacle bounds expanded by monster clearance.
- Centipede movement and Stalker chase stop before entering a newly blocking obstacle; patrol can select a new valid destination afterward.
- Existing territory/sanctuary checks, movement speed, arrival behavior, and Fusion-authoritative velocity replication remain intact.
- Focused project compilation, `git diff --check`, and repository status checks are recorded. Unity Host/Client playtest remains for the user.

## 실제 변경

## 검증 결과

## 남은 위험

