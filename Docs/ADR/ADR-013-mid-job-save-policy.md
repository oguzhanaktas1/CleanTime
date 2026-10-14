# ADR-013: Mid-job save policy

- **Status:** Deferred
- **Architecture reference:** §61, §127

## Context

Saving during a cleaning job requires persisting every target's health.

## Decision

Not supported yet. The active job and cleaning session live in memory only and are not `ISaveParticipant`s; saves taken during a job restore the state from before it started. Stable target ids and the `active_job` / `active_job_target_state` tables are already designed for the day this is needed.

## Consequences

Decide with design whether players may save mid-job before implementing.
