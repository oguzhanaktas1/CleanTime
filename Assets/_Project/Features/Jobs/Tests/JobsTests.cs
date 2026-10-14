using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CleanTime.Cleaning.Domain;
using CleanTime.Jobs.Application;
using CleanTime.Jobs.Domain;
using CleanTime.Jobs.Infrastructure;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;

namespace CleanTime.Jobs.Tests
{
    internal sealed class JobDefinitions : IJobDefinitionProvider
    {
        public static readonly JobDefinitionId Small = new JobDefinitionId("job.small_apartment.001");
        public static readonly LocationId SmallLocation = new LocationId("location.apartment.small.001");

        private readonly List<JobDefinitionSnapshot> _items = new List<JobDefinitionSnapshot>
        {
            new JobDefinitionSnapshot(
                Small, SmallLocation, new NpcId("npc.customer.001"), 1, new Money(25000),
                new[] { new JobObjective(JobObjectiveTypes.PropertyCleaning, 1f) }, parDurationSeconds: 100f),
        };

        public Result<JobDefinitionSnapshot> Get(JobDefinitionId id)
        {
            var found = _items.FirstOrDefault(i => i.Id == id);
            return found != null ? Result<JobDefinitionSnapshot>.Success(found) : Result<JobDefinitionSnapshot>.Failure("x", "x");
        }

        public IReadOnlyCollection<JobDefinitionSnapshot> GetAll() => _items;
    }

    internal sealed class SwitchablePolicy : IJobAvailabilityPolicy
    {
        public bool Available { get; set; } = true;

        public bool IsAvailable(JobDefinitionSnapshot definition) => Available;
    }

    internal sealed class JobsFixture
    {
        public EventBus Bus { get; } = new EventBus();
        public InMemoryJobRepository Repository { get; } = new InMemoryJobRepository();
        public SwitchablePolicy Availability { get; } = new SwitchablePolicy();
        public FakeGameClock GameClock { get; } = new FakeGameClock();
        public FakeUtcClock UtcClock { get; } = new FakeUtcClock();
        public JobDefinitions Definitions { get; } = new JobDefinitions();
        public AcceptJobHandler Accept { get; }
        public StartJobHandler Start { get; }
        public CompleteJobHandler Complete { get; }
        public AbandonJobHandler Abandon { get; }
        public FinishJobResultHandler Finish { get; }

        public JobsFixture()
        {
            Accept = new AcceptJobHandler(Definitions, Repository, Availability, new SequentialIdGenerator(), new SeededRandomSource(1), Bus);
            Start = new StartJobHandler(Repository, GameClock, UtcClock, Bus);
            Complete = new CompleteJobHandler(
                Repository,
                new CustomerRatingCalculator(CustomerEvaluationWeights.Default),
                new QualityScaledRewardCalculator(),
                new JobEvaluationSettings(mistakeTolerance: 10),
                GameClock,
                UtcClock,
                Bus);
            Abandon = new AbandonJobHandler(Repository, Bus);
            Finish = new FinishJobResultHandler(Repository, Bus);
        }

        public JobId AcceptAndStart()
        {
            Accept.Handle(new AcceptJobCommand(JobDefinitions.Small));
            var id = Repository.GetActive().Value.Id;
            Start.Handle(new StartJobCommand(id));
            return id;
        }

        public static CleaningResult Cleaned(float progress = 1f, int wrong = 0) =>
            new CleaningResult(progress, 4, 4, wrong, 12);
    }

    public sealed class JobLifecycleTests
    {
        [Test]
        public void Accept_CreatesAnAcceptedActiveJob()
        {
            var f = new JobsFixture();
            using var accepted = f.Bus.Record<JobAcceptedEvent>();

            var result = f.Accept.Handle(new AcceptJobCommand(JobDefinitions.Small));

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(JobState.Accepted, f.Repository.GetActive().Value.State);
            Assert.AreEqual(1, accepted.Count);
        }

        [Test]
        public void Accept_FailsWhileAnotherJobIsActive()
        {
            var f = new JobsFixture();
            f.Accept.Handle(new AcceptJobCommand(JobDefinitions.Small));

            Assert.AreEqual(JobErrors.AlreadyActive, f.Accept.Handle(new AcceptJobCommand(JobDefinitions.Small)).ErrorCode);
        }

