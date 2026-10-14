# Data ownership

One module owns each piece of state; others read it through queries / public contracts or react to events (architecture §133-134, §141).

| Data | Owner | Read by others via |
|---|---|---|
| Money, ledger | Economy | `IWalletService`, `GetPlayerWalletQuery`, money events |
| Cleaning target health, session | Cleaning | `ICleaningReadService`, cleaning events |
| Owned / selected equipment | Equipment | `ISelectedEquipmentQuery`, equipment events |
| Inventory quantities | Inventory | `GetInventoryQuery` |
| Active job, job history, pending result | Jobs | `GetActiveJobQuery`, job events |
| Location unlocks and completion counts | Locations | `GetUnlockedLocationsQuery`, `LocationCompletionChangedEvent` |
| Total jobs, company unlock, story milestones, job tier | Progression | `IProgressionRepository` (read), progression events |
| Game date and phase | GameTime | `IGameClock`, `GetCurrentGameDateQuery` |
| Company, upgrades | Company | `GetCompanyOverviewQuery` |
| Employees, assignments | Employees | `GetEmployeesQuery`, employee events |
| NPC schedule state | NPC | `NpcScheduleNodeChangedEvent` |
| Relationship points / levels | Relationships | `GetRelationshipQuery`, relationship events |
| Gift ownership | Gifts | `GetAvailableGiftsQuery`, gift events |
| Quest state | Quests | `GetActiveQuestsQuery`, quest events |
| Save slots, schema version | Save | `GetSaveSlotsQuery`, `ISaveCoordinator` |
| Settings | Settings | `GetSettingsQuery`, `SettingsChangedEvent` |
| UI state | Presentation | — |
| Static definitions | Content registry | `I*DefinitionProvider` ports |

Identity: strongly typed string ids (`Shared/Kernel/Runtime/IDs`), e.g. `LocationId("location.apartment.small.001")`. Money: `Money(long Cents)`.
