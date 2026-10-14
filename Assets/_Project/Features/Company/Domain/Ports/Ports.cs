using CleanTime.Shared.Kernel;

namespace CleanTime.Company.Domain
{
    public interface ICompanyDefinitionProvider : IDefinitionProvider<CompanyUpgradeId, CompanyUpgradeDefinition>
    {
    }

    public interface ICompanyRepository
    {
        /// <summary>Fails with <see cref="CompanyErrors.NotFound"/> until the company was created.</summary>
        Result<CompanyProfile> Get();

        Result Save(CompanyProfile company);
    }
}
