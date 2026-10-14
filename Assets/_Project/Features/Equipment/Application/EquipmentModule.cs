using System.Collections.Generic;
using CleanTime.Equipment.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Economy.Application;

namespace CleanTime.Equipment.Application
{
    public sealed class EquipmentModule : FeatureModule
    {
        public override string Name => "Equipment";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<IEquipmentPricePolicy>(_ => new DefaultEquipmentPricePolicy());
            services.RegisterSingleton<IEquipmentUpgradePolicy>(_ => new LinearEquipmentUpgradePolicy());

            services.RegisterSingleton<ICommandHandler<EquipItemCommand>>(
                r => new EquipItemHandler(r.Resolve<IEquipmentRepository>(), r.Resolve<IEventBus>()));
            services.RegisterSingleton<ICommandHandler<SwitchEquipmentCommand>>(
                r => new SwitchEquipmentHandler(r.Resolve<IEquipmentRepository>(), r.Resolve<IEventBus>()));
            services.RegisterSingleton<ICommandHandler<PurchaseEquipmentCommand>>(r => new PurchaseEquipmentHandler(
                r.Resolve<IEquipmentRepository>(),
                r.Resolve<IEquipmentPricePolicy>(),
                r.Resolve<IWalletService>(),
                r.Resolve<IEventBus>()));
            services.RegisterSingleton<ICommandHandler<UpgradeEquipmentCommand>>(r => new UpgradeEquipmentHandler(
                r.Resolve<IEquipmentRepository>(),
                r.Resolve<IEquipmentUpgradePolicy>(),
                r.Resolve<IWalletService>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<ISelectedEquipmentQuery>(
                r => new SelectedEquipmentQuery(r.Resolve<IEquipmentRepository>()));
            services.RegisterSingleton<IQueryHandler<GetCurrentEquipmentQuery, Result<EquipmentProgress>>>(
                r => new GetCurrentEquipmentHandler(r.Resolve<IEquipmentRepository>()));
            services.RegisterSingleton<IQueryHandler<GetOwnedEquipmentQuery, IReadOnlyList<EquipmentProgress>>>(
                r => new GetOwnedEquipmentHandler(r.Resolve<IEquipmentRepository>()));
        }
    }
}
