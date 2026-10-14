using System.Collections.Generic;
using CleanTime.Inventory.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Inventory.Application
{
    internal sealed class AddItemHandler : ICommandHandler<AddItemCommand>
    {
        private readonly IItemDefinitionProvider _definitions;
        private readonly IInventoryRepository _inventory;
        private readonly IGameClock _clock;
        private readonly IEventBus _events;

        public AddItemHandler(IItemDefinitionProvider definitions, IInventoryRepository inventory, IGameClock clock, IEventBus events)
        {
            _definitions = definitions;
            _inventory = inventory;
            _clock = clock;
            _events = events;
        }

        public Result Handle(AddItemCommand command)
        {
            var definition = _definitions.Get(command.ItemId);
            if (definition.IsFailure)
                return Result.Failure(InventoryErrors.UnknownItem, $"Unknown item '{command.ItemId}'.");

            var inventory = _inventory.Get();
            var added = inventory.Add(definition.Value, command.Quantity, _clock.Now.Day);
            if (added.IsFailure)
                return added;

            _inventory.Save(inventory);
            _events.Publish(new ItemAddedEvent(command.ItemId, command.Quantity, inventory.GetQuantity(command.ItemId)));
            return Result.Success();
        }
    }

    internal sealed class RemoveItemHandler : ICommandHandler<RemoveItemCommand>
    {
        private readonly IInventoryRepository _inventory;
        private readonly IEventBus _events;

        public RemoveItemHandler(IInventoryRepository inventory, IEventBus events)
        {
            _inventory = inventory;
            _events = events;
        }

        public Result Handle(RemoveItemCommand command)
        {
            var inventory = _inventory.Get();
            var removed = inventory.Remove(command.ItemId, command.Quantity);
            if (removed.IsFailure)
                return removed;

            _inventory.Save(inventory);
            _events.Publish(new ItemRemovedEvent(command.ItemId, command.Quantity, inventory.GetQuantity(command.ItemId)));
            return Result.Success();
        }
    }

    internal sealed class GetInventoryHandler : IQueryHandler<GetInventoryQuery, IReadOnlyList<InventoryItem>>
    {
        private readonly IInventoryRepository _inventory;

        public GetInventoryHandler(IInventoryRepository inventory)
        {
            _inventory = inventory;
        }

        public IReadOnlyList<InventoryItem> Handle(GetInventoryQuery query) => _inventory.Get().Snapshot();
    }
}
