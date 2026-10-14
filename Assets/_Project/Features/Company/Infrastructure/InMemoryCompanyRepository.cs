using CleanTime.Company.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Company.Infrastructure
{
    public sealed class InMemoryCompanyRepository : ICompanyRepository
    {
        private CompanyProfile _company;

        public Result<CompanyProfile> Get() => _company != null
            ? Result<CompanyProfile>.Success(_company)
            : Result<CompanyProfile>.Failure(CompanyErrors.NotFound, "The company has not been created.");

        public Result Save(CompanyProfile company)
        {
            _company = company;
            return Result.Success();
        }
    }

    public sealed class CompanyInfrastructureModule : FeatureModule
    {
        public override string Name => "Company.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<ICompanyRepository>(_ => new InMemoryCompanyRepository());
        }
    }
}
