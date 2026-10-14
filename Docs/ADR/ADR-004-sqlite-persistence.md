# ADR-004: SQLite persistence

- **Status:** Accepted design; provider pending
- **Architecture reference:** §50-75, §150

## Context

Saves need slots, transactions, versioned migrations and corruption recovery. Unity has no built-in SQLite provider that works on every target platform.

## Decision

Persistence sits behind ports (`ISaveParticipant`, `IUnitOfWork`, `ISaveSlotRepository`, `ISaveMigration`). `Save.Infrastructure` contains the provider-agnostic ADO.NET parts: `AdoUnitOfWork`, `SaveSchemaV1Migration` (all tables from the architecture), `SaveDatabaseInitializer`. Until a provider is chosen, in-memory implementations satisfy the same ports (`InMemoryUnitOfWork` rolls back like a transaction).

## Consequences

Phase 3 = choose a provider (Microsoft.Data.Sqlite / SQLite-net / Mono.Data.Sqlite per platform), implement `IDbConnectionFactory` and SQL repositories, and switch `SaveInfrastructureModule`. No gameplay code changes.
