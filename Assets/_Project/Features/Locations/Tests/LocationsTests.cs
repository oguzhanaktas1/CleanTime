using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CleanTime.Jobs.Domain;
using CleanTime.Locations.Application;
using CleanTime.Locations.Domain;
using CleanTime.Locations.Infrastructure;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;

namespace CleanTime.Locations.Tests
{
    internal sealed class LocationDefinitions : ILocationDefinitionProvider
    {
        public static readonly LocationId Small = new LocationId("location.apartment.small.001");
        public static readonly LocationId Large = new LocationId("location.apartment.large.001");

        private readonly List<LocationDefinitionSnapshot> _items = new List<LocationDefinitionSnapshot>
        {
            new LocationDefinitionSnapshot(Small, "k", PropertyTier.SmallApartment, "Property.Small", true, null),
            new LocationDefinitionSnapshot(Large, "k", PropertyTier.LargeApartment, "Property.Large", false,
                new[] { new LocationUnlockRequirement(LocationRequirementTypes.CompletionCount, Small, 6) }),
        };

        public Result<LocationDefinitionSnapshot> Get(LocationId id)
        {
            var found = _items.FirstOrDefault(i => i.Id == id);
            return found != null ? Result<LocationDefinitionSnapshot>.Success(found) : Result<LocationDefinitionSnapshot>.Failure("x", "x");
        }

        public IReadOnlyCollection<LocationDefinitionSnapshot> GetAll() => _items;
    }

    public sealed class LocationUnlockTests
    {
        private static ProgressionSnapshot Completions(int small) =>
            new ProgressionSnapshot(new Dictionary<LocationId, int> { { LocationDefinitions.Small, small } });

        private static LocationUnlockEvaluator Evaluator() =>
            new LocationUnlockEvaluator(new ILocationUnlockPolicy[] { new CompletionCountUnlockPolicy() });

        [Test]
        public void CompletionCountPolicy_NeedsTheRequiredCount()
        {
            var requirement = new LocationUnlockRequirement(LocationRequirementTypes.CompletionCount, LocationDefinitions.Small, 6);
            var policy = new CompletionCountUnlockPolicy();

            Assert.IsFalse(policy.IsSatisfied(requirement, Completions(5)));
            Assert.IsTrue(policy.IsSatisfied(requirement, Completions(6)));
            Assert.IsTrue(policy.IsSatisfied(requirement, Completions(9)));
        }

        [Test]
        public void Evaluator_UnlocksLocationsWhoseRequirementsAreMet()
        {
            var large = new LocationDefinitions().Get(LocationDefinitions.Large).Value;

            Assert.IsFalse(Evaluator().IsUnlockable(large, Completions(5)));
            Assert.IsTrue(Evaluator().IsUnlockable(large, Completions(6)));
        }

        [Test]
        public void LocationsWithoutRequirements_FollowTheDefaultFlag()
        {
            var small = new LocationDefinitions().Get(LocationDefinitions.Small).Value;
            var locked = new LocationDefinitionSnapshot(new LocationId("x"), "k", PropertyTier.Villa, "s", false, null);

            Assert.IsTrue(Evaluator().IsUnlockable(small, Completions(0)));
            Assert.IsFalse(Evaluator().IsUnlockable(locked, Completions(100)));
        }

        [Test]
        public void UnknownRequirementTypes_NeverUnlock()
        {
            var weird = new LocationDefinitionSnapshot(new LocationId("x"), "k", PropertyTier.Villa, "s", false,
                new[] { new LocationUnlockRequirement("mystery", LocationDefinitions.Small, 0) });

            Assert.IsFalse(Evaluator().IsUnlockable(weird, Completions(100)));
        }
    }

