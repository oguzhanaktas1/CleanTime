using System.Collections.Generic;
using NUnit.Framework;
using CleanTime.Jobs.Domain;
using CleanTime.Locations.Application;
using CleanTime.Locations.Domain;
using CleanTime.Locations.Infrastructure;
using CleanTime.Progression.Application;
using CleanTime.Progression.Domain;
using CleanTime.Progression.Infrastructure;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;

namespace CleanTime.Progression.Tests
{
    internal sealed class Locations : ILocationDefinitionProvider
    {
        public static readonly LocationId Small = new LocationId("location.apartment.small.001");
        public static readonly LocationId Large = new LocationId("location.apartment.large.001");

        private readonly List<LocationDefinitionSnapshot> _items = new List<LocationDefinitionSnapshot>
        {
            new LocationDefinitionSnapshot(Small, "k", PropertyTier.SmallApartment, "s", true, null),
            new LocationDefinitionSnapshot(Large, "k", PropertyTier.LargeApartment, "s", false,
                new[] { new LocationUnlockRequirement(LocationRequirementTypes.CompletionCount, Small, 6) }),
        };

        public Result<LocationDefinitionSnapshot> Get(LocationId id) =>
            Result<LocationDefinitionSnapshot>.Success(_items.Find(i => i.Id == id));

        public IReadOnlyCollection<LocationDefinitionSnapshot> GetAll() => _items;
    }

    internal sealed class ProgressionFixture
    {
        public EventBus Bus { get; } = new EventBus();
        public InMemoryLocationProgressRepository LocationProgress { get; }
        public InMemoryProgressionRepository Progression { get; }
        public ProgressionProjection Projection { get; }
        public LocationCompletionProjection Completions { get; }

        public ProgressionFixture(int companyAfterJobs = 3, params int[] tierThresholds)
        {
            var definitions = new Locations();
            var store = new InMemorySnapshotStore();
            LocationProgress = new InMemoryLocationProgressRepository(definitions, store);
            Progression = new InMemoryProgressionRepository(store);

            var unlockLocation = new UnlockLocationHandler(LocationProgress, Bus);
            var unlockCompany = new UnlockCompanyHandler(Progression, Bus);
            var milestone = new ApplyStoryMilestoneHandler(Progression, Bus);

            var evaluate = new EvaluateProgressionHandler(
                Progression,
                definitions,
                LocationProgress,
                new LocationUnlockEvaluator(new ILocationUnlockPolicy[] { new CompletionCountUnlockPolicy() }),
                new IProgressionRule[] { new CompanyUnlockRule(companyAfterJobs), new JobTierRule(tierThresholds) },
                unlockLocation,
                unlockCompany,
                milestone,
                Bus);

            // Same subscription order as production: locations count first, progression second.
            Completions = new LocationCompletionProjection(LocationProgress, Bus, NullLogger.Instance);
            Projection = new ProgressionProjection(Progression, evaluate, Bus);
        }

        public void CompleteJob(LocationId location) =>
            Bus.Publish(new JobCompletedEvent(
                new JobId("j"), new JobDefinitionId("d"), location, new Money(100), 0.9f,
                new CustomerEvaluation(1, 1, 1, 1, 0.9f), 60f, 1));
    }

    public sealed class LocationUnlockProgressionTests
    {
        [Test]
        public void Location_ShouldRemainLocked_AfterFiveCompletions()
        {
            var f = new ProgressionFixture();

            for (var i = 0; i < 5; i++)
                f.CompleteJob(Locations.Small);

            Assert.IsFalse(f.LocationProgress.Get(Locations.Large).Value.Unlocked);
        }

        [Test]
        public void Location_ShouldUnlock_AfterSixCompletions()
        {
            var f = new ProgressionFixture();
            using var unlocked = f.Bus.Record<LocationUnlockedEvent>();

            for (var i = 0; i < 6; i++)
                f.CompleteJob(Locations.Small);

            Assert.IsTrue(f.LocationProgress.Get(Locations.Large).Value.Unlocked);
            Assert.AreEqual(1, unlocked.Count);
            Assert.AreEqual(Locations.Large, unlocked.Last.LocationId);
        }