        [Test]
        public void Accept_RejectsUnknownAndUnavailableJobs()
        {
            var f = new JobsFixture();

            Assert.AreEqual(JobErrors.NotFound, f.Accept.Handle(new AcceptJobCommand(new JobDefinitionId("nope"))).ErrorCode);

            f.Availability.Available = false;
            Assert.AreEqual(JobErrors.NotAvailable, f.Accept.Handle(new AcceptJobCommand(JobDefinitions.Small)).ErrorCode);
        }

        [Test]
        public void Start_MovesToInProgress_AndStoresTheDay()
        {
            var f = new JobsFixture();
            f.GameClock.Now = new GameDate(4, DayPhase.Morning);

            f.AcceptAndStart();

            var job = f.Repository.GetActive().Value;
            Assert.AreEqual(JobState.InProgress, job.State);
            Assert.AreEqual(4, job.StartedDay);
        }

        [Test]
        public void Complete_BeforeStart_IsRejected()
        {
            var f = new JobsFixture();
            f.Accept.Handle(new AcceptJobCommand(JobDefinitions.Small));
            var id = f.Repository.GetActive().Value.Id;

            Assert.AreEqual(JobErrors.InvalidTransition, f.Complete.Handle(new CompleteJobCommand(id, JobsFixture.Cleaned())).ErrorCode);
        }

        [Test]
        public void Complete_WithAnotherJobsId_IsRejected()
        {
            var f = new JobsFixture();
            f.AcceptAndStart();

            Assert.AreEqual(JobErrors.WrongJob, f.Complete.Handle(new CompleteJobCommand(new JobId("other"), JobsFixture.Cleaned())).ErrorCode);
        }

        [Test]
        public void PerfectJob_PaysTheMaximumMultiplier_AndStoresAPendingResult()
        {
            var f = new JobsFixture();
            var id = f.AcceptAndStart();
            f.UtcClock.Advance(TimeSpan.FromSeconds(60)); // under the 100s par time
            using var completed = f.Bus.Record<JobCompletedEvent>();
            using var rating = f.Bus.Record<CustomerRatingCalculatedEvent>();

            var result = f.Complete.Handle(new CompleteJobCommand(id, JobsFixture.Cleaned()));

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1f, completed.Last.CustomerScore, 0.001f);
            Assert.AreEqual(31250, completed.Last.Reward.Cents); // 25000 * 1.25
            Assert.AreEqual(5, completed.Last.Evaluation.Stars);
            Assert.AreEqual(1, rating.Count);
            Assert.IsTrue(f.Repository.GetActive().IsFailure);
            Assert.IsTrue(f.Repository.GetPendingResult().IsSuccess);
            Assert.AreEqual(1, f.Repository.GetHistory().Count);
        }

        [Test]
        public void Mistakes_LowerTheScore()
        {
            var f = new JobsFixture();
            var id = f.AcceptAndStart();
            using var completed = f.Bus.Record<JobCompletedEvent>();

            f.Complete.Handle(new CompleteJobCommand(id, JobsFixture.Cleaned(wrong: 10)));

            Assert.AreEqual(0f, completed.Last.Evaluation.Mistakes, 0.001f);
            Assert.AreEqual(0.9f, completed.Last.CustomerScore, 0.001f);
        }

        [Test]
        public void SlowJobs_LoseEfficiency()
        {
            var f = new JobsFixture();
            var id = f.AcceptAndStart();
            f.UtcClock.Advance(TimeSpan.FromSeconds(200)); // twice the par time
            using var completed = f.Bus.Record<JobCompletedEvent>();

            f.Complete.Handle(new CompleteJobCommand(id, JobsFixture.Cleaned()));

            Assert.AreEqual(0.5f, completed.Last.Evaluation.Efficiency, 0.001f);
        }

        [Test]
        public void NewJobs_AreBlocked_UntilTheResultIsDismissed()
        {
            var f = new JobsFixture();
            var id = f.AcceptAndStart();
            f.Complete.Handle(new CompleteJobCommand(id, JobsFixture.Cleaned()));
            using var acknowledged = f.Bus.Record<JobResultAcknowledgedEvent>();

            Assert.AreEqual(JobErrors.ResultPending, f.Accept.Handle(new AcceptJobCommand(JobDefinitions.Small)).ErrorCode);

            Assert.IsTrue(f.Finish.Handle(new FinishJobResultCommand()).IsSuccess);
            Assert.AreEqual(1, acknowledged.Count);
            Assert.IsTrue(f.Accept.Handle(new AcceptJobCommand(JobDefinitions.Small)).IsSuccess);
        }