    public sealed class LocationProgressTests
    {
        [Test]
        public void RegisterCompletion_TracksCountBestScoreAndBestTime()
        {
            var progress = new LocationProgress(LocationDefinitions.Small, unlocked: true);

            progress.RegisterCompletion(0.6f, 120f, 3);
            progress.RegisterCompletion(0.9f, 150f, 4);
            progress.RegisterCompletion(0.7f, 90f, 5);

            Assert.AreEqual(3, progress.CompletionCount);
            Assert.AreEqual(0.9f, progress.BestScore.Value, 0.0001f);
            Assert.AreEqual(90, progress.BestCompletionSeconds.Value);
            Assert.AreEqual(5, progress.LastCompletedDay.Value);
        }

        [Test]
        public void Unlock_ReportsWhetherItChangedAnything()
        {
            var progress = new LocationProgress(LocationDefinitions.Large);

            Assert.IsTrue(progress.Unlock());
            Assert.IsFalse(progress.Unlock());
        }
    }

    public sealed class LocationCompletionProjectionTests
    {
        private static JobCompletedEvent Completed(LocationId location, float score = 0.8f) =>
            new JobCompletedEvent(new JobId("j"), new JobDefinitionId("d"), location, new Money(1), score,
                new CustomerEvaluation(1, 1, 1, 1, score), 30f, 2);

        [Test]
        public void JobCompleted_IncrementsTheCompletionCount_AndAnnouncesIt()
        {
            var bus = new EventBus();
            var repository = new InMemoryLocationProgressRepository(new LocationDefinitions(), new InMemorySnapshotStore());
            using var projection = new LocationCompletionProjection(repository, bus, NullLogger.Instance);
            using var changed = bus.Record<LocationCompletionChangedEvent>();

            bus.Publish(Completed(LocationDefinitions.Small));
            bus.Publish(Completed(LocationDefinitions.Small));

            Assert.AreEqual(2, repository.Get(LocationDefinitions.Small).Value.CompletionCount);
            Assert.AreEqual(2, changed.Last.CompletionCount);
        }

        [Test]
        public void UnknownLocations_AreIgnored()
        {
            var bus = new EventBus();
            var repository = new InMemoryLocationProgressRepository(new LocationDefinitions(), new InMemorySnapshotStore());
            using var projection = new LocationCompletionProjection(repository, bus, NullLogger.Instance);
            using var changed = bus.Record<LocationCompletionChangedEvent>();

            bus.Publish(Completed(new LocationId("location.nowhere")));

            Assert.AreEqual(0, changed.Count);
        }

        [Test]
        public void SaveLoadRoundTrip_PreservesProgress()
        {
            var store = new InMemorySnapshotStore();
            var repository = new InMemoryLocationProgressRepository(new LocationDefinitions(), store);
            var small = repository.Get(LocationDefinitions.Small).Value;
            small.RegisterCompletion(0.75f, 61f, 9);
            repository.Get(LocationDefinitions.Large).Value.Unlock();
            var slot = SaveId.New();
            repository.Flush(slot);

            repository.Reset();
            Assert.AreEqual(0, repository.Get(LocationDefinitions.Small).Value.CompletionCount);

            Assert.IsTrue(repository.Load(slot).IsSuccess);
            var restored = repository.Get(LocationDefinitions.Small).Value;
            Assert.AreEqual(1, restored.CompletionCount);
            Assert.AreEqual(0.75f, restored.BestScore.Value, 0.0001f);
            Assert.AreEqual(61, restored.BestCompletionSeconds.Value);
            Assert.AreEqual(9, restored.LastCompletedDay.Value);
            Assert.IsTrue(repository.Get(LocationDefinitions.Large).Value.Unlocked);
        }

        [Test]
        public void UnlockedQuery_ListsOnlyUnlockedLocations()
        {
            var repository = new InMemoryLocationProgressRepository(new LocationDefinitions(), new InMemorySnapshotStore());
            var query = new GetUnlockedLocationsHandler(new LocationDefinitions(), repository);

            Assert.AreEqual(1, query.Handle(new GetUnlockedLocationsQuery()).Count);

            repository.Get(LocationDefinitions.Large).Value.Unlock();
            Assert.AreEqual(2, query.Handle(new GetUnlockedLocationsQuery()).Count);
        }
    }
}
