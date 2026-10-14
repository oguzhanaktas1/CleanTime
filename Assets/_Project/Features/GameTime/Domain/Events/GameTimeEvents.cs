using CleanTime.Shared.Kernel;

namespace CleanTime.GameTime.Domain
{
    public sealed record ActionConsumedEvent(GameActionKind Kind, int Cost, GameDate Now) : IDomainEvent;

    public sealed record DayPhaseChangedEvent(DayPhase Previous, DayPhase Current, int Day) : IDomainEvent;

    public sealed record GameDayEndedEvent(int Day) : IDomainEvent;

    public sealed record DaySummaryCreatedEvent(DaySummary Summary) : IDomainEvent;

    public sealed record GameDayStartedEvent(int Day) : IDomainEvent;
}
