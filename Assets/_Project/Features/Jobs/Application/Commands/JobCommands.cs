using System.Collections.Generic;
using CleanTime.Cleaning.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Jobs.Application
{
    public sealed record AcceptJobCommand(JobDefinitionId DefinitionId) : ICommand;

    public sealed record StartJobCommand(JobId JobId) : ICommand;

    public sealed record CompleteJobCommand(JobId JobId, CleaningResult Cleaning) : ICommand;

    public sealed record AbandonJobCommand(JobId JobId) : ICommand;

    /// <summary>The player dismissed the result screen.</summary>
    public sealed record FinishJobResultCommand : ICommand;

    public sealed record GetAvailableJobsQuery : IQuery<IReadOnlyList<JobDefinitionSnapshot>>;

    public sealed record GetActiveJobQuery : IQuery<Result<Job>>;

    public sealed record GetJobProgressQuery : IQuery<float>;
}
