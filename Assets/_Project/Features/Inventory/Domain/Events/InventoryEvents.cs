using CleanTime.Shared.Kernel;

namespace CleanTime.Inventory.Domain
{
    public sealed record ItemAddedEvent(ItemId ItemId, int Quantity, int NewTotal) : IDomainEvent;

    public sealed record ItemRemovedEvent(ItemId ItemId, int Quantity, int NewTotal) : IDomainEvent;
}
