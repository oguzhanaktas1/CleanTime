# Content authoring

All static game data is ScriptableObjects under `Assets/_Project/Content/Definitions/<kind>/`, referenced from **one**
`GameContentCatalog` that is assigned on the `GameBootstrapper` (`Settings > Content Catalog`). Definitions are converted to immutable
snapshots at boot (`ContentRegistry`); domain code never sees ScriptableObjects.

## Creating content

`Assets > Create > Clean Time > ...`

| Asset | Naming | Id example |
|---|---|---|
| Dirt Definition | `SO_Dirt_Dust` | `dirt.dust` |
| Equipment Definition | `SO_Equipment_Broom_Basic` | `equipment.broom.basic` |
| Location Definition | `SO_Location_SmallApartment_001` | `location.apartment.small.001` |
| Job Definition | `SO_Job_SmallApartment_001` | `job.small_apartment.001` |
| Employee / Gift / Quest / NPC / Company Upgrade | `SO_Employee_...` | `employee.rookie`, `gift.mother.cup.001`, `quest.customer.001`, `npc.mother`, `upgrade.office` |

- The **id** is the business identity stored in saves; the asset name is just a label. Never rename an id after saves exist.
- Money fields are integer cents. Text fields are localization keys.
- Equipment lists the dirt ids it can handle (`supportedTargetIds`); matching is data, not code.
- A location unlocks via requirements, e.g. `location_completion_count` on `location.apartment.small.001` with count `6`.
  A location with no requirements follows `unlockedByDefault`.
- Add the asset to the catalog, then run **Clean Time > Validate Content** (or `Tools/validate-content.sh`). It reports
  duplicate / empty ids, equipment for unknown dirt, jobs at unknown locations, unknown unlock references, unlock cycles and
  assets that are not in the catalog.

## Authoring a property

1. Copy `Scenes/Properties/PropertyShell.unity` to `Scenes/Properties/<Name>.unity`.
2. Set `CleaningSceneRoot > Location Id` to the location's id.
3. Under `Targets`, add objects with `CleaningTargetView` + `CleaningTargetAuthoring` (a stable id is generated when the component is added).
   Pick the dirt definition, optionally override weight / health, optionally set a room id.
4. Run **Clean Time > Properties > Validate Open Scene**. After duplicating objects use **Fix Duplicate Target Ids**.
5. Put the scene's name in the location definition's `Scene Key` and add the scene to Build Settings.

Ids are generated once and must stay stable (mid-job saves will reference them).

## Adding a new feature module

1. Create `Features/<Name>/{Domain,Application,Infrastructure,Presentation,Tests}` and copy the `.asmdef` + `AssemblyInfo.cs` pair of an existing
   module for each layer (`CleanTime.<Name>.<Layer>`; Domain and Application keep `noEngineReferences`; Tests are Editor-only and reference
   `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `nunit.framework.dll`).
2. Domain: entities, value objects, ports, events. Application: commands/queries/handlers (internal), a public `FeatureModule`.
   Infrastructure: adapters and a module that registers them. Register both in `FeatureRegistration`.
3. If it owns saved state implement `ISaveParticipant` and register it.
4. Tests (EditMode), then an integration scenario in `Bootstrap/Tests` if it talks to other modules.
5. `ArchitectureTests` will tell you if a dependency points the wrong way.
