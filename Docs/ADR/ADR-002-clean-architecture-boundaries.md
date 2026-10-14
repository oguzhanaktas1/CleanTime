# ADR-002: Clean Architecture boundaries

- **Status:** Accepted
- **Architecture reference:** §4, §8-9, §82

## Context

Gameplay rules must stay testable without scenes and survive engine / storage changes.

## Decision

Every feature has `Domain` (entities, rules, events, ports), `Application` (commands, queries, handlers, projections), `Infrastructure` (adapters) and `Presentation` (MonoBehaviours, views) assemblies. Domain and Application use `noEngineReferences`. Dependencies point inwards; the allowed edges are asserted by `ArchitectureTests`.

## Consequences

Domain and Application run in plain EditMode tests. Presentation code is thin. Cross-module use of another module's `Application` assembly is limited to its public contracts (commands, query records, facades such as `IWalletService`).
