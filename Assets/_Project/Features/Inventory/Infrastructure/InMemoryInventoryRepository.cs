using CleanTime.Inventory.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Inventory.Infrastructure
{
    public sealed class InMemoryInventoryRepository : IInventoryRepository
    {
        private PlayerInventory _inventory = new PlayerInventory();

        public PlayerInventory Get() => _inventory;

        public Result Save(PlayerInventory inventory)
        {
            _inventory = inventory;
            return Result.Success();
        }
    }

    public sealed class InventoryInfrastructureModule : FeatureModule
    {
        public override string Name => "Inventory.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<IInventoryRepository>(_ => new InMemoryInventoryRepository());
        }
    }
}
