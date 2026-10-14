using CleanTime.Shared.Kernel;

namespace CleanTime.Jobs.Domain
{
    public sealed record JobAcceptedEvent(JobId JobId, JobDefinitionId DefinitionId, LocationId LocationId) : IDomainEvent;

    public sealed record JobStartedEvent(JobId JobId, JobDefinitionId DefinitionId, LocationId LocationId, int Day) : IDomainEvent;

    public sealed record JobObjectiveProgressChangedEvent(JobId JobId, string ObjectiveType, float Progress) : IDomainEvent;

    public sealed record CustomerRatingCalculatedEvent(JobId JobId, CustomerEvaluation Evaluation) : IDomainEvent;

    public sealed record JobCompletedEvent(
        JobId JobId,
        JobDefinitionId DefinitionId,
        LocationId LocationId,
        Money Reward,
        float CustomerScore,
        CustomerEvaluation Evaluation,
        float DurationSeconds,
        int GameDay) : IDomainEvent;

    /// <summary>Raised when a job ends without completion (currently: abandoned by the player).</summary>
    public sealed record JobFailedEvent(JobId JobId, JobDefinitionId DefinitionId, LocationId LocationId, string Reason) : IDomainEvent;

    public sealed record JobResultAcknowledgedEvent(JobId JobId) : IDomainEvent;
}
