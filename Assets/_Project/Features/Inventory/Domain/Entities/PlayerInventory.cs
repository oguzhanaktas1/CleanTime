using System.Collections.Generic;
using System.Linq;
using CleanTime.Shared.Kernel;

namespace CleanTime.Inventory.Domain
{
    public static class InventoryErrors
    {
        public const string InvalidQuantity = "inventory.invalid_quantity";
        public const string NotEnough = "inventory.not_enough";
        public const string StackFull = "inventory.stack_full";
        public const string UnknownItem = "inventory.unknown_item";
    }

    public sealed class ItemDefinitionSnapshot
    {
        public ItemDefinitionSnapshot(ItemId id, string displayNameKey, int maxStack = 99)
        {
            Id = id;
            DisplayNameKey = displayNameKey ?? string.Empty;
            MaxStack = maxStack < 1 ? 1 : maxStack;
        }

        public ItemId Id { get; }

        public string DisplayNameKey { get; }

        public int MaxStack { get; }
    }

    public sealed class InventoryItem
    {
        public InventoryItem(ItemId itemId, int quantity, int? acquiredAtDay = null)
        {
            ItemId = itemId;
            Quantity = Guard.NotNegative(quantity, nameof(quantity));
            AcquiredAtDay = acquiredAtDay;
        }

        public ItemId ItemId { get; }

        public int Quantity { get; internal set; }

        public int? AcquiredAtDay { get; }
    }

    /// <summary>
    /// Quantity based item storage. The GDD currently needs one of each tool, but quantities keep
    /// consumables possible without a rewrite.
    /// </summary>
    public sealed class PlayerInventory
    {
        private readonly Dictionary<ItemId, InventoryItem> _items = new Dictionary<ItemId, InventoryItem>();

        public IReadOnlyCollection<InventoryItem> Items => _items.Values;

        public int GetQuantity(ItemId id) => _items.TryGetValue(id, out var item) ? item.Quantity : 0;

        public bool Has(ItemId id, int quantity = 1) => GetQuantity(id) >= quantity;

        public Result Add(ItemDefinitionSnapshot definition, int quantity, int gameDay)
        {
            if (quantity <= 0)
                return Result.Failure(InventoryErrors.InvalidQuantity, "Quantity must be positive.");

            var current = GetQuantity(definition.Id);
            if (current + quantity > definition.MaxStack)
                return Result.Failure(InventoryErrors.StackFull, $"'{definition.Id}' is limited to {definition.MaxStack}.");

            if (_items.TryGetValue(definition.Id, out var existing))
                existing.Quantity += quantity;
            else
                _items[definition.Id] = new InventoryItem(definition.Id, quantity, gameDay);

            return Result.Success();
        }

        public Result Remove(ItemId id, int quantity)
        {
            if (quantity <= 0)
                return Result.Failure(InventoryErrors.InvalidQuantity, "Quantity must be positive.");

            if (!_items.TryGetValue(id, out var item) || item.Quantity < quantity)
                return Result.Failure(InventoryErrors.NotEnough, $"Not enough '{id}'.");

            item.Quantity -= quantity;
            if (item.Quantity == 0)
                _items.Remove(id);

            return Result.Success();
        }

        public IReadOnlyList<InventoryItem> Snapshot() => _items.Values.OrderBy(i => i.ItemId.Value).ToList();
    }
}
