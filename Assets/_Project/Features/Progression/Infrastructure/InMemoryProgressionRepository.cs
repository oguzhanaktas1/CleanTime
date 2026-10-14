using System;
using System.Globalization;
using System.Linq;
using CleanTime.Progression.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Progression.Infrastructure
{
    public sealed class InMemoryProgressionRepository : IProgressionRepository, ISaveParticipant
    {
        private readonly ISnapshotStore _snapshots;
        private PlayerProgression _progression = new PlayerProgression();

        public InMemoryProgressionRepository(ISnapshotStore snapshots)
        {
            _snapshots = snapshots;
        }

        public string Key => "progression.player";

        public Result<PlayerProgression> Get() => Result<PlayerProgression>.Success(_progression);

        public Result Save(PlayerProgression progression)
        {
            _progression = progression;
            return Result.Success();
        }

        public Result Flush(SaveId saveId)
        {
            var p = _progression;
            var header = string.Join("|", new[]
            {
                p.TotalJobsCompleted.ToString(CultureInfo.InvariantCulture),
                p.CompanyUnlocked ? "1" : "0",
                p.StoryLevel.ToString(CultureInfo.InvariantCulture),
                p.JobTier.ToString(CultureInfo.InvariantCulture),
            });

            _snapshots.Put(saveId, Key, header + "\n" + string.Join(",", p.ReachedMilestones));
            return Result.Success();
        }

        public Result Load(SaveId saveId)
        {
            if (!_snapshots.TryGet(saveId, Key, out var payload))
                return Result.Failure("save.missing_section", $"No '{Key}' section in save {saveId}.");

            var lines = payload.Split('\n');
            var parts = lines[0].Split('|');

            if (parts.Length != 4 ||
                !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var jobs) || jobs < 0 ||
                !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var story) || story < 0 ||
                !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var tier) || tier < 1)
                return Result.Failure("save.invalid_section", $"Invalid '{Key}' payload.");

            var milestones = lines.Length > 1
                ? lines[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();

            _progression = new PlayerProgression(jobs, parts[1] == "1", story, tier, milestones.ToList());
            return Result.Success();
        }

        public void Reset() => _progression = new PlayerProgression();
    }

    public sealed class ProgressionInfrastructureModule : FeatureModule
    {
        public override string Name => "Progression.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton(r => new InMemoryProgressionRepository(r.Resolve<ISnapshotStore>()));
            services.Forward<IProgressionRepository, InMemoryProgressionRepository>();
            services.Forward<ISaveParticipant, InMemoryProgressionRepository>();
        }
    }
}
