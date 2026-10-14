# ADR-007: Stable content IDs

- **Status:** Accepted
- **Architecture reference:** §13, §125

## Context

Saves must survive renaming scenes, prefabs and ScriptableObjects.

## Decision

Every business entity is identified by a strongly typed string id (`LocationId`, `EquipmentId`, ... in `Shared/Kernel/Runtime/IDs`) such as `equipment.broom.basic`. Cleaning targets carry a generated, never-regenerated `stableInstanceId`. Unity object names and instance ids are never persisted.

## Consequences

Editor tools assign / validate target ids (`Clean Time/Properties/...`). Renaming an id is a data migration.
