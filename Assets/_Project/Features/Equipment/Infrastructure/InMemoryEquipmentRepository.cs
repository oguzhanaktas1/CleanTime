using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CleanTime.Equipment.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Equipment.Infrastructure
{
    public sealed class InMemoryEquipmentRepository : IEquipmentRepository, ISaveParticipant
    {
        private readonly IEquipmentDefinitionProvider _definitions;
        private readonly ISnapshotStore _snapshots;
        private Dictionary<EquipmentId, EquipmentProgress> _equipment;
        private EquipmentLoadout _loadout;

        public InMemoryEquipmentRepository(IEquipmentDefinitionProvider definitions, ISnapshotStore snapshots)
        {
            _definitions = definitions;
            _snapshots = snapshots;
        }

        public string Key => "equipment.progress";

        public Result<EquipmentProgress> Get(EquipmentId id)
        {
            EnsureSeeded();

            return _equipment.TryGetValue(id, out var item)
                ? Result<EquipmentProgress>.Success(item)
                : Result<EquipmentProgress>.Failure(EquipmentErrors.NotFound, $"Unknown equipment '{id}'.");
        }

        public Result<EquipmentProgress> GetOwned(EquipmentId id)
        {
            var found = Get(id);
            if (found.IsFailure)
                return found;

            return found.Value.IsOwned
                ? found
                : Result<EquipmentProgress>.Failure(EquipmentErrors.NotOwned, $"'{id}' is not owned.");
        }

        public IReadOnlyList<EquipmentProgress> GetAll()
        {
            EnsureSeeded();
            return _equipment.Values.ToList();
        }

        public IReadOnlyList<EquipmentProgress> GetAllOwned() => GetAll().Where(e => e.IsOwned).ToList();

        public Result Save(EquipmentProgress equipment)
        {
            EnsureSeeded();
            _equipment[equipment.Id] = equipment;
            return Result.Success();
        }

        public EquipmentLoadout GetLoadout()
        {
            EnsureSeeded();
            return _loadout;
        }

        public Result SaveLoadout(EquipmentLoadout loadout)
        {
            EnsureSeeded();
            _loadout = loadout;
            return Result.Success();
        }

        public Result Flush(SaveId saveId)
        {
            EnsureSeeded();

            var rows = _equipment.Values.Select(e => string.Join("|", new[]
            {
                e.Id.Value,
                e.IsOwned ? "1" : "0",
                e.UpgradeLevel.ToString(CultureInfo.InvariantCulture),
            }));

            var loadout = string.Join(",", _loadout.Items.Select(i => i.Value));
            var selected = _loadout.HasSelection ? _loadout.Selected.Value : string.Empty;

            _snapshots.Put(saveId, Key, string.Join("\n", rows) + "\n#" + loadout + "\n#" + selected);
            return Result.Success();
        }

        public Result Load(SaveId saveId)
        {
            if (!_snapshots.TryGet(saveId, Key, out var payload))
                return Result.Failure("save.missing_section", $"No '{Key}' section in save {saveId}.");

            EnsureSeeded();

            var lines = payload.Split('\n');
            var loadout = new EquipmentLoadout();
            var selectedId = string.Empty;

            foreach (var line in lines)
            {
                if (line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var parts = line.Split('|');
                if (parts.Length != 3)
                    continue;

                var id = new EquipmentId(parts[0]);
                if (!_equipment.TryGetValue(id, out var current) ||
                    !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var level) || level < 0)
                    return Result.Failure("save.invalid_section", $"Invalid '{Key}' row: {line}");

                _equipment[id] = new EquipmentProgress(current.Definition, parts[1] == "1", level);
            }

            var meta = lines.Where(l => l.StartsWith("#", StringComparison.Ordinal)).ToArray();
            if (meta.Length >= 1)
            {
                foreach (var id in meta[0].Substring(1).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    loadout.Add(new EquipmentId(id));
            }

            if (meta.Length >= 2)
                selectedId = meta[1].Substring(1);

            if (selectedId.Length > 0)
                loadout.Select(new EquipmentId(selectedId));

            _loadout = loadout;
            return Result.Success();
        }

        public void Reset()
        {
            _equipment = null;
            _loadout = null;
        }

        private void EnsureSeeded()
        {
            if (_equipment != null)
                return;

            _equipment = new Dictionary<EquipmentId, EquipmentProgress>();
            _loadout = new EquipmentLoadout();

            foreach (var definition in _definitions.GetAll().OrderBy(d => d.Id.Value, StringComparer.Ordinal))
            {
                _equipment[definition.Id] = new EquipmentProgress(definition, definition.IsStarter);

                if (definition.IsStarter)
                    _loadout.Add(definition.Id);
            }
        }
    }

    public sealed class EquipmentInfrastructureModule : FeatureModule
    {
        public override string Name => "Equipment.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton(r => new InMemoryEquipmentRepository(
                r.Resolve<IEquipmentDefinitionProvider>(),
                r.Resolve<ISnapshotStore>()));
            services.Forward<IEquipmentRepository, InMemoryEquipmentRepository>();
            services.Forward<ISaveParticipant, InMemoryEquipmentRepository>();
        }
    }
}
