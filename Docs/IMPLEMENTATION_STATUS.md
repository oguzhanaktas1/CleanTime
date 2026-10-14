# Implementation status

How the code maps to [`TECHNICAL_ARCHITECTURE.md`](TECHNICAL_ARCHITECTURE.md): what exists, what is a placeholder, and where the
implementation deliberately differs. Section numbers (§) refer to the architecture document.

## Phases (§158-168)

| Phase | State |
|---|---|
| 0 Foundation | **Done.** Kernel, Result, IDs, event bus, DI container, boot state machine, input port, logging, scene loader. Boots cleanly (PlayMode smoke test). |
| 1 Cleaning vertical slice | **Logic done and tested; no art.** Player motor, interaction, tool matching, target HP, weighted progress, per-room / property completion, feedback hooks. `TestScenes/CleaningVerticalSlice.unity` exists as a skeleton and needs content + sprites. |
| 2 Jobs / economy | **Done** (job state machine, customer evaluation, reward, wallet, ledger, result presenter/view). Result *screen art* pending. |
| 3 Persistence | **Ports, coordinator, autosave, migrations and the full SQL schema are done; the SQLite provider is not chosen** (ADR-004). Everything runs on in-memory implementations behind the same ports. |
| 4 Progression | **Done** (completion counts, "6 completions" rule as data, job tiers, company unlock). |
| 5 Town / NPC / dialogue | **Domain + application done** (schedules, dialogue graph + session). Town scene is an empty shell; no dialogue UI, no condition resolvers / action executors (they fail closed). |
| 6 Relationships / gifts | **Done** (thresholds as data, mother is just an NPC id, milestone events). |
| 7 Company / employees | **Done** (hire, assign, passive deterministic simulation, revenue, upgrades). |
| 8 Quests / content pipeline | **Quests done** (generic objectives driven by events). Content validators + catalog done; graph/inspector tools not built. |
| 9 Production polish | Not started (localization package, accessibility UI, performance pass). |
| 10 Online services | Not started by design; only ports + offline-safe defaults exist. |

## Modules

| Module | Domain | Application | Infrastructure | Presentation | Tests |
|---|---|---|---|---|---|
| Cleaning | targets, session, result, progress calc, compatibility | start/apply/cancel, coordinator, facade | in-memory repos | target view/authoring/selector, controller, HUD + feedback presenters, scene root | yes |
| Equipment | progress, loadout, definition snapshot, policies | equip/switch/purchase/upgrade, selected-equipment query | in-memory (+save participant) | switch controller | yes |
| Inventory | inventory, item definitions | add/remove/get | in-memory | — | yes |
| Jobs | job state machine, evaluation, reward calc, events | accept/start/complete/abandon/finish, queries, coordinator | in-memory | — | yes |
| Locations | definitions, progress, unlock policies | queries, completion projection | in-memory (+save participant) | — | yes |
| Progression | player progression, rules | unlock/evaluate handlers, projection | in-memory (+save participant) | — | yes |
| Economy | wallet, transactions, purchase policy | wallet service, handlers, reward projection | in-memory (+save participant) | — | yes |
| GameTime | action-count calendar, phase/action policies, summary | consume/end day, summary projection, clock | in-memory (+save participant) | — | yes |
| Company | company, upgrades, level policy | create/upgrade/reputation, revenue projection | in-memory | — | yes |
| Employees | employee, assignment, simulator | hire/fire/assign/cancel/resolve, day projection | in-memory | — | yes |
| NPC | profile, schedule, abstract state | schedule projection | in-memory | view + schedule controller | via integration |
| Relationships | relationship, threshold sets, level policy | apply change, gift projection | in-memory (+save participant) | — | yes |
| Gifts | definitions, ownership, effects | purchase/give/available | in-memory | — | yes |
| Dialogue | graph, node, choice, condition/action, session | dialogue service | in-memory | — | via quests |
| Quests | definitions, instance, signals, evaluator | activate/claim, progress projection | in-memory (+save participant) | — | yes |
| Save | slots, schema version, events | coordinator, autosave, dirty tracker, migration runner | in-memory UoW/slots, ADO UoW, schema v1, initializer | lifecycle hooks | yes |
| Settings | audio/input/graphics/accessibility/language | update/get | in-memory + JSON file | — | via integration |
| Audio | — | ports, null service, settings projection | Unity AudioSource service, cue library | town music controller | via integration |
| Analytics | — | service port, buffered/null service, projection | — | — | via integration |
| Platform | — | platform/achievement/cloud-save ports | standalone platform service | — | — |
| World | — | scene loader port, game flow state machine + coordinator | `SceneManager` loader | — | via integration |
| DebugTools | — | command registry + cheats (compile-gated) | — | IMGUI debug panel (F1) | via integration |
| Player | profile, state machine | module | — | 2D motor | — |
| Interaction | `IInteractable`, context | — | — | selector, controller, prompt presenter | — |
| GameInput | — | `IGameplayInput`, intents | — | Input System adapter + default bindings | — |

Tests: EditMode unit tests per feature, `Bootstrap/Tests` (real module composition, end-to-end scenarios, content validation,
architecture rules) and `Bootstrap/PlayModeTests` (boots the real scenes).

