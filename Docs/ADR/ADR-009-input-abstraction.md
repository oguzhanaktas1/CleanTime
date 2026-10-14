# ADR-009: Input abstraction

- **Status:** Accepted
- **Architecture reference:** §44, §146

## Context

Mobile and gamepad ports must not require gameplay changes.

## Decision

Gameplay depends on `IGameplayInput` (`MoveInput` + `GameplayIntent`). `UnityGameplayInput` implements it with the Input System, from an assigned `InputActionAsset` or from default keyboard/gamepad bindings built in code. Hold-to-clean is a presentation mapping; the domain only knows `ApplyCleaningAction`.

## Consequences

No `.inputactions` asset is required yet. Add one later and assign it on `UnityGameplayInput`.
