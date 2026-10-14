using System.Collections.Generic;
using CleanTime.Inventory.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Inventory.Application
{
    public sealed record AddItemCommand(ItemId ItemId, int Quantity = 1) : ICommand;

    public sealed record RemoveItemCommand(ItemId ItemId, int Quantity = 1) : ICommand;

    public sealed record GetInventoryQuery : IQuery<IReadOnlyList<InventoryItem>>;
}