        [Test]
        public void Abandon_FailsTheJob_AndFreesTheSlot()
        {
            var f = new JobsFixture();
            var id = f.AcceptAndStart();
            using var failed = f.Bus.Record<JobFailedEvent>();

            Assert.IsTrue(f.Abandon.Handle(new AbandonJobCommand(id)).IsSuccess);

            Assert.AreEqual(1, failed.Count);
            Assert.IsTrue(f.Repository.GetActive().IsFailure);
            Assert.AreEqual(JobErrors.NoActiveJob, f.Complete.Handle(new CompleteJobCommand(id, JobsFixture.Cleaned())).ErrorCode);
        }

        [Test]
        public void JobCoordinator_CompletesTheJob_WhenItsPropertyIsCleaned()
        {
            var f = new JobsFixture();
            using var coordinator = new JobCoordinator(f.Repository, f.Complete, f.Bus, NullLogger.Instance);
            var id = f.AcceptAndStart();
            using var completed = f.Bus.Record<JobCompletedEvent>();

            f.Bus.Publish(new PropertyCompletedEvent(new LocationId("location.elsewhere"), JobsFixture.Cleaned()));
            Assert.AreEqual(0, completed.Count);

            f.Bus.Publish(new PropertyCompletedEvent(JobDefinitions.SmallLocation, JobsFixture.Cleaned()));
            Assert.AreEqual(1, completed.Count);
            Assert.AreEqual(id, completed.Last.JobId);
        }

        [Test]
        public void JobCoordinator_ForwardsPropertyProgress()
        {
            var f = new JobsFixture();
            using var coordinator = new JobCoordinator(f.Repository, f.Complete, f.Bus, NullLogger.Instance);
            f.AcceptAndStart();
            using var progress = f.Bus.Record<JobObjectiveProgressChangedEvent>();

            f.Bus.Publish(new PropertyCleaningProgressChangedEvent(JobDefinitions.SmallLocation, 0.4f));

            Assert.AreEqual(0.4f, f.Repository.GetActive().Value.CleaningProgress, 0.001f);
            Assert.AreEqual(1, progress.Count);
        }

        [Test]
        public void AvailableJobs_FollowTheAvailabilityPolicy()
        {
            var f = new JobsFixture();
            var query = new GetAvailableJobsHandler(f.Definitions, f.Availability);

            Assert.AreEqual(1, query.Handle(new GetAvailableJobsQuery()).Count);

            f.Availability.Available = false;
            Assert.AreEqual(0, query.Handle(new GetAvailableJobsQuery()).Count);
        }
    }

    public sealed class CustomerEvaluationTests
    {
        [Test]
        public void Weights_MustSumToOne()
        {
            Assert.Throws<ArgumentException>(() => new CustomerEvaluationWeights(0.5f, 0.5f, 0.5f, 0.5f));
            Assert.DoesNotThrow(() => new CustomerEvaluationWeights(0.7f, 0.1f, 0.1f, 0.1f));
        }

        [Test]
        public void Score_IsTheWeightedSum()
        {
            var calculator = new CustomerRatingCalculator(CustomerEvaluationWeights.Default);

            var evaluation = calculator.Calculate(new CustomerEvaluationInput(1f, 0f, 0.5f, 1f));

            Assert.AreEqual(1f * 0.6f + 0f * 0.2f + 0.5f * 0.1f + 1f * 0.1f, evaluation.Score, 0.0001f);
        }

        [Test]
        public void Inputs_AreClampedToTheUnitRange()
        {
            var input = new CustomerEvaluationInput(2f, -1f, 5f, -3f);

            Assert.AreEqual(1f, input.Cleaning);
            Assert.AreEqual(0f, input.Optional);
        }

        [Test]
        public void Stars_AreBetweenOneAndFive()
        {
            Assert.AreEqual(1, new CustomerEvaluation(0, 0, 0, 0, 0f).Stars);
            Assert.AreEqual(5, new CustomerEvaluation(1, 1, 1, 1, 1f).Stars);
            Assert.AreEqual(3, new CustomerEvaluation(0, 0, 0, 0, 0.6f).Stars);
        }

        [Test]
        public void RewardScalesBetweenTheConfiguredMultipliers()
        {
            var job = new JobDefinitions().GetAll().First();
            var calculator = new QualityScaledRewardCalculator(0.5m, 1.25m);

            Assert.AreEqual(12500, calculator.Calculate(job, new CustomerEvaluation(0, 0, 0, 0, 0f)).Cents);
            Assert.AreEqual(31250, calculator.Calculate(job, new CustomerEvaluation(1, 1, 1, 1, 1f)).Cents);
        }
    }
}
