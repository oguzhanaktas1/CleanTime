using CleanTime.Economy.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Economy.Infrastructure
{
    public sealed class EconomyInfrastructureModule : FeatureModule
    {
        private readonly Money _startingBalance;

        public EconomyInfrastructureModule(Money startingBalance)
        {
            _startingBalance = startingBalance;
        }

        public override string Name => "Economy.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton(r => new InMemoryWalletRepository(_startingBalance, r.Resolve<ISnapshotStore>()));
            services.Forward<IWalletRepository, InMemoryWalletRepository>();
            services.Forward<ISaveParticipant, InMemoryWalletRepository>();
            services.RegisterSingleton<ITransactionRepository>(_ => new InMemoryTransactionRepository());
        }
    }
}
