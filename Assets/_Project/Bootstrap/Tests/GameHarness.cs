using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using CleanTime.Bootstrap;
using CleanTime.Cleaning.Application;
using CleanTime.Content;
using CleanTime.Equipment.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Locations.Domain;
using CleanTime.NPC.Domain;
using CleanTime.Quests.Domain;
using CleanTime.Settings.Infrastructure;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;
using CleanTime.World.Application;

namespace CleanTime.Bootstrap.Tests
{
    /// <summary>Builds throw-away ScriptableObject content for tests (private serialized fields set by reflection).</summary>
    internal static class TestAssets
    {
        public static T Make<T>(params (string Field, object Value)[] fields)
            where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = typeof(T).Name;
            Set(asset, fields);
            return asset;
        }

        public static void Set(object target, params (string Field, object Value)[] fields)
        {
            foreach (var (field, value) in fields)
            {
                var info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (info == null)
                    throw new MissingFieldException(target.GetType().Name, field);

                info.SetValue(target, value);
            }
        }

        public static DirtDefinition Dirt(string id, float health, float weight) =>
            Make<DirtDefinition>(("id", id), ("defaultHealth", health), ("defaultWeight", weight));

        public static EquipmentDefinition Tool(string id, long price, float power, bool starter, params string[] targets) =>
            Make<EquipmentDefinition>(
                ("id", id), ("priceCents", price), ("baseCleaningPower", power), ("isStarter", starter),
                ("supportedTargetIds", targets), ("maxUpgradeLevel", 2), ("powerPerUpgradeLevel", 1f));

        public static LocationDefinitionAsset Location(string id, string sceneKey, bool unlocked, params (string Location, int Count)[] requires)
        {
            var requirements = new LocationDefinitionAsset.UnlockRequirement[requires.Length];

            for (var i = 0; i < requires.Length; i++)
            {
                requirements[i] = new LocationDefinitionAsset.UnlockRequirement
                {
                    type = LocationRequirementTypes.CompletionCount,
                    requiredLocationId = requires[i].Location,
                    requiredCompletionCount = requires[i].Count,
                };
            }

            return Make<LocationDefinitionAsset>(("id", id), ("sceneKey", sceneKey), ("unlockedByDefault", unlocked), ("unlockRequirements", requirements));
        }

        public static JobDefinitionAsset Job(string id, string location, long reward, int difficulty = 1) =>
            Make<JobDefinitionAsset>(("id", id), ("locationId", location), ("customerId", "npc.customer.001"),
                ("baseRewardCents", reward), ("difficulty", difficulty));

        public static EmployeeDefinitionAsset Employee(string id, long hireCost) =>
            Make<EmployeeDefinitionAsset>(("id", id), ("hireCostCents", hireCost), ("cleaningSkill", 80), ("speedSkill", 50), ("reliabilitySkill", 50));

        public static GiftDefinitionAsset Gift(string id, string npc, long price, int gain) =>
            Make<GiftDefinitionAsset>(("id", id), ("recipientNpcId", npc), ("priceCents", price), ("relationshipGain", gain));

        public static QuestDefinitionAsset Quest(string id, QuestObjectiveType type, string key, int count, long reward, bool active)
        {
            var objectives = new[] { new QuestDefinitionAsset.Objective { type = type, targetKey = key, requiredCount = count } };
            return Make<QuestDefinitionAsset>(("id", id), ("objectives", objectives), ("rewardCents", reward), ("activeByDefault", active));
        }

        public static NpcDefinitionAsset Npc(string id) => Make<NpcDefinitionAsset>(("id", id));

        public static GameContentCatalog Catalog(
            DirtDefinition[] dirt = null,
            EquipmentDefinition[] equipment = null,
            LocationDefinitionAsset[] locations = null,
            JobDefinitionAsset[] jobs = null,
            EmployeeDefinitionAsset[] employees = null,
            GiftDefinitionAsset[] gifts = null,
            QuestDefinitionAsset[] quests = null,
            NpcDefinitionAsset[] npcs = null)
        {
            return Make<GameContentCatalog>(
                ("dirt", dirt ?? new DirtDefinition[0]),
                ("equipment", equipment ?? new EquipmentDefinition[0]),
                ("locations", locations ?? new LocationDefinitionAsset[0]),
                ("jobs", jobs ?? new JobDefinitionAsset[0]),
                ("employees", employees ?? new EmployeeDefinitionAsset[0]),
                ("gifts", gifts ?? new GiftDefinitionAsset[0]),
                ("quests", quests ?? new QuestDefinitionAsset[0]),
                ("npcs", npcs ?? new NpcDefinitionAsset[0]));
        }

