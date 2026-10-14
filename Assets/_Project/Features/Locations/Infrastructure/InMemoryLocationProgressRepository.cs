using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CleanTime.Locations.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Locations.Infrastructure
{
    public sealed class InMemoryLocationProgressRepository : ILocationProgressRepository, ISaveParticipant
    {
        private readonly ILocationDefinitionProvider _definitions;
        private readonly ISnapshotStore _snapshots;
        private Dictionary<LocationId, LocationProgress> _progress;

        public InMemoryLocationProgressRepository(ILocationDefinitionProvider definitions, ISnapshotStore snapshots)
        {
            _definitions = definitions;
            _snapshots = snapshots;
        }

        public string Key => "locations.progress";

        public Result<LocationProgress> Get(LocationId locationId)
        {
            EnsureSeeded();

            return _progress.TryGetValue(locationId, out var progress)
                ? Result<LocationProgress>.Success(progress)
                : Result<LocationProgress>.Failure("locations.unknown", $"Unknown location '{locationId}'.");
        }

        public IReadOnlyList<LocationProgress> GetAll()
        {
            EnsureSeeded();
            return _progress.Values.ToList();
        }

        public Result Save(LocationProgress progress)
        {
            EnsureSeeded();
            _progress[progress.LocationId] = progress;
            return Result.Success();
        }

        public Result Flush(SaveId saveId)
        {
            EnsureSeeded();

            var lines = _progress.Values.Select(p => string.Join("|", new[]
            {
                p.LocationId.Value,
                p.Unlocked ? "1" : "0",
                p.CompletionCount.ToString(CultureInfo.InvariantCulture),
                p.BestScore?.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty,
                p.BestCompletionSeconds?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                p.LastCompletedDay?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            }));

            _snapshots.Put(saveId, Key, string.Join("\n", lines));
            return Result.Success();
        }

        public Result Load(SaveId saveId)
        {
            if (!_snapshots.TryGet(saveId, Key, out var payload))
                return Result.Failure("save.missing_section", $"No '{Key}' section in save {saveId}.");

            var loaded = new Dictionary<LocationId, LocationProgress>();

            foreach (var line in payload.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('|');
                if (parts.Length != 6 || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 0)
                    return Result.Failure("save.invalid_section", $"Invalid '{Key}' row: {line}");

                var id = new LocationId(parts[0]);
                loaded[id] = new LocationProgress(
                    id,
                    parts[1] == "1",
                    count,
                    ParseFloat(parts[3]),
                    ParseInt(parts[4]),
                    ParseInt(parts[5]));
            }

            _progress = loaded;
            return Result.Success();
        }

        public void Reset() => _progress = null;

        private void EnsureSeeded()
        {
            if (_progress != null)
                return;

            _progress = new Dictionary<LocationId, LocationProgress>();

            foreach (var definition in _definitions.GetAll())
                _progress[definition.Id] = new LocationProgress(definition.Id, unlocked: definition.UnlockedByDefault);
        }

        private static float? ParseFloat(string text) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : (float?)null;

        private static int? ParseInt(string text) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : (int?)null;
    }

    public sealed class LocationsInfrastructureModule : FeatureModule
    {
        public override string Name => "Locations.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton(r => new InMemoryLocationProgressRepository(
                r.Resolve<ILocationDefinitionProvider>(),
                r.Resolve<ISnapshotStore>()));
            services.Forward<ILocationProgressRepository, InMemoryLocationProgressRepository>();
            services.Forward<ISaveParticipant, InMemoryLocationProgressRepository>();
        }
    }
}
