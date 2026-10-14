using CleanTime.Company.Domain;
using CleanTime.Economy.Application;
using CleanTime.Economy.Domain;
using CleanTime.Progression.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Company.Application
{
    internal sealed class CreateCompanyHandler : ICommandHandler<CreateCompanyCommand>
    {
        private readonly IProgressionRepository _progression;
        private readonly ICompanyRepository _company;
        private readonly IEventBus _events;

        public CreateCompanyHandler(IProgressionRepository progression, ICompanyRepository company, IEventBus events)
        {
            _progression = progression;
            _company = company;
            _events = events;
        }

        public Result Handle(CreateCompanyCommand command)
        {
            var progression = _progression.Get();
            if (progression.IsFailure || !progression.Value.CompanyUnlocked)
                return Result.Failure(CompanyErrors.NotUnlocked, "Company creation is not unlocked yet.");

            if (_company.Get().IsSuccess)
                return Result.Failure(CompanyErrors.AlreadyExists, "A company already exists.");

            if (string.IsNullOrWhiteSpace(command.Name))
                return Result.Failure(CompanyErrors.InvalidName, "A company needs a name.");

            _company.Save(new CompanyProfile(command.Name));
            _events.Publish(new CompanyCreatedEvent(command.Name.Trim()));
            return Result.Success();
        }
    }

    internal sealed class PurchaseCompanyUpgradeHandler : ICommandHandler<PurchaseCompanyUpgradeCommand>
    {
        private readonly ICompanyRepository _company;
        private readonly ICompanyDefinitionProvider _definitions;
        private readonly ICompanyUpgradePolicy _policy;
        private readonly IWalletService _wallet;

        public PurchaseCompanyUpgradeHandler(
            ICompanyRepository company,
            ICompanyDefinitionProvider definitions,
            ICompanyUpgradePolicy policy,
            IWalletService wallet)
        {
            _company = company;
            _definitions = definitions;
            _policy = policy;
            _wallet = wallet;
        }

        public Result Handle(PurchaseCompanyUpgradeCommand command)
        {
            var company = _company.Get();
            if (company.IsFailure)
                return company;

            var definition = _definitions.Get(command.UpgradeId);
            if (definition.IsFailure)
                return Result.Failure(CompanyErrors.UnknownUpgrade, $"Unknown upgrade '{command.UpgradeId}'.");

            var level = company.Value.GetUpgradeLevel(command.UpgradeId);
            if (level >= definition.Value.MaxLevel)
                return Result.Failure(CompanyErrors.UpgradeMaxed, $"'{command.UpgradeId}' is fully upgraded.");

            var paid = _wallet.Pay(_policy.GetCost(definition.Value, level), TransactionReason.CompanyUpgrade, command.UpgradeId.Value);
            if (paid.IsFailure)
                return paid;

            company.Value.PurchaseUpgrade(definition.Value);
            return _company.Save(company.Value);
        }
    }

    internal sealed class IncreaseCompanyReputationHandler : ICommandHandler<IncreaseCompanyReputationCommand>
    {
        private readonly ICompanyRepository _company;
        private readonly ICompanyUpgradePolicy _policy;
        private readonly IEventBus _events;

        public IncreaseCompanyReputationHandler(ICompanyRepository company, ICompanyUpgradePolicy policy, IEventBus events)
        {
            _company = company;
            _policy = policy;
            _events = events;
        }

        public Result Handle(IncreaseCompanyReputationCommand command)
        {
            var company = _company.Get();
            if (company.IsFailure)
                return company;

            var previous = company.Value.Level;
            company.Value.AddReputation(command.Amount);

            if (company.Value.SetLevel(_policy.CalculateLevel(company.Value)))
                _events.Publish(new CompanyLevelChangedEvent(previous, company.Value.Level));

            return _company.Save(company.Value);
        }
    }

    internal sealed class GetCompanyOverviewHandler : IQueryHandler<GetCompanyOverviewQuery, Result<CompanyProfile>>
    {
        private readonly ICompanyRepository _company;

        public GetCompanyOverviewHandler(ICompanyRepository company)
        {
            _company = company;
        }

        public Result<CompanyProfile> Handle(GetCompanyOverviewQuery query) => _company.Get();
    }
}