        /// <summary>The small game used by the integration tests.</summary>
        public static GameContentCatalog StandardCatalog() => Catalog(
            dirt: new[] { Dirt(Ids.Dust, 10f, 1f), Dirt(Ids.Stain, 10f, 2f) },
            equipment: new[]
            {
                Tool(Ids.Broom, 0, 5f, true, Ids.Dust),
                Tool(Ids.Mop, 0, 5f, true, Ids.Stain),
                Tool(Ids.Vacuum, 5000, 10f, false, Ids.Dust),
            },
            locations: new[]
            {
                Location(Ids.SmallLocation, "Property.Small", true),
                Location(Ids.LargeLocation, "Property.Large", false, (Ids.SmallLocation, 6)),
            },
            jobs: new[]
            {
                Job(Ids.SmallJob, Ids.SmallLocation, 25000),
                Job(Ids.LargeJob, Ids.LargeLocation, 60000, difficulty: 3),
            },
            employees: new[] { Employee(Ids.Rookie, 15000) },
            gifts: new[] { Gift(Ids.Cup, Ids.Mother, 2000, 25) },
            quests: new[]
            {
                Quest(Ids.StarterQuest, QuestObjectiveType.CompleteJob, string.Empty, 1, 1000, active: true),
                Quest(Ids.MotherQuest, QuestObjectiveType.ReachRelationshipLevel, Ids.Mother, 1, 0, active: true),
            },
            npcs: new[] { Npc(Ids.Mother) });
    }

    internal static class Ids
    {
        public const string Dust = "dirt.dust";
        public const string Stain = "dirt.stain";
        public const string Broom = "equipment.broom.basic";
        public const string Mop = "equipment.mop.basic";
        public const string Vacuum = "equipment.vacuum.tier1";
        public const string SmallLocation = "location.apartment.small.001";
        public const string LargeLocation = "location.apartment.large.001";
        public const string SmallJob = "job.small_apartment.001";
        public const string LargeJob = "job.large_apartment.001";
        public const string Rookie = "employee.rookie";
        public const string Mother = "npc.mother";
        public const string Cup = "gift.mother.cup.001";
        public const string StarterQuest = "quest.starter";
        public const string MotherQuest = "quest.mother_level_1";
    }

    internal sealed class FakeSceneLoader : ISceneLoader
    {
        public System.Collections.Generic.List<string> Calls { get; } = new System.Collections.Generic.List<string>();

        public Task LoadMainMenuAsync() { Calls.Add("menu"); return Task.CompletedTask; }

        public Task LoadTownAsync() { Calls.Add("town"); return Task.CompletedTask; }

        public Task LoadPropertyAsync(LocationId locationId) { Calls.Add("property:" + locationId.Value); return Task.CompletedTask; }

        public Task UnloadCurrentGameplaySceneAsync() { Calls.Add("unload"); return Task.CompletedTask; }
    }

    /// <summary>
    /// The real game, composed through <see cref="FeatureRegistration"/> exactly like production, minus the
    /// Unity adapters (scene loading, audio, files). Time and ids are fakes so runs are deterministic.
    /// </summary>
    internal sealed class GameHarness : IDisposable
    {
        private readonly GameObject _root = new GameObject("TestRoot");

        private GameHarness(ServiceContainer services, EventBus bus)
        {
            Services = services;
            Bus = bus;
        }

        public ServiceContainer Services { get; }

        public IEventBus Events => Bus;

        public EventBus Bus { get; }

        public FakeSceneLoader Scenes { get; private set; }

        public static GameHarness Create(GameContentCatalog catalog = null, long startingMoneyCents = 20000)
        {
            var settings = new BootstrapSettings { startingMoneyCents = startingMoneyCents, persistSettings = false };
            var registry = ContentRegistry.Build(catalog ?? TestAssets.StandardCatalog());
            if (registry.IsFailure)
                throw new InvalidOperationException(registry.Error.Value.Message);

            var services = new ServiceContainer();
            var logger = new RecordingLogger();
            var clock = new FakeUtcClock();
            var bus = (EventBus)CoreServices.Register(services, logger, clock, new SeededRandomSource(42), new SequentialIdGenerator());
            var scenes = new FakeSceneLoader();
            services.RegisterInstance<ISceneLoader>(scenes);

            var harness = new GameHarness(services, bus) { Scenes = scenes };
            harness.UseClock(clock);
            harness.UseLogger(logger);

            var modules = new System.Collections.Generic.List<IFeatureModule>(FeatureRegistration.CreateGameplayModules(settings, registry.Value))
            {
                new SettingsInfrastructureModule(),
            };

            foreach (var module in modules)
                module.Register(services);

            foreach (var module in modules)
                module.Initialize(services);

            return harness;
        }

        public T Resolve<T>() => Services.Resolve<T>();

        public Result Send<TCommand>(TCommand command)
            where TCommand : ICommand =>
            Services.Resolve<ICommandHandler<TCommand>>().Handle(command);

        public TResult Ask<TQuery, TResult>(TQuery query)
            where TQuery : IQuery<TResult> =>
            Services.Resolve<IQueryHandler<TQuery, TResult>>().Handle(query);

        public void Dispose()
        {
            Services.Dispose();
            UnityEngine.Object.DestroyImmediate(_root);
        }

        // The clock / logger registered with the container are the same instances exposed here.
        private void UseClock(FakeUtcClock clock) => _clock = clock;

        private void UseLogger(RecordingLogger logger) => _logger = logger;

        private FakeUtcClock _clock;
        private RecordingLogger _logger;

        public FakeUtcClock UtcClock => _clock;

        public RecordingLogger Log => _logger;
    }
}
