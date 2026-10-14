# Save format

## Today (in-memory)

`ISaveParticipant`s (one per stateful repository) write a small text section per slot into an `ISnapshotStore`
inside an atomic `IUnitOfWork`. Sections are `key -> payload` strings with invariant-culture numbers:

| Key | Owner | Payload |
|---|---|---|
| `economy.wallet` | Economy | balance in cents |
| `locations.progress` | Locations | one line per location: `id\|unlocked\|completions\|bestScore\|bestSeconds\|lastDay` |
| `progression.player` | Progression | `jobs\|companyUnlocked\|storyLevel\|jobTier` then a line of milestone ids |
| `equipment.progress` | Equipment | one line per tool `id\|owned\|upgradeLevel`, then loadout and selection |
| `relationships.all` | Relationships | `npcId\|points\|level\|lastChangedDay` per line |
| `quests.progress` | Quests | `questId\|state\|progress,progress,...` per line |
| `time.calendar` | GameTime | `day\|phase\|actionsInPhase` |

Rules (enforced by tests):

- Loading resets every participant first. A **missing** section means "use defaults" (older save, newer build);
  an **invalid** section aborts the load and leaves a clean state.
- Saving is all-or-nothing; a failed participant rolls the whole save back and `SaveFailedEvent` is published.
- Corrupted slots are refused (`is_corrupted`), the active slot cannot be deleted, deleting a slot removes its data.
- Autosave runs only when something changed and only at checkpoints (job completed, day started, purchase, gift,
  company created, employee job resolved, focus lost / pause / quit).

## Target (SQLite, Phase 3)

`SaveSchemaV1Migration` already contains the complete schema from architecture §55-§71 (plus the version table):
`schema_version`, `save_slot`, `player_state`, `location_progress`, `inventory_item`, `equipment_progress`, `job_history`,
`active_job`, `relationship`, `gift_history`, `quest_progress`, `company`, `employee`, `employee_assignment`,
`company_upgrade`, `economy_transaction`, `game_setting`, `story_flag`. Every per-slot table cascades from `save_slot`.
Connection pragmas (`foreign_keys`, WAL, `synchronous = NORMAL`) are applied once after opening, outside transactions.

Upgrading: `SaveDatabaseInitializer` reads `schema_version` (missing table = 0), refuses newer databases, and runs the migration
chain inside one transaction (`BEGIN` ... `COMMIT`/`ROLLBACK`). Back up the file before a migration (`save.db.pre_migration_backup`)
when the provider is wired. Add a migration class + a test for every schema change and bump `SaveSchema.CurrentVersion`.

Not persisted yet: the active job, the cleaning session, job history, gift ownership, inventory, company, employees and
their assignments, the ledger (see IMPLEMENTATION_STATUS "Known gaps").
