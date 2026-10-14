using System.Collections.Generic;
using CleanTime.Inventory.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Inventory.Application
{
    public sealed class InventoryModule : FeatureModule
    {
        public override string Name => "Inventory";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<ICommandHandler<AddItemCommand>>(r => new AddItemHandler(
                r.Resolve<IItemDefinitionProvider>(), r.Resolve<IInventoryRepository>(), r.Resolve<IGameClock>(), r.Resolve<IEventBus>()));
            services.RegisterSingleton<ICommandHandler<RemoveItemCommand>>(r => new RemoveItemHandler(
                r.Resolve<IInventoryRepository>(), r.Resolve<IEventBus>()));
            services.RegisterSingleton<IQueryHandler<GetInventoryQuery, IReadOnlyList<InventoryItem>>>(
                r => new GetInventoryHandler(r.Resolve<IInventoryRepository>()));
        }
    }
}
