using System.Collections.Generic;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Jobs.Infrastructure
{
    public sealed class InMemoryJobRepository : IJobRepository
    {
        private readonly List<JobResult> _history = new List<JobResult>();
        private Job _active;
        private JobResult _pending;

        public Result<Job> GetActive() => _active != null
            ? Result<Job>.Success(_active)
            : Result<Job>.Failure(JobErrors.NoActiveJob, "No active job.");

        public Result SaveActive(Job job)
        {
            _active = job;
            return Result.Success();
        }

        public void ClearActive() => _active = null;

        public void AddHistory(JobResult result) => _history.Add(result);

        public IReadOnlyList<JobResult> GetHistory() => _history;

        public Result<JobResult> GetPendingResult() => _pending != null
            ? Result<JobResult>.Success(_pending)
            : Result<JobResult>.Failure(JobErrors.NotFound, "No pending job result.");

        public void SetPendingResult(JobResult result) => _pending = result;

        public void ClearPendingResult() => _pending = null;
    }

    public sealed class JobsInfrastructureModule : FeatureModule
    {
        public override string Name => "Jobs.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<IJobRepository>(_ => new InMemoryJobRepository());
        }
    }
}
