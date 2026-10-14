using CleanTime.Shared.Kernel;

namespace CleanTime.Quests.Domain
{
    public sealed record QuestActivatedEvent(QuestId QuestId) : IDomainEvent;

    public sealed record QuestProgressChangedEvent(QuestId QuestId) : IDomainEvent;

    public sealed record QuestCompletedEvent(QuestId QuestId) : IDomainEvent;

    public sealed record QuestRewardGrantedEvent(QuestId QuestId, Money Money, string UnlockKey) : IDomainEvent;
}
