using System.Collections.Generic;
using UnityEngine;
using CleanTime.Analytics.Application;
using CleanTime.Audio.Application;
using CleanTime.Audio.Infrastructure;
using CleanTime.Cleaning.Application;
using CleanTime.Cleaning.Infrastructure;
using CleanTime.Company.Application;
using CleanTime.Company.Infrastructure;
using CleanTime.Content;
using CleanTime.DebugTools.Application;
using CleanTime.Dialogue.Application;
using CleanTime.Dialogue.Infrastructure;
using CleanTime.Economy.Application;
using CleanTime.Economy.Infrastructure;
using CleanTime.Employees.Application;
using CleanTime.Employees.Infrastructure;
using CleanTime.Equipment.Application;
using CleanTime.Equipment.Infrastructure;
using CleanTime.GameTime.Application;
using CleanTime.GameTime.Infrastructure;
using CleanTime.Gifts.Application;
using CleanTime.Gifts.Infrastructure;
using CleanTime.Inventory.Application;
using CleanTime.Inventory.Infrastructure;
using CleanTime.Jobs.Application;
using CleanTime.Jobs.Infrastructure;
using CleanTime.Locations.Application;
using CleanTime.Locations.Infrastructure;
using CleanTime.NPC.Application;
using CleanTime.NPC.Infrastructure;
using CleanTime.Platform.Application;
using CleanTime.Platform.Infrastructure;
using CleanTime.Player.Application;
using CleanTime.Progression.Application;
using CleanTime.Progression.Infrastructure;
using CleanTime.Quests.Application;
using CleanTime.Quests.Infrastructure;
using CleanTime.Relationships.Application;
using CleanTime.Relationships.Infrastructure;
using CleanTime.Save.Application;
using CleanTime.Save.Infrastructure;
using CleanTime.Settings.Application;
using CleanTime.Settings.Infrastructure;
using CleanTime.Shared.Kernel;
using CleanTime.World.Application;
using CleanTime.World.Infrastructure;

namespace CleanTime.Bootstrap
{
    /// <summary>
    /// The one place that knows every module. Order matters in two ways:
    /// <list type="bullet">
    /// <item>Registration: a later registration of the same service overrides an earlier one, so
    /// infrastructure modules come after the application modules whose defaults they replace.</item>
    /// <item>Initialization: modules subscribe to events in list order, which fixes the order handlers
    /// run in (rewards, then progression, then time/day-end, then autosave last).</item>
    /// </list>
    /// </summary>
    public static class FeatureRegistration
    {
        /// <summary>Everything the game needs at runtime: gameplay modules followed by Unity platform adapters.</summary>
        public static IReadOnlyList<IFeatureModule> CreateModules(
            BootstrapSettings settings,
            ContentRegistry content,
            string settingsFilePath,
            Transform persistentRoot)
        {
            var modules = new List<IFeatureModule>(CreateGameplayModules(settings, content));
            modules.AddRange(CreatePlatformModules(settings, settingsFilePath, persistentRoot));
            return modules;
        }

        /// <summary>
        /// Content, all application modules (in event-subscription order) and the in-memory infrastructure
        /// modules. Free of scene / audio / file system adapters, so tests can compose the real game from it.
        /// </summary>
        public static IReadOnlyList<IFeatureModule> CreateGameplayModules(BootstrapSettings settings, ContentRegistry content)
        {
            return new IFeatureModule[]
            {
                new ContentModule(content),

                // Application modules, in event-subscription order.
                new EconomyModule(),
                new LocationsModule(),
                new ProgressionModule(),
                new CleaningModule(),
                new JobsModule(),
                new EquipmentModule(),
                new InventoryModule(),
                new QuestsModule(),
                new CompanyModule(),
                new RelationshipsModule(),
                new GiftsModule(),
                new EmployeesModule(),
                new NpcModule(),
                new GameTimeModule(),
                new DialogueModule(),
                new PlayerModule(),
                new AudioModule(),
                new SettingsModule(),
                new AnalyticsModule(),
                new PlatformModule(),
                new WorldModule(),
                new DebugToolsModule(),
                new SaveModule(),

                // Infrastructure modules; they register after the application defaults they override.
                new EconomyInfrastructureModule(new Money(settings.startingMoneyCents)),
                new LocationsInfrastructureModule(),
                new ProgressionInfrastructureModule(),
                new CleaningInfrastructureModule(),
                new JobsInfrastructureModule(),
                new EquipmentInfrastructureModule(),
                new InventoryInfrastructureModule(),
                new QuestsInfrastructureModule(),
                new CompanyInfrastructureModule(),
                new RelationshipsInfrastructureModule(),
                new GiftsInfrastructureModule(),
                new EmployeesInfrastructureModule(),
                new NpcInfrastructureModule(),
                new GameTimeInfrastructureModule(),
                new DialogueInfrastructureModule(),
                new SaveInfrastructureModule(),
            };
        }

        /// <summary>Unity-specific adapters: settings file, platform info, scene loading, audio output.</summary>
        public static IReadOnlyList<IFeatureModule> CreatePlatformModules(
            BootstrapSettings settings,
            string settingsFilePath,
            Transform persistentRoot)
        {
            return new IFeatureModule[]
            {
                new SettingsInfrastructureModule(settings.persistSettings ? settingsFilePath : null),
                new PlatformInfrastructureModule(),
                new WorldInfrastructureModule(new SceneNames
                {
                    MainMenu = settings.mainMenuSceneName,
                    Town = settings.townSceneName,
                }),
                new AudioInfrastructureModule(settings.audioCues, persistentRoot),
            };
        }
    }
}