        [Test]
        public void Unlocking_IsIdempotent()
        {
            var f = new ProgressionFixture();
            using var unlocked = f.Bus.Record<LocationUnlockedEvent>();

            for (var i = 0; i < 9; i++)
                f.CompleteJob(Locations.Small);

            Assert.AreEqual(1, unlocked.Count);
        }
    }

    public sealed class PlayerProgressionTests
    {
        [Test]
        public void Company_ShouldUnlock_AtConfiguredMilestone()
        {
            var f = new ProgressionFixture(companyAfterJobs: 3);
            using var company = f.Bus.Record<CompanyUnlockedEvent>();

            f.CompleteJob(Locations.Small);
            f.CompleteJob(Locations.Small);
            Assert.IsFalse(f.Progression.Get().Value.CompanyUnlocked);

            f.CompleteJob(Locations.Small);

            Assert.IsTrue(f.Progression.Get().Value.CompanyUnlocked);
            Assert.AreEqual(1, company.Count);
        }

        [Test]
        public void JobTier_RisesWithEachThreshold()
        {
            var f = new ProgressionFixture(100, 2, 4);
            using var tiers = f.Bus.Record<JobTierUnlockedEvent>();

            for (var i = 0; i < 4; i++)
                f.CompleteJob(Locations.Small);

            Assert.AreEqual(3, f.Progression.Get().Value.JobTier);
            Assert.AreEqual(2, tiers.Count);
        }

        [Test]
        public void TotalJobs_AreCountedAndAnnounced()
        {
            var f = new ProgressionFixture();
            using var changed = f.Bus.Record<PlayerProgressChangedEvent>();

            f.CompleteJob(Locations.Small);
            f.CompleteJob(Locations.Small);

            Assert.AreEqual(2, f.Progression.Get().Value.TotalJobsCompleted);
            Assert.AreEqual(2, changed.Last.TotalJobsCompleted);
        }

        [Test]
        public void StoryMilestones_AreRecordedOnce()
        {
            var f = new ProgressionFixture();
            var milestone = new ApplyStoryMilestoneHandler(f.Progression, f.Bus);
            using var reached = f.Bus.Record<StoryMilestoneReachedEvent>();

            milestone.Handle(new ApplyStoryMilestoneCommand("mother.final"));
            milestone.Handle(new ApplyStoryMilestoneCommand("mother.final"));

            Assert.AreEqual(1, reached.Count);
            Assert.AreEqual(1, f.Progression.Get().Value.StoryLevel);
        }

        [Test]
        public void JobCountMilestoneRule_FiresWhenReached()
        {
            var rule = new JobCountMilestoneRule("act1.done", 2);
            var progression = new PlayerProgression(totalJobsCompleted: 1);

            Assert.AreEqual(0, rule.Evaluate(progression).Count);

            progression.RegisterJobCompleted();
            Assert.AreEqual(1, rule.Evaluate(progression).Count);

            progression.ReachMilestone("act1.done");
            Assert.AreEqual(0, rule.Evaluate(progression).Count);
        }

        [Test]
        public void SaveLoadRoundTrip_PreservesProgression()
        {
            var f = new ProgressionFixture(companyAfterJobs: 1);
            f.CompleteJob(Locations.Small);
            f.Progression.Get().Value.ReachMilestone("m1");
            var slot = SaveId.New();
            var store = new InMemorySnapshotStore();
            var repository = new InMemoryProgressionRepository(store);
            repository.Save(f.Progression.Get().Value);
            repository.Flush(slot);

            repository.Reset();
            Assert.AreEqual(0, repository.Get().Value.TotalJobsCompleted);

            Assert.IsTrue(repository.Load(slot).IsSuccess);
            Assert.AreEqual(1, repository.Get().Value.TotalJobsCompleted);
            Assert.IsTrue(repository.Get().Value.CompanyUnlocked);
            CollectionAssert.Contains(repository.Get().Value.ReachedMilestones, "m1");
        }
    }
}
