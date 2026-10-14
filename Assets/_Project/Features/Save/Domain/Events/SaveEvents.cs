using CleanTime.Shared.Kernel;

namespace CleanTime.Save.Domain
{
    public sealed record SaveRequestedEvent(SaveId SaveId) : IDomainEvent;

    public sealed record SaveCompletedEvent(SaveId SaveId) : IDomainEvent;

    public sealed record SaveFailedEvent(SaveId SaveId, string ErrorCode, string Message) : IDomainEvent;

    public sealed record SaveLoadedEvent(SaveId SaveId) : IDomainEvent;

    public sealed record SaveMigratedEvent(int FromVersion, int ToVersion) : IDomainEvent;
}
