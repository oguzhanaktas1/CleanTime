using CleanTime.Shared.Kernel;

namespace CleanTime.Dialogue.Domain
{
    public sealed record DialogueStartedEvent(DialogueId DialogueId, NpcId NpcId) : IDomainEvent;

    public sealed record DialogueEndedEvent(DialogueId DialogueId, NpcId NpcId) : IDomainEvent;
}
