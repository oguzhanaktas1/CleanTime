using CleanTime.Shared.Kernel;

namespace CleanTime.Relationships.Domain
{
    public sealed record RelationshipPointsChangedEvent(NpcId NpcId, int Delta, int Points) : IDomainEvent;

    public sealed record RelationshipLevelChangedEvent(NpcId NpcId, int PreviousLevel, int Level) : IDomainEvent;

    /// <summary>A configured story beat tied to a relationship level (replaces a hard-coded mother event).</summary>
    public sealed record RelationshipMilestoneReachedEvent(NpcId NpcId, string MilestoneId, int Level) : IDomainEvent;
}
