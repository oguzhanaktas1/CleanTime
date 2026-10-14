using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Jobs.Domain
{
    public interface IJobDefinitionProvider : IDefinitionProvider<JobDefinitionId, JobDefinitionSnapshot>
    {
    }

    public interface IJobRepository
    {
        Result<Job> GetActive();

        Result SaveActive(Job job);

        void ClearActive();

        void AddHistory(JobResult result);

        IReadOnlyList<JobResult> GetHistory();

        /// <summary>Result awaiting acknowledgement on the result screen.</summary>
        Result<JobResult> GetPendingResult();

        void SetPendingResult(JobResult result);

        void ClearPendingResult();
    }

    public interface IJobAvailabilityPolicy
    {
        bool IsAvailable(JobDefinitionSnapshot definition);
    }
}
