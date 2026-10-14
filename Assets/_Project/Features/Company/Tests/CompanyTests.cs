using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CleanTime.Company.Application;
using CleanTime.Company.Domain;
using CleanTime.Company.Infrastructure;
using CleanTime.Economy.Application;
using CleanTime.Economy.Domain;
using CleanTime.Employees.Domain;
using CleanTime.Progression.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;

namespace CleanTime.Company.Tests
{
    internal sealed class FakeWallet : IWalletService
    {
        public Money Balance { get; set; } = new Money(100000);
        public bool CanAfford(Money price) => Balance >= price;
        public Result Credit(Money amount, TransactionReason reason, string referenceId = "") { Balance += amount; return Result.Success(); }
        public Result Debit(Money amount, TransactionReason reason, string referenceId = "") => Pay(amount, reason, referenceId);

        public Result Pay(Money price, TransactionReason reason, string referenceId = "")
        {
            if (!CanAfford(price)) return Result.Failure(EconomyErrors.InsufficientFunds, "no money");
            Balance -= price;
            return Result.Success();
        }
    }

    internal sealed class FakeProgression : IProgressionRepository
    {
        public PlayerProgression Progression { get; set; } = new PlayerProgression();
        public Result<PlayerProgression> Get() => Result<PlayerProgression>.Success(Progression);
        public Result Save(PlayerProgression progression) { Progression = progression; return Result.Success(); }
    }

    internal sealed class Upgrades : ICompanyDefinitionProvider
    {
        public static readonly CompanyUpgradeId Office = new CompanyUpgradeId("upgrade.office");

        private readonly CompanyUpgradeDefinition _office = new CompanyUpgradeDefinition(Office, "k", 2, new Money(10000), 2m);

        public Result<CompanyUpgradeDefinition> Get(CompanyUpgradeId id) => id == Office
            ? Result<CompanyUpgradeDefinition>.Success(_office)
            : Result<CompanyUpgradeDefinition>.Failure("x", "x");

        public IReadOnlyCollection<CompanyUpgradeDefinition> GetAll() => new[] { _office };
    }

    internal sealed class CompanyFixture
    {
        public EventBus Bus { get; } = new EventBus();
        public FakeProgression Progression { get; } = new FakeProgression();
        public FakeWallet Wallet { get; } = new FakeWallet();
        public InMemoryCompanyRepository Repository { get; } = new InMemoryCompanyRepository();
        public CreateCompanyHandler Create { get; }
        public PurchaseCompanyUpgradeHandler Upgrade { get; }
        public IncreaseCompanyReputationHandler Reputation { get; }

        public CompanyFixture()
        {
            var policy = new ReputationCompanyUpgradePolicy(new[] { 10, 30 });
            Create = new CreateCompanyHandler(Progression, Repository, Bus);
            Upgrade = new PurchaseCompanyUpgradeHandler(Repository, new Upgrades(), policy, Wallet);
            Reputation = new IncreaseCompanyReputationHandler(Repository, policy, Bus);
        }
    }

    public sealed class CompanyTests
    {
        [Test]
        public void CreatingACompany_RequiresTheUnlock()
        {
            var f = new CompanyFixture();

            Assert.AreEqual(CompanyErrors.NotUnlocked, f.Create.Handle(new CreateCompanyCommand("Sparkle")).ErrorCode);
        }

        [Test]
        public void Create_StoresTheCompany_AndAnnouncesIt()
        {
            var f = new CompanyFixture();
            f.Progression.Progression.UnlockCompany();
            using var created = f.Bus.Record<CompanyCreatedEvent>();

            Assert.IsTrue(f.Create.Handle(new CreateCompanyCommand("  Sparkle  ")).IsSuccess);

            Assert.AreEqual("Sparkle", f.Repository.Get().Value.Name);
            Assert.AreEqual(1, created.Count);
        }

