using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CleanTime.Relationships.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Relationships.Infrastructure
{
    public sealed class InMemoryRelationshipRepository : IRelationshipRepository, ISaveParticipant
    {
        private readonly ISnapshotStore _snapshots;
        private Dictionary<NpcId, Relationship> _relationships = new Dictionary<NpcId, Relationship>();

        public InMemoryRelationshipRepository(ISnapshotStore snapshots)
        {
            _snapshots = snapshots;
        }

        public string Key => "relationships.all";

        public Result<Relationship> Get(NpcId npcId)
        {
            if (npcId.IsEmpty)
                return Result<Relationship>.Failure("relationships.invalid_npc", "An NPC id is required.");

            if (!_relationships.TryGetValue(npcId, out var relationship))
            {
                relationship = new Relationship(npcId);
                _relationships[npcId] = relationship;
            }

            return Result<Relationship>.Success(relationship);
        }

        public IReadOnlyList<Relationship> GetAll() => _relationships.Values.ToList();

        public Result Save(Relationship relationship)
        {
            _relationships[relationship.NpcId] = relationship;
            return Result.Success();
        }

        public Result Flush(SaveId saveId)
        {
            var rows = _relationships.Values.Select(r => string.Join("|", new[]
            {
                r.NpcId.Value,
                r.Points.ToString(CultureInfo.InvariantCulture),
                r.Level.ToString(CultureInfo.InvariantCulture),
                r.LastChangedDay?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            }));

            _snapshots.Put(saveId, Key, string.Join("\n", rows));
            return Result.Success();
        }

        public Result Load(SaveId saveId)
        {
            if (!_snapshots.TryGet(saveId, Key, out var payload))
                return Result.Failure("save.missing_section", $"No '{Key}' section in save {saveId}.");

            var loaded = new Dictionary<NpcId, Relationship>();

            foreach (var line in payload.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('|');
                if (parts.Length != 4 ||
                    !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var points) || points < 0 ||
                    !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var level) || level < 0)
                    return Result.Failure("save.invalid_section", $"Invalid '{Key}' row: {line}");

                var id = new NpcId(parts[0]);
                int? lastDay = int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var day) ? day : (int?)null;
                loaded[id] = new Relationship(id, points, level, lastDay);
            }

            _relationships = loaded;
            return Result.Success();
        }

        public void Reset() => _relationships = new Dictionary<NpcId, Relationship>();
    }

    public sealed class RelationshipsInfrastructureModule : FeatureModule
    {
        public override string Name => "Relationships.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton(r => new InMemoryRelationshipRepository(r.Resolve<ISnapshotStore>()));
            services.Forward<IRelationshipRepository, InMemoryRelationshipRepository>();
            services.Forward<ISaveParticipant, InMemoryRelationshipRepository>();
        }
    }
}
