using CleanTime.Shared.Kernel;

namespace CleanTime.Progression.Domain
{
    public sealed record PlayerProgressChangedEvent(int TotalJobsCompleted) : IDomainEvent;

    public sealed record LocationUnlockedEvent(LocationId LocationId) : IDomainEvent;

    public sealed record JobTierUnlockedEvent(int Tier) : IDomainEvent;

    public sealed record CompanyUnlockedEvent : IDomainEvent;

    public sealed record StoryMilestoneReachedEvent(string MilestoneId) : IDomainEvent;
}
