using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CleanTime.Quests.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Quests.Infrastructure
{
    public sealed class InMemoryQuestRepository : IQuestRepository, ISaveParticipant
    {
        private readonly IQuestDefinitionProvider _definitions;
        private readonly ISnapshotStore _snapshots;
        private Dictionary<QuestId, QuestInstance> _quests;

        public InMemoryQuestRepository(IQuestDefinitionProvider definitions, ISnapshotStore snapshots)
        {
            _definitions = definitions;
            _snapshots = snapshots;
        }

        public string Key => "quests.progress";

        public Result<QuestInstance> Get(QuestId id)
        {
            EnsureSeeded();

            return _quests.TryGetValue(id, out var quest)
                ? Result<QuestInstance>.Success(quest)
                : Result<QuestInstance>.Failure(QuestErrors.NotActive, $"Quest '{id}' has not been activated.");
        }

        public IReadOnlyList<QuestInstance> GetActive()
        {
            EnsureSeeded();
            return _quests.Values.Where(q => q.State == QuestState.Active).ToList();
        }

        public IReadOnlyList<QuestInstance> GetCompleted()
        {
            EnsureSeeded();
            return _quests.Values.Where(q => q.State != QuestState.Active).ToList();
        }

        public Result Save(QuestInstance quest)
        {
            EnsureSeeded();
            _quests[quest.Id] = quest;
            return Result.Success();
        }

        public Result Flush(SaveId saveId)
        {
            EnsureSeeded();

            var rows = _quests.Values.Select(q => string.Join("|", new[]
            {
                q.Id.Value,
                ((int)q.State).ToString(CultureInfo.InvariantCulture),
                q.SerializeProgress(),
            }));

            _snapshots.Put(saveId, Key, string.Join("\n", rows));
            return Result.Success();
        }

        public Result Load(SaveId saveId)
        {
            if (!_snapshots.TryGet(saveId, Key, out var payload))
                return Result.Failure("save.missing_section", $"No '{Key}' section in save {saveId}.");

            var loaded = new Dictionary<QuestId, QuestInstance>();

            foreach (var line in payload.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('|');
                var id = parts.Length == 3 ? new QuestId(parts[0]) : default;

                if (parts.Length != 3 ||
                    !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var state) ||
                    !Enum.IsDefined(typeof(QuestState), state) ||
                    _definitions.Get(id).IsFailure)
                    return Result.Failure("save.invalid_section", $"Invalid '{Key}' row: {line}");

                loaded[id] = new QuestInstance(
                    _definitions.Get(id).Value,
                    (QuestState)state,
                    QuestInstance.ParseProgress(parts[2]));
            }

            _quests = loaded;
            return Result.Success();
        }

        public void Reset() => _quests = null;

        private void EnsureSeeded()
        {
            if (_quests != null)
                return;

            _quests = new Dictionary<QuestId, QuestInstance>();

            foreach (var definition in _definitions.GetAll().Where(d => d.ActiveByDefault))
                _quests[definition.Id] = new QuestInstance(definition);
        }
    }

    public sealed class QuestsInfrastructureModule : FeatureModule
    {
        public override string Name => "Quests.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton(r => new InMemoryQuestRepository(
                r.Resolve<IQuestDefinitionProvider>(),
                r.Resolve<ISnapshotStore>()));
            services.Forward<IQuestRepository, InMemoryQuestRepository>();
            services.Forward<ISaveParticipant, InMemoryQuestRepository>();
        }
    }
}