        [Test]
        public void OnlyOneCompany_AndItNeedsAName()
        {
            var f = new CompanyFixture();
            f.Progression.Progression.UnlockCompany();

            Assert.AreEqual(CompanyErrors.InvalidName, f.Create.Handle(new CreateCompanyCommand("  ")).ErrorCode);
            f.Create.Handle(new CreateCompanyCommand("A"));
            Assert.AreEqual(CompanyErrors.AlreadyExists, f.Create.Handle(new CreateCompanyCommand("B")).ErrorCode);
        }

        [Test]
        public void Reputation_RaisesTheCompanyLevel_AndCapacity()
        {
            var f = new CompanyFixture();
            f.Progression.Progression.UnlockCompany();
            f.Create.Handle(new CreateCompanyCommand("Sparkle"));
            using var levels = f.Bus.Record<CompanyLevelChangedEvent>();
            var capacityBefore = f.Repository.Get().Value.EmployeeCapacity;

            f.Reputation.Handle(new IncreaseCompanyReputationCommand(35));

            Assert.AreEqual(3, f.Repository.Get().Value.Level);
            Assert.AreEqual(1, levels.Count);
            Assert.Greater(f.Repository.Get().Value.EmployeeCapacity, capacityBefore);
        }

        [Test]
        public void Upgrades_CostMoreEachLevel_UntilMaxed()
        {
            var f = new CompanyFixture();
            f.Progression.Progression.UnlockCompany();
            f.Create.Handle(new CreateCompanyCommand("Sparkle"));

            Assert.IsTrue(f.Upgrade.Handle(new PurchaseCompanyUpgradeCommand(Upgrades.Office)).IsSuccess);
            Assert.AreEqual(90000, f.Wallet.Balance.Cents);

            Assert.IsTrue(f.Upgrade.Handle(new PurchaseCompanyUpgradeCommand(Upgrades.Office)).IsSuccess);
            Assert.AreEqual(70000, f.Wallet.Balance.Cents); // second level costs 2x

            Assert.AreEqual(CompanyErrors.UpgradeMaxed, f.Upgrade.Handle(new PurchaseCompanyUpgradeCommand(Upgrades.Office)).ErrorCode);
            Assert.AreEqual(2, f.Repository.Get().Value.GetUpgradeLevel(Upgrades.Office));
        }

        [Test]
        public void Upgrades_NeedFunds_AndAKnownId()
        {
            var f = new CompanyFixture();
            f.Progression.Progression.UnlockCompany();
            f.Create.Handle(new CreateCompanyCommand("Sparkle"));
            f.Wallet.Balance = new Money(5);

            Assert.AreEqual(EconomyErrors.InsufficientFunds, f.Upgrade.Handle(new PurchaseCompanyUpgradeCommand(Upgrades.Office)).ErrorCode);
            Assert.AreEqual(CompanyErrors.UnknownUpgrade, f.Upgrade.Handle(new PurchaseCompanyUpgradeCommand(new CompanyUpgradeId("nope"))).ErrorCode);
        }

        [Test]
        public void EmployeeRevenue_AccumulatesAsLifetimeRevenue()
        {
            var f = new CompanyFixture();
            f.Progression.Progression.UnlockCompany();
            f.Create.Handle(new CreateCompanyCommand("Sparkle"));
            using var projection = new CompanyRevenueProjection(f.Bus, f.Repository);

            f.Bus.Publish(new EmployeeRevenueGeneratedEvent(new EmployeeId("e"), new Money(700)));
            f.Bus.Publish(new EmployeeRevenueGeneratedEvent(new EmployeeId("e"), new Money(300)));

            Assert.AreEqual(1000, f.Repository.Get().Value.LifetimeRevenue.Cents);
        }

        [Test]
        public void OverviewFails_BeforeTheCompanyExists()
        {
            var f = new CompanyFixture();

            Assert.AreEqual(CompanyErrors.NotFound, new GetCompanyOverviewHandler(f.Repository).Handle(new GetCompanyOverviewQuery()).ErrorCode);
        }
    }
}
