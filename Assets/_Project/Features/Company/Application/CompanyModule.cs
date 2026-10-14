using CleanTime.Company.Domain;
using CleanTime.Economy.Application;
using CleanTime.Progression.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Company.Application
{
    public sealed class CompanyModule : FeatureModule
    {
        public override string Name => "Company";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<ICompanyUpgradePolicy>(_ => new ReputationCompanyUpgradePolicy());

            services.RegisterSingleton<ICommandHandler<CreateCompanyCommand>>(r => new CreateCompanyHandler(
                r.Resolve<IProgressionRepository>(), r.Resolve<ICompanyRepository>(), r.Resolve<IEventBus>()));

            services.RegisterSingleton<ICommandHandler<PurchaseCompanyUpgradeCommand>>(r => new PurchaseCompanyUpgradeHandler(
                r.Resolve<ICompanyRepository>(),
                r.Resolve<ICompanyDefinitionProvider>(),
                r.Resolve<ICompanyUpgradePolicy>(),
                r.Resolve<IWalletService>()));

            services.RegisterSingleton<ICommandHandler<IncreaseCompanyReputationCommand>>(r => new IncreaseCompanyReputationHandler(
                r.Resolve<ICompanyRepository>(), r.Resolve<ICompanyUpgradePolicy>(), r.Resolve<IEventBus>()));

            services.RegisterSingleton<IQueryHandler<GetCompanyOverviewQuery, Result<CompanyProfile>>>(
                r => new GetCompanyOverviewHandler(r.Resolve<ICompanyRepository>()));

            services.RegisterSingleton(r => new CompanyRevenueProjection(r.Resolve<IEventBus>(), r.Resolve<ICompanyRepository>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            services.Resolve<CompanyRevenueProjection>();
        }
    }
}
