# ADR-011: Analytics

- **Status:** Accepted
- **Architecture reference:** §87-88

## Context

Telemetry must never influence or block gameplay.

## Decision

`AnalyticsProjection` observes domain events and calls `IAnalyticsService.Track`. `BufferedAnalyticsService` batches events to an `IAnalyticsSink` that may fail; `NullAnalyticsService` is the default.

## Consequences

No vendor SDK is bound yet.
