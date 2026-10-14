# Clean Time

2D top-down cleaning simulation (Unity 6, C#, PC first, mobile-ready). The full design of the codebase is in
[`Docs/TECHNICAL_ARCHITECTURE.md`](Docs/TECHNICAL_ARCHITECTURE.md); what is built so far, and where the code
deviates from that document, is in [`Docs/IMPLEMENTATION_STATUS.md`](Docs/IMPLEMENTATION_STATUS.md).

## Status

The **structure** is in place and runs: all modules, composition root, boot sequence, in-memory persistence,
editor tools and 240+ automated tests. There is **no art, audio or authored content yet** — everything that
needs assets is wired through ports and waits for them (see "Connecting assets").

## Open and run

1. Open the project with Unity **6000.6.4f1**.
2. Open `Assets/_Project/Scenes/Bootstrap.unity` and press Play. The boot sequence runs, the services are
   composed and the (empty) MainMenu scene is loaded additively. The console shows `event=boot_started ... services_ready`.
3. F1 toggles the developer overlay (editor / development builds only).

The Unity template content that shipped with the project (`Assets/SourceFiles`, `Assets/Tutorials`, `Assets/Scenes`, ...)
is untouched and not referenced by the game; delete it whenever you like.

## Layout

```text
Assets/_Project/
  Bootstrap/        composition root: GameBootstrapper, boot steps, scopes, FeatureRegistration, integration tests
  Shared/Kernel/    Result, strong IDs, Money, event bus, DI container, state machine, logging (no Unity)
  Shared/Unity/     Unity adapters for the Kernel (logger, JSON, frame ticker)
  Features/<X>/     Domain | Application | Infrastructure | Presentation | Tests   (one assembly each)
  Content/          ScriptableObject definitions, content catalog, registry (+ Sprites/Audio/Prefabs... for later)
  UI/               HUD, JobResult, MainMenu, ... (views + presenters)
  Scenes/           Bootstrap, MainMenu, Town/, Properties/, TestScenes/
  Editor/           content + property validators, scene setup, inspectors
Docs/               architecture, ADRs, status, save format, content authoring, test strategy
Tools/              CI scripts (tests, content validation, scene setup)
```

Feature modules: Player, Interaction, GameInput, Cleaning, Equipment, Inventory, Jobs, Locations, Progression,
Economy, GameTime, Company, Employees, NPC, Relationships, Gifts, Dialogue, Quests, World, Save, Settings,
Audio, Analytics, Platform, DebugTools.

## The rules, in short

- `*.Domain` and `*.Application` assemblies cannot reference Unity (`noEngineReferences`). `ArchitectureTests` also
  checks that dependencies only point inwards.
- Commands change state, queries read it, **events** notify other modules. A module never reaches into another
  module's storage.
- MonoBehaviours only adapt Unity (input, transforms, sprites, UI). Scene components receive services in
  `ISceneService.Construct`, **not** in `Awake`/`Start`.
- A MonoBehaviour / ScriptableObject must live in a file named after the class (Unity requirement; tested).
- Business ids are strongly typed strings (`equipment.broom.basic`). Never persist scene names or instance ids.
- Text is a localization key, money is integer cents, randomness is an injected `IRandomSource` with a stored seed.

## Common tasks

Run tests / validate content (batch mode, usable in CI):

```bash
Tools/run-tests.sh editmode     # unit + integration + architecture tests
Tools/run-tests.sh playmode     # boots the real Bootstrap scene
Tools/validate-content.sh       # content catalog + orphan check
Tools/create-base-scenes.sh     # (re)creates Bootstrap / MainMenu / Town / PropertyShell / test scene
```

Inside the editor: `Clean Time > Validate Content`, `Clean Time > Properties > ...`, `Clean Time > Setup > Create Base Scenes`,
and the Test Runner window.

Add a feature or change behaviour: follow the checklist in [`Docs/CONTENT_AUTHORING.md`](Docs/CONTENT_AUTHORING.md) (content)
and "Definition of done" in the architecture document §157 (code).

## Connecting assets (next step)

| When you have ... | Do this |
|---|---|
| Dirt / tool / location / job definitions | Create the ScriptableObjects (`Assets > Create > Clean Time`), list them in a `GameContentCatalog`, assign it on `GameBootstrapper > Settings > Content Catalog`, run `Clean Time > Validate Content`. |
| Property scenes and sprites | Copy `Scenes/Properties/PropertyShell.unity`, add `CleaningTargetAuthoring` objects under `Targets`, set the `CleaningSceneRoot` location id, set the location's scene key to the scene name, add the scene to Build Settings. |
| UI art | Build Canvases using the views in `UI/HUD`, `UI/JobResult`; assign them on the `HudBinder` / `JobResultBinder`. |
| Audio clips | Create an `AudioCueLibrary` and assign it on `GameBootstrapper > Settings > Audio Cues` (cue ids are in `AudioCues`). |
| Addressables / Localization | See ADR-006 and ADR-012. |
