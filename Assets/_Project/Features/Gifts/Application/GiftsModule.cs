using System.Collections.Generic;
using CleanTime.Economy.Application;
using CleanTime.Gifts.Domain;
using CleanTime.Relationships.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Gifts.Application
{
    public sealed class GiftsModule : FeatureModule
    {
        public override string Name => "Gifts";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<IGiftEffectResolver>(_ => new DefinitionGiftEffectResolver());

            services.RegisterSingleton<ICommandHandler<PurchaseGiftCommand>>(r => new PurchaseGiftHandler(
                r.Resolve<IGiftDefinitionProvider>(),
                r.Resolve<IGiftOwnershipRepository>(),
                r.Resolve<IWalletService>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<ICommandHandler<GiveGiftCommand>>(r => new GiveGiftHandler(
                r.Resolve<IGiftDefinitionProvider>(),
                r.Resolve<IGiftOwnershipRepository>(),
                r.Resolve<IGiftEffectResolver>(),
                r.Resolve<IRelationshipRepository>(),
                r.Resolve<IGameClock>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<IQueryHandler<GetAvailableGiftsQuery, IReadOnlyList<GiftDefinitionSnapshot>>>(
                r => new GetAvailableGiftsHandler(r.Resolve<IGiftDefinitionProvider>(), r.Resolve<IRelationshipRepository>()));
        }
    }
}
