# ADR-001: Modular monolith

- **Status:** Accepted
- **Architecture reference:** §3, §5, §114-115

## Context

Clean Time is a single-player, offline-first game. Splitting gameplay into network services would add latency and failure modes without any benefit.

## Decision

The client is one Unity project split into feature modules (`Assets/_Project/Features/*`). Each module owns its data and talks to others through public commands, queries and domain events. Only account / cloud save / analytics / remote config may ever become server-side.

## Consequences

Modules are cheap to add and test. Boundaries are enforced by assembly definitions and `ArchitectureTests`, not by the network.
