using CleanTime.Economy.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Economy.Application
{
    public sealed class EconomyModule : FeatureModule
    {
        public override string Name => "Economy";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<IPurchasePolicy>(_ => new AffordabilityPurchasePolicy());

            services.RegisterSingleton<IWalletService>(r => new WalletService(
                r.Resolve<IWalletRepository>(),
                r.Resolve<ITransactionRepository>(),
                r.Resolve<IPurchasePolicy>(),
                r.Resolve<IEventBus>(),
                r.Resolve<IGameClock>(),
                r.Resolve<IUtcClock>(),
                r.Resolve<IIdGenerator>()));

            services.RegisterSingleton<ICommandHandler<CreditMoneyCommand>>(
                r => new CreditMoneyHandler(r.Resolve<IWalletService>()));
            services.RegisterSingleton<ICommandHandler<DebitMoneyCommand>>(
                r => new DebitMoneyHandler(r.Resolve<IWalletService>()));
            services.RegisterSingleton<IQueryHandler<GetPlayerWalletQuery, Money>>(
                r => new GetPlayerWalletHandler(r.Resolve<IWalletService>()));

            services.RegisterSingleton(r => new EconomyRewardProjection(
                r.Resolve<IEventBus>(),
                r.Resolve<IWalletService>(),
                r.Resolve<ILogger>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            services.Resolve<EconomyRewardProjection>();
        }
    }
}
