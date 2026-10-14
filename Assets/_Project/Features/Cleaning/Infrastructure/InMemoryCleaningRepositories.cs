using System.Collections.Generic;
using CleanTime.Cleaning.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Infrastructure
{
    public sealed class InMemoryCleaningTargetRepository : ICleaningTargetRepository
    {
        private readonly Dictionary<string, CleaningTarget> _targets = new Dictionary<string, CleaningTarget>();

        public Result<CleaningTarget> Get(string instanceId) =>
            instanceId != null && _targets.TryGetValue(instanceId, out var target)
                ? Result<CleaningTarget>.Success(target)
                : Result<CleaningTarget>.Failure(CleaningErrors.TargetNotFound, instanceId ?? string.Empty);

        public Result Save(CleaningTarget target)
        {
            _targets[target.InstanceId] = target;
            return Result.Success();
        }

        public IReadOnlyCollection<CleaningTarget> GetAll() => _targets.Values;

        public void ReplaceAll(IEnumerable<CleaningTarget> targets)
        {
            _targets.Clear();

            foreach (var target in targets)
                _targets[target.InstanceId] = target;
        }

        public void Clear() => _targets.Clear();
    }

    public sealed class InMemoryCleaningSessionRepository : ICleaningSessionRepository
    {
        private CleaningSession _current;

        public Result<CleaningSession> GetCurrent() => _current != null
            ? Result<CleaningSession>.Success(_current)
            : Result<CleaningSession>.Failure(CleaningErrors.NoSession, "No cleaning session.");

        public Result Save(CleaningSession session)
        {
            _current = session;
            return Result.Success();
        }

        public void Clear() => _current = null;
    }

    public sealed class CleaningInfrastructureModule : FeatureModule
    {
        public override string Name => "Cleaning.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<ICleaningTargetRepository>(_ => new InMemoryCleaningTargetRepository());
            services.RegisterSingleton<ICleaningSessionRepository>(_ => new InMemoryCleaningSessionRepository());
        }
    }
}