## Deliberate differences from the document

| Topic | Document | Here | Why |
|---|---|---|---|
| C# features | records, `record struct`, nullable annotations | records + `init` via a shim in the Kernel (`IsExternalInit`); IDs are hand-written `readonly struct`s; no `T?`/`null!` | Unity 6000.6 compiles C# 9 (no `record struct`), nullable context is off. Verified by compiling. |
| Namespace `Time` | module "Time" | module / namespace / assembly **`GameTime`** | `CleanTime.Time` would shadow `UnityEngine.Time` for every file under `CleanTime.*`. |
| Module "Input" | `Input` | **`GameInput`** | Same shadowing problem with `UnityEngine.Input`. |
| Entity `Equipment` | `Equipment` class | **`EquipmentProgress`** | A class named like its module namespace (`CleanTime.Equipment`) cannot be referenced from the module's own `Application` layer. The doc's `EquipmentProgress` name is reused. |
| Entities `Company`, `Inventory` | `Company`, `Inventory` | **`CompanyProfile`**, **`PlayerInventory`** | Same namespace collision. |
| `Error?` in `Result` | nullable annotation | `Error?` (`Nullable<Error>`) works unchanged | — |
| `CleaningTarget` ctor | 4 args | 5th optional `roomId` | Per-room progress/completion. |
| `ILocationUnlockPolicy` | `IsSatisfied` | + `RequirementType` | A policy must say which requirement type it evaluates so an evaluator can dispatch. |
| `ISaveCoordinator`, `SaveCoordinator` | hard-coded 4 repositories | iterates all registered `ISaveParticipant`s | Save must not know other modules' repositories; adding a module = registering a participant. |
| Purchase handlers | `PurchaseEquipment/Gift/CompanyUpgradeHandler` in Economy | In Equipment / Gifts / Company, calling the public `IWalletService.Pay` | Keeps each module owning its own rules; Economy owns money only. |
| `MotherStoryMilestoneReachedEvent` | mother-specific | generic `RelationshipMilestoneReachedEvent` | §38: the mother is not a special system. |
| Day-end autosave | on "game day ended" | on `GameDayStarted` | The day is fully settled (employees, revenue, summary) only after it ended. |
| `Repair/Pickup/RemoveInsect` commands | separate commands | not implemented | They are `ApplyCleaningAction` on different target types; add only if their rules diverge. |
| Addressables, Localization, VContainer, SQLite | packages | not installed | See ADR-003/004/006/012. No assets yet. |
| Test Framework | — | `com.unity.test-framework` 1.6.0 added to `Packages/manifest.json` | Needed for the tests. |

Classes in §169 that were intentionally not created (no behaviour yet): `CleaningRuleSet`, `ICleaningRule`, `CleaningPower`,
`CleaningObjective`, `RoomCleaningState`, `PropertyCleaningState`, `EquipmentStats`, `EquipmentUpgrade` (folded into
`EquipmentProgress`/definition), `Price`, `Purchase`, `CompanyReputation`/`CompanyStats`/`CompanyLevel` (fields on `CompanyProfile`),
`GameDay` (the calendar owns the day), `INpcWorldAdapter` (events are the port), `IDialogueRepository`-backed unlocks,
`SqliteConnectionFactory`/`SqliteUnitOfWork`/`SqliteSaveSlotRepository`, `AutosavePolicy` (a fixed list in `AutosaveService`),
`SaveSlotPresenter`, `UiAudioAdapter`, `MusicController` (→ `TownMusicController`), `BootErrorPresenter` is IMGUI.

## Placeholders to replace with design decisions

- Progression: company unlocks after **18** jobs; job tiers at 6/12/24/36 jobs (`ProgressionSettings`).
- Game time: **1 action per phase**, only a completed job costs an action (`ActionsPerPhaseDayPhasePolicy`, `DefaultGameActionPolicy`).
- Customer evaluation weights 0.6/0.2/0.1/0.1, reward multiplier 0.5×–1.25× (`CustomerEvaluationWeights`, `QualityScaledRewardCalculator`);
  mistake tolerance 10 wrong-tool attempts. Optional objectives are always counted as complete.
- Employees: duration = `max(1, difficulty - speed/25)` days; revenue = 100 % of the simulated reward.
- Relationship thresholds 20/60/120/200/300; company level thresholds 50/150/400/900 reputation.
- Starting money 0 (`BootstrapSettings.startingMoneyCents`).

## Known gaps / next steps

1. Create starter content (dirt, tools, locations, jobs) and the content catalog, then play `CleaningVerticalSlice`.
2. Choose and wire the SQLite provider (ADR-004), add SQL repositories, mid-job save decision (ADR-013).
3. Participants for state that is not yet saved: active job / cleaning session, job history, gift ownership, inventory,
   company, employees, assignments, ledger, dialogue "seen" flags.
4. Main menu / save slot / settings screens (presenters not written), localization package, Addressables.
5. CI: run `Tools/run-tests.sh` and `Tools/validate-content.sh`; add `-nographics` Windows/Linux runners as needed.
