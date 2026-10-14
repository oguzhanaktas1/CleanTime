# ADR-008: Save versioning

- **Status:** Accepted
- **Architecture reference:** §73-74

## Context

Players keep saves across releases.

## Decision

`SaveSchema.CurrentVersion` is independent from the app version. Migrations form a chain (`SaveMigrationRunner`); a missing link is an error, a newer database than the build understands is refused. Participants treat a missing section as 'use defaults' so new modules can be added without breaking old saves.

## Consequences

Every schema change needs a migration and a test.
