using CleanTime.Shared.Kernel;

namespace CleanTime.Company.Domain
{
    public sealed record CompanyCreatedEvent(string Name) : IDomainEvent;

    public sealed record CompanyLevelChangedEvent(int PreviousLevel, int CurrentLevel) : IDomainEvent;
}
