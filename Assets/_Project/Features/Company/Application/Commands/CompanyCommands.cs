using CleanTime.Company.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Company.Application
{
    public sealed record CreateCompanyCommand(string Name) : ICommand;

    public sealed record PurchaseCompanyUpgradeCommand(CompanyUpgradeId UpgradeId) : ICommand;

    public sealed record IncreaseCompanyReputationCommand(int Amount) : ICommand;

    public sealed record GetCompanyOverviewQuery : IQuery<Result<CompanyProfile>>;
}
