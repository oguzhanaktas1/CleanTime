using System;
using System.Collections.Generic;
using System.Linq;
using CleanTime.Company.Domain;
using CleanTime.Dialogue.Domain;
using CleanTime.Employees.Domain;
using CleanTime.Equipment.Domain;
using CleanTime.Gifts.Domain;
using CleanTime.Inventory.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Locations.Domain;
using CleanTime.NPC.Domain;
using CleanTime.Quests.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Content
{
    /// <summary>Id-keyed, read-only view of one kind of static definition.</summary>
    public class DefinitionTable<TId, TDefinition>
        where TId : struct
    {
        private readonly Dictionary<TId, TDefinition> _items;

        public DefinitionTable(IEnumerable<TDefinition> items, Func<TDefinition, TId> idOf)
        {
            _items = new Dictionary<TId, TDefinition>();

            foreach (var item in items)
                _items[idOf(item)] = item;
        }

        public Result<TDefinition> Get(TId id) => _items.TryGetValue(id, out var item)
            ? Result<TDefinition>.Success(item)
            : Result<TDefinition>.Failure("content.not_found", $"No definition with id '{id}'.");

        public IReadOnlyCollection<TDefinition> GetAll() => _items.Values;
    }

    internal sealed class EquipmentProvider : DefinitionTable<EquipmentId, EquipmentDefinitionSnapshot>, IEquipmentDefinitionProvider
    {
        public EquipmentProvider(IEnumerable<EquipmentDefinitionSnapshot> items) : base(items, x => x.Id) { }
    }

    internal sealed class JobProvider : DefinitionTable<JobDefinitionId, JobDefinitionSnapshot>, IJobDefinitionProvider
    {
        public JobProvider(IEnumerable<JobDefinitionSnapshot> items) : base(items, x => x.Id) { }
    }

    internal sealed class LocationProvider : DefinitionTable<LocationId, LocationDefinitionSnapshot>, ILocationDefinitionProvider
    {
        public LocationProvider(IEnumerable<LocationDefinitionSnapshot> items) : base(items, x => x.Id) { }
    }

    internal sealed class EmployeeProvider : DefinitionTable<EmployeeDefinitionId, EmployeeDefinitionSnapshot>, IEmployeeDefinitionProvider
    {
        public EmployeeProvider(IEnumerable<EmployeeDefinitionSnapshot> items) : base(items, x => x.Id) { }
    }

    internal sealed class GiftProvider : DefinitionTable<GiftId, GiftDefinitionSnapshot>, IGiftDefinitionProvider
    {
        public GiftProvider(IEnumerable<GiftDefinitionSnapshot> items) : base(items, x => x.Id) { }
    }

    internal sealed class QuestProvider : DefinitionTable<QuestId, QuestDefinitionSnapshot>, IQuestDefinitionProvider
    {
        public QuestProvider(IEnumerable<QuestDefinitionSnapshot> items) : base(items, x => x.Id) { }
    }

    internal sealed class NpcProvider : DefinitionTable<NpcId, NpcProfile>, INpcDefinitionProvider
    {
        public NpcProvider(IEnumerable<NpcProfile> items) : base(items, x => x.Id) { }
    }

    internal sealed class CompanyUpgradeProvider : DefinitionTable<CompanyUpgradeId, CompanyUpgradeDefinition>, ICompanyDefinitionProvider
    {
        public CompanyUpgradeProvider(IEnumerable<CompanyUpgradeDefinition> items) : base(items, x => x.Id) { }
    }

    internal sealed class ItemProvider : DefinitionTable<ItemId, ItemDefinitionSnapshot>, IItemDefinitionProvider
    {
        public ItemProvider(IEnumerable<ItemDefinitionSnapshot> items) : base(items, x => x.Id) { }
    }

    internal sealed class DialogueProvider : DefinitionTable<DialogueId, DialogueGraph>, IDialogueDefinitionProvider
    {
        public DialogueProvider(IEnumerable<DialogueGraph> items) : base(items, x => x.Id) { }
    }

    internal sealed class AuthoredNpcScheduleProvider : INpcScheduleProvider
    {
        private readonly Dictionary<NpcId, NpcSchedule> _schedules;

        public AuthoredNpcScheduleProvider(Dictionary<NpcId, NpcSchedule> schedules)
        {
            _schedules = schedules;
        }

        public NpcSchedule GetSchedule(NpcId npcId) =>
            _schedules.TryGetValue(npcId, out var schedule) ? schedule : new NpcSchedule(null, "home");
    }

    /// <summary>
    /// Runtime snapshots of every authored definition, built once at boot. Domain code only ever sees
    /// these immutable snapshots, never ScriptableObjects.
    /// </summary>
    public sealed class ContentRegistry
    {
        internal ContentRegistry(GameContentCatalog catalog)
        {
            Equipment = new EquipmentProvider(catalog.Equipment.Select(x => x.ToSnapshot()));
            Jobs = new JobProvider(catalog.Jobs.Select(x => x.ToSnapshot()));
            Locations = new LocationProvider(catalog.Locations.Select(x => x.ToSnapshot()));
            Employees = new EmployeeProvider(catalog.Employees.Select(x => x.ToSnapshot()));
            Gifts = new GiftProvider(catalog.Gifts.Select(x => x.ToSnapshot()));
            Quests = new QuestProvider(catalog.Quests.Select(x => x.ToSnapshot()));
            Npcs = new NpcProvider(catalog.Npcs.Select(x => x.ToProfile()));
            NpcSchedules = new AuthoredNpcScheduleProvider(catalog.Npcs.ToDictionary(x => new NpcId(x.Id), x => x.ToSchedule()));
            CompanyUpgrades = new CompanyUpgradeProvider(catalog.CompanyUpgrades.Select(x => x.ToDefinition()));
            Items = new ItemProvider(Array.Empty<ItemDefinitionSnapshot>());
            Dialogues = new DialogueProvider(Array.Empty<DialogueGraph>());
        }

        internal IEquipmentDefinitionProvider Equipment { get; }

        internal IJobDefinitionProvider Jobs { get; }

        internal ILocationDefinitionProvider Locations { get; }

        internal IEmployeeDefinitionProvider Employees { get; }

        internal IGiftDefinitionProvider Gifts { get; }

        internal IQuestDefinitionProvider Quests { get; }

        internal INpcDefinitionProvider Npcs { get; }

        internal INpcScheduleProvider NpcSchedules { get; }

        internal ICompanyDefinitionProvider CompanyUpgrades { get; }

        internal IItemDefinitionProvider Items { get; }

        internal IDialogueDefinitionProvider Dialogues { get; }

        /// <summary>Validates the catalog and builds the registry. Fails with every problem found, not just the first.</summary>
        public static Result<ContentRegistry> Build(GameContentCatalog catalog)
        {
            var issues = ContentValidation.Validate(catalog);
            if (issues.Count > 0)
                return Result<ContentRegistry>.Failure("content.invalid", string.Join("\n", issues));

            try
            {
                return Result<ContentRegistry>.Success(new ContentRegistry(catalog));
            }
            catch (Exception ex)
            {
                return Result<ContentRegistry>.Failure("content.invalid", ex.Message);
            }
        }
    }

    public sealed class ContentModule : FeatureModule
    {
        private readonly ContentRegistry _registry;

        public ContentModule(ContentRegistry registry)
        {
            _registry = registry;
        }

        public override string Name => "Content";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterInstance(_registry.Equipment);
            services.RegisterInstance(_registry.Jobs);
            services.RegisterInstance(_registry.Locations);
            services.RegisterInstance(_registry.Employees);
            services.RegisterInstance(_registry.Gifts);
            services.RegisterInstance(_registry.Quests);
            services.RegisterInstance(_registry.Npcs);
            services.RegisterInstance(_registry.NpcSchedules);
            services.RegisterInstance(_registry.CompanyUpgrades);
            services.RegisterInstance(_registry.Items);
            services.RegisterInstance(_registry.Dialogues);
        }
    }
}
