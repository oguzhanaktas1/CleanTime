# ADR-012: Localization

- **Status:** Accepted design; package pending
- **Architecture reference:** §97-98

## Context

All player-facing text must be translatable.

## Decision

Domain and application code only carries localization keys (`DisplayNameKey`, `PromptKey`, error codes). Views currently show the key as placeholder text.

## Consequences

Install Unity Localization when text exists and make the views resolve keys.
