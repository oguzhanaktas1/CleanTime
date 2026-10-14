# ADR-006: Addressables

- **Status:** Deferred
- **Architecture reference:** §47-48

## Context

Addressables is the intended content loader, but there are no assets yet and the package needs a configured settings asset.

## Decision

The package is not installed. Definitions are plain ScriptableObjects without `AssetReference` fields; scenes are resolved by a business `SceneKey` through `ILocationSceneResolver` / `ISceneLoader`, so the loader can switch to Addressables without touching gameplay.

## Consequences

When art arrives: install `com.unity.addressables`, add `AssetReference*` fields to the definitions, implement an Addressables `ISceneLoader`.
