using CleanTime.Shared.Kernel;

namespace CleanTime.Locations.Domain
{
    public sealed record LocationCompletionChangedEvent(LocationId LocationId, int CompletionCount) : IDomainEvent;
}
