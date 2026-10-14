using CleanTime.Shared.Kernel;

namespace CleanTime.Inventory.Domain
{
    public interface IItemDefinitionProvider : IDefinitionProvider<ItemId, ItemDefinitionSnapshot>
    {
    }

    public interface IInventoryRepository
    {
        PlayerInventory Get();

        Result Save(PlayerInventory inventory);
    }
}
