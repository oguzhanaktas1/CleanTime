# ADR-010: Backend boundary

- **Status:** Accepted
- **Architecture reference:** §113-118

## Context

Future online features must not leak into the client domain.

## Decision

Server features sit behind ports (`ICloudSaveService`, `IAchievementService`, `IAnalyticsService`) with offline-safe defaults (`OfflineCloudSaveService`, `NullAchievementService`, `NullAnalyticsService`). HTTP DTOs are mapped to application contracts and never become domain objects.

## Consequences

The game is fully playable offline; every online adapter can fail without affecting gameplay.
