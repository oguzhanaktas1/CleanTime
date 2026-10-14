using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CleanTime.Company.Domain;
using CleanTime.Economy.Application;
using CleanTime.Economy.Domain;
using CleanTime.Employees.Application;
using CleanTime.Employees.Domain;
using CleanTime.Employees.Infrastructure;
using CleanTime.GameTime.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;

namespace CleanTime.Employees.Tests
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

    internal sealed class FakeCompany : ICompanyRepository
    {
        public CompanyProfile Company { get; set; } = new CompanyProfile("Sparkle");

        public Result<CompanyProfile> Get() => Company != null
            ? Result<CompanyProfile>.Success(Company)
            : Result<CompanyProfile>.Failure(CompanyErrors.NotFound, "none");

        public Result Save(CompanyProfile company) { Company = company; return Result.Success(); }
    }

    internal sealed class Jobs : IJobDefinitionProvider
    {
        public static readonly JobDefinitionId Small = new JobDefinitionId("job.small");
        public static readonly JobDefinitionId Hard = new JobDefinitionId("job.hard");

        private readonly List<JobDefinitionSnapshot> _items = new List<JobDefinitionSnapshot>
        {
            new JobDefinitionSnapshot(Small, new LocationId("l"), new NpcId("c"), 2, new Money(20000), null),
            new JobDefinitionSnapshot(Hard, new LocationId("l"), new NpcId("c"), 6, new Money(60000), null),
        };

        public Result<JobDefinitionSnapshot> Get(JobDefinitionId id)
        {
            var found = _items.FirstOrDefault(i => i.Id == id);
            return found != null ? Result<JobDefinitionSnapshot>.Success(found) : Result<JobDefinitionSnapshot>.Failure("x", "x");
        }

        public IReadOnlyCollection<JobDefinitionSnapshot> GetAll() => _items;
    }

    internal sealed class Roster : IEmployeeDefinitionProvider
    {
        public static readonly EmployeeDefinitionId Rookie = new EmployeeDefinitionId("employee.rookie");

        private readonly EmployeeDefinitionSnapshot _rookie =
            new EmployeeDefinitionSnapshot(Rookie, "k", new Money(15000), new EmployeeStats(50, 25, 30));

        public Result<EmployeeDefinitionSnapshot> Get(EmployeeDefinitionId id) => id == Rookie
            ? Result<EmployeeDefinitionSnapshot>.Success(_rookie)
            : Result<EmployeeDefinitionSnapshot>.Failure("x", "x");

        public IReadOnlyCollection<EmployeeDefinitionSnapshot> GetAll() => new[] { _rookie };
    }

    internal sealed class EmployeeFixture
    {
        public EventBus Bus { get; } = new EventBus();
        public FakeWallet Wallet { get; } = new FakeWallet();
        public FakeCompany Company { get; } = new FakeCompany();
        public FakeGameClock Clock { get; } = new FakeGameClock();
        public InMemoryEmployeeRepository Employees { get; } = new InMemoryEmployeeRepository();
        public InMemoryEmployeeAssignmentRepository Assignments { get; } = new InMemoryEmployeeAssignmentRepository();
        public HireEmployeeHandler Hire { get; }
        public AssignEmployeeHandler Assign { get; }
        public ResolveEmployeeJobsHandler Resolve { get; }

        public EmployeeFixture()
        {
            Hire = new HireEmployeeHandler(new Roster(), Employees, Company, Wallet, new SequentialIdGenerator(), Clock, Bus);
            Assign = new AssignEmployeeHandler(Employees, Assignments, new Jobs(), new SequentialIdGenerator(), new SeededRandomSource(5), Clock, Bus);
            Resolve = new ResolveEmployeeJobsHandler(
                Employees, Assignments, new Jobs(), new EmployeeJobSimulator(), new ShareEmployeeRevenueCalculator(), Bus);
        }

        public Employee HireOne()
        {
            Hire.Handle(new HireEmployeeCommand(Roster.Rookie));
            return Employees.GetAll().Last();
        }
    }

    public sealed class HiringTests
    {
        [Test]
        public void Hiring_PaysAndCreatesAnIdleEmployee()
        {
            var f = new EmployeeFixture();
            using var hired = f.Bus.Record<EmployeeHiredEvent>();

            var result = f.Hire.Handle(new HireEmployeeCommand(Roster.Rookie));

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(85000, f.Wallet.Balance.Cents);
            Assert.IsTrue(f.Employees.GetAll().Single().IsIdle);
            Assert.AreEqual(1, hired.Count);
        }

        [Test]
        public void Hiring_RequiresACompany()
        {
            var f = new EmployeeFixture();
            f.Company.Company = null;

            Assert.AreEqual(EmployeeErrors.NoCompany, f.Hire.Handle(new HireEmployeeCommand(Roster.Rookie)).ErrorCode);
        }

        [Test]
        public void Hiring_StopsAtTheCompanyCapacity()
        {
            var f = new EmployeeFixture();

            for (var i = 0; i < f.Company.Company.EmployeeCapacity; i++)
                Assert.IsTrue(f.Hire.Handle(new HireEmployeeCommand(Roster.Rookie)).IsSuccess);

            Assert.AreEqual(EmployeeErrors.CapacityReached, f.Hire.Handle(new HireEmployeeCommand(Roster.Rookie)).ErrorCode);
        }

        [Test]
        public void Hiring_FailsWithoutFunds_AndCreatesNobody()
        {
            var f = new EmployeeFixture();
            f.Wallet.Balance = new Money(10);

            Assert.AreEqual(EconomyErrors.InsufficientFunds, f.Hire.Handle(new HireEmployeeCommand(Roster.Rookie)).ErrorCode);
            Assert.AreEqual(0, f.Employees.GetAll().Count);
        }
    }

    public sealed class EmployeeAssignmentTests
    {
        [Test]
        public void EmployeeCannotBeAssigned_WhenNotIdle()
        {
            var f = new EmployeeFixture();
            var employee = f.HireOne();
            f.Assign.Handle(new AssignEmployeeCommand(employee.Id, Jobs.Small));

            var again = f.Assign.Handle(new AssignEmployeeCommand(employee.Id, Jobs.Small));

            Assert.AreEqual(EmployeeErrors.NotIdle, again.ErrorCode);
        }

        [Test]
        public void Assign_StartsWorking_AndPlansAFinishDay()
        {
            var f = new EmployeeFixture();
            var employee = f.HireOne();
            using var assigned = f.Bus.Record<EmployeeAssignedEvent>();

            f.Assign.Handle(new AssignEmployeeCommand(employee.Id, Jobs.Small));

            Assert.AreEqual(EmployeeState.Working, f.Employees.Get(employee.Id).Value.State);
            // difficulty 2, speed skill 25 => 2 - 1 = 1 day
            Assert.AreEqual(2, assigned.Last.FinishDay);
        }

        [Test]
        public void SlowerWork_TakesLonger()
        {
            var f = new EmployeeFixture();
            var employee = f.HireOne();
            using var assigned = f.Bus.Record<EmployeeAssignedEvent>();

            f.Assign.Handle(new AssignEmployeeCommand(employee.Id, Jobs.Hard));

            Assert.AreEqual(1 + 5, assigned.Last.FinishDay); // difficulty 6 - 1
        }

        [Test]
        public void UnknownEmployeeOrJob_IsRejected()
        {
            var f = new EmployeeFixture();
            var employee = f.HireOne();

            Assert.AreEqual(EmployeeErrors.NotFound, f.Assign.Handle(new AssignEmployeeCommand(new EmployeeId("ghost"), Jobs.Small)).ErrorCode);
            Assert.AreEqual(EmployeeErrors.UnknownJob, f.Assign.Handle(new AssignEmployeeCommand(employee.Id, new JobDefinitionId("nope"))).ErrorCode);
        }

        [Test]
        public void CancelAssignment_ReturnsTheEmployeeToIdle()
        {
            var f = new EmployeeFixture();
            var employee = f.HireOne();
            f.Assign.Handle(new AssignEmployeeCommand(employee.Id, Jobs.Small));
            var cancel = new CancelEmployeeAssignmentHandler(f.Employees, f.Assignments);

            Assert.IsTrue(cancel.Handle(new CancelEmployeeAssignmentCommand(employee.Id)).IsSuccess);

            Assert.IsTrue(f.Employees.Get(employee.Id).Value.IsIdle);
            Assert.AreEqual(0, f.Assignments.GetAll().Count);
        }

        [Test]
        public void Firing_RemovesTheEmployeeAndTheirAssignment()
        {
            var f = new EmployeeFixture();
            var employee = f.HireOne();
            f.Assign.Handle(new AssignEmployeeCommand(employee.Id, Jobs.Small));
            var fire = new FireEmployeeHandler(f.Employees, f.Assignments, f.Bus);

            Assert.IsTrue(fire.Handle(new FireEmployeeCommand(employee.Id)).IsSuccess);

            Assert.AreEqual(0, f.Employees.GetAll().Count);
            Assert.AreEqual(0, f.Assignments.GetAll().Count);
        }
    }

    public sealed class EmployeeResolutionTests
    {
        [Test]
        public void EmployeeJob_ShouldResolve_OnFinishDay()
        {
            var f = new EmployeeFixture();
            var employee = f.HireOne();
            f.Assign.Handle(new AssignEmployeeCommand(employee.Id, Jobs.Hard)); // finishes on day 6
            using var completed = f.Bus.Record<EmployeeJobCompletedEvent>();

            f.Resolve.Handle(new ResolveEmployeeJobsCommand(5));
            Assert.AreEqual(0, completed.Count);
            Assert.AreEqual(EmployeeState.Working, f.Employees.Get(employee.Id).Value.State);

            f.Resolve.Handle(new ResolveEmployeeJobsCommand(6));
            Assert.AreEqual(1, completed.Count);
            Assert.IsTrue(f.Employees.Get(employee.Id).Value.IsIdle);
        }

        [Test]
        public void EmployeeJob_ShouldGenerateRevenue()
        {
            var f = new EmployeeFixture();
            var employee = f.HireOne();
            f.Assign.Handle(new AssignEmployeeCommand(employee.Id, Jobs.Small));
            using var revenue = f.Bus.Record<EmployeeRevenueGeneratedEvent>();

            f.Resolve.Handle(new ResolveEmployeeJobsCommand(10));

            Assert.AreEqual(1, revenue.Count);
            Assert.Greater(revenue.Last.Revenue.Cents, 0);
            Assert.AreEqual(employee.Id, revenue.Last.EmployeeId);
        }

        [Test]
        public void ResolvedAssignments_AreNotResolvedTwice()
        {
            var f = new EmployeeFixture();
            var employee = f.HireOne();
            f.Assign.Handle(new AssignEmployeeCommand(employee.Id, Jobs.Small));
            using var revenue = f.Bus.Record<EmployeeRevenueGeneratedEvent>();

            f.Resolve.Handle(new ResolveEmployeeJobsCommand(10));
            f.Resolve.Handle(new ResolveEmployeeJobsCommand(11));

            Assert.AreEqual(1, revenue.Count);
        }

        [Test]
        public void EmployeeOutcome_ShouldBeDeterministic_WithSameSeed()
        {
            var employee = new Employee(new EmployeeId("e"), Roster.Rookie, new EmployeeStats(60, 20, 20), 1);
            var job = new Jobs().Get(Jobs.Small).Value;
            var simulator = new EmployeeJobSimulator();

            var first = simulator.Simulate(employee, job, 1234);
            var second = simulator.Simulate(employee, job, 1234);

            Assert.AreEqual(first.Quality, second.Quality);
            Assert.AreEqual(first.Reward, second.Reward);
        }

        [Test]
        public void SkilledEmployees_DoBetter_ThanUnskilledOnes()
        {
            var job = new Jobs().Get(Jobs.Hard).Value;
            var simulator = new EmployeeJobSimulator();
            var expert = new Employee(new EmployeeId("a"), Roster.Rookie, new EmployeeStats(100, 20, 20), 1);
            var novice = new Employee(new EmployeeId("b"), Roster.Rookie, new EmployeeStats(10, 20, 20), 1);

            Assert.Greater(simulator.Simulate(expert, job, 7).Quality, simulator.Simulate(novice, job, 7).Quality);
        }

        [Test]
        public void RewardStaysWithinTheBaseRewardBand()
        {
            var employee = new Employee(new EmployeeId("e"), Roster.Rookie, new EmployeeStats(150, 20, 20), 1);
            var job = new Jobs().Get(Jobs.Small).Value;

            for (var seed = 0; seed < 50; seed++)
            {
                var outcome = new EmployeeJobSimulator().Simulate(employee, job, seed);
                Assert.That(outcome.Reward.Cents, Is.InRange(12000L, 20000L)); // 60%..100% of the base reward
            }
        }

        [Test]
        public void GameDayEnded_TriggersResolution()
        {
            var f = new EmployeeFixture();
            var employee = f.HireOne();
            f.Assign.Handle(new AssignEmployeeCommand(employee.Id, Jobs.Small)); // due day 2
            using var projection = new EmployeeDayProjection(f.Bus, f.Resolve);
            using var revenue = f.Bus.Record<EmployeeRevenueGeneratedEvent>();

            f.Bus.Publish(new GameDayEndedEvent(1));
            Assert.AreEqual(0, revenue.Count);

            f.Bus.Publish(new GameDayEndedEvent(2));
            Assert.AreEqual(1, revenue.Count);
        }

        [Test]
        public void StateMachine_RejectsSkippingSteps()
        {
            var employee = new Employee(new EmployeeId("e"), Roster.Rookie, new EmployeeStats(1, 1, 1), 1);

            Assert.IsTrue(employee.Start().IsFailure); // Idle -> Working is not allowed
            Assert.IsTrue(employee.Assign().IsSuccess);
            Assert.IsTrue(employee.Resolve().IsFailure); // Assigned -> Completed is not allowed
        }
    }
}
