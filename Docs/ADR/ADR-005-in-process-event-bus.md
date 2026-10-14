# ADR-005: In-process event bus

- **Status:** Accepted
- **Architecture reference:** §15-16, §85

## Context

Modules must react to each other (job completed -> reward, progression, quests, save) without direct references.

## Decision

`EventBus` is synchronous and main-thread only. Handlers run in subscription order, a throwing handler is logged and isolated. Commands trigger primary behaviour; events only notify. Subscription order is fixed by the module order in `FeatureRegistration`.

## Consequences

Deterministic, trivially testable. Order matters, so `Save` is always registered/initialised last and autosave checkpoints run after state changes.
