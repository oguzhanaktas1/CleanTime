using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CleanTime.Cleaning.Application;
using CleanTime.Cleaning.Domain;
using CleanTime.Cleaning.Infrastructure;
using CleanTime.Equipment.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;

namespace CleanTime.Cleaning.Tests
{
    internal sealed class FakeEquipmentRepository : IEquipmentRepository
    {
        private readonly Dictionary<EquipmentId, EquipmentProgress> _items = new Dictionary<EquipmentId, EquipmentProgress>();

        public EquipmentLoadout Loadout { get; } = new EquipmentLoadout();

        public void Add(EquipmentDefinitionSnapshot definition, bool owned = true)
        {
            _items[definition.Id] = new EquipmentProgress(definition, owned);
            if (owned) Loadout.Add(definition.Id);
        }

        public Result<EquipmentProgress> Get(EquipmentId id) => _items.TryGetValue(id, out var item)
            ? Result<EquipmentProgress>.Success(item)
            : Result<EquipmentProgress>.Failure(EquipmentErrors.NotFound, id.Value);

        public Result<EquipmentProgress> GetOwned(EquipmentId id)
        {
            var found = Get(id);
            return found.IsSuccess && found.Value.IsOwned ? found : Result<EquipmentProgress>.Failure(EquipmentErrors.NotOwned, id.Value);
        }

        public IReadOnlyList<EquipmentProgress> GetAll() => _items.Values.ToList();

        public IReadOnlyList<EquipmentProgress> GetAllOwned() => _items.Values.Where(i => i.IsOwned).ToList();

        public Result Save(EquipmentProgress equipment) => Result.Success();

        public EquipmentLoadout GetLoadout() => Loadout;

        public Result SaveLoadout(EquipmentLoadout loadout) => Result.Success();
    }

    internal sealed class Fixture
    {
        public const string Dust = "dirt.dust";
        public const string Stain = "dirt.stain";

        public EventBus Bus { get; } = new EventBus();
        public FakeEquipmentRepository Equipment { get; } = new FakeEquipmentRepository();
        public InMemoryCleaningTargetRepository Targets { get; } = new InMemoryCleaningTargetRepository();
        public InMemoryCleaningSessionRepository Sessions { get; } = new InMemoryCleaningSessionRepository();
        public CleaningProgressCalculator Calculator { get; } = new CleaningProgressCalculator();
        public EquipmentId Broom { get; } = new EquipmentId("equipment.broom.basic");
        public EquipmentId Mop { get; } = new EquipmentId("equipment.mop.basic");
        public ApplyCleaningActionHandler Apply { get; }
        public StartCleaningSessionHandler Start { get; }
        public CleaningCompletionCoordinator Coordinator { get; }

        public Fixture()
        {
            Equipment.Add(new EquipmentDefinitionSnapshot(Broom, "equipment.broom", Money.Zero, 5f, new[] { Dust }, isStarter: true));
            Equipment.Add(new EquipmentDefinitionSnapshot(Mop, "equipment.mop", Money.Zero, 5f, new[] { Stain }, isStarter: true));

            var compatibility = new EquipmentCompatibilityPolicy();
            Apply = new ApplyCleaningActionHandler(Targets, Equipment, compatibility, Bus);
            Start = new StartCleaningSessionHandler(Targets, Sessions, Calculator, Bus);
            Coordinator = new CleaningCompletionCoordinator(Targets, Sessions, Calculator, Bus);
        }

        public Result StartSession(params CleaningTargetSeed[] seeds) =>
            Start.Handle(new StartCleaningSessionCommand(new LocationId("location.test"), new JobId("job-1"), seeds));

        public static CleaningTargetSeed Seed(string id, string type, float health = 10f, float weight = 1f, string room = "") =>
            new CleaningTargetSeed(id, new DirtTypeId(type), health, weight, room);
    }

    public sealed class CleaningTargetTests
    {
        [Test]
        public void CorrectTool_ShouldReduceHealth()
        {
            var f = new Fixture();
            f.StartSession(Fixture.Seed("t1", Fixture.Dust, health: 10f));

            var result = f.Apply.Handle(new ApplyCleaningActionCommand("t1", f.Broom));

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(5f, f.Targets.Get("t1").Value.CurrentHealth, 0.001f);
        }

        [Test]
        public void WrongTool_ShouldRejectCleaning_AndAnnounceIt()
        {
            var f = new Fixture();
            f.StartSession(Fixture.Seed("t1", Fixture.Dust));
            using var wrong = f.Bus.Record<WrongEquipmentUsedEvent>();

            var result = f.Apply.Handle(new ApplyCleaningActionCommand("t1", f.Mop));

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(CleaningErrors.WrongEquipment, result.ErrorCode);
            Assert.AreEqual(10f, f.Targets.Get("t1").Value.CurrentHealth, 0.001f);
            Assert.AreEqual(1, wrong.Count);
        }

        [Test]
        public void CompletedTarget_ShouldNotAcceptMoreDamage()
        {
            var f = new Fixture();
            f.StartSession(Fixture.Seed("t1", Fixture.Dust, health: 5f), Fixture.Seed("t2", Fixture.Dust));
            f.Apply.Handle(new ApplyCleaningActionCommand("t1", f.Broom));

            var again = f.Apply.Handle(new ApplyCleaningActionCommand("t1", f.Broom));

            Assert.IsTrue(again.IsFailure);
            Assert.AreEqual(CleaningErrors.TargetAlreadyCompleted, again.ErrorCode);
        }

        [Test]
        public void UnknownTarget_IsReported()
        {
            var f = new Fixture();

            Assert.AreEqual(CleaningErrors.TargetNotFound, f.Apply.Handle(new ApplyCleaningActionCommand("nope", f.Broom)).ErrorCode);
        }

        [Test]
        public void UnownedEquipment_IsRejected()
        {
            var f = new Fixture();
            f.StartSession(Fixture.Seed("t1", Fixture.Dust));

            var result = f.Apply.Handle(new ApplyCleaningActionCommand("t1", new EquipmentId("equipment.ghost")));

            Assert.AreEqual(EquipmentErrors.NotOwned, result.ErrorCode);
        }

        [Test]
        public void Completing_PublishesHitThenCompleted()
        {
            var f = new Fixture();
            f.StartSession(Fixture.Seed("t1", Fixture.Dust, health: 5f), Fixture.Seed("t2", Fixture.Dust));
            using var hits = f.Bus.Record<CleaningHitAppliedEvent>();
            using var completed = f.Bus.Record<CleaningTargetCompletedEvent>();

            f.Apply.Handle(new ApplyCleaningActionCommand("t1", f.Broom));

            Assert.AreEqual(1, hits.Count);
            Assert.AreEqual(1, completed.Count);
            Assert.AreEqual("t1", completed.Last.TargetInstanceId);
        }
    }

    public sealed class CleaningProgressTests
    {
        [Test]
        public void CleaningProgress_ShouldUseWeights()
        {
            var light = new CleaningTarget("a", new DirtTypeId("dirt.dust"), 10f, weight: 1f);
            var heavy = new CleaningTarget("b", new DirtTypeId("dirt.mold"), 10f, weight: 4f);
            var calculator = new CleaningProgressCalculator();

            light.Apply(10f); // completes the weight-1 target

            Assert.AreEqual(1f / 5f, calculator.Calculate(new[] { light, heavy }), 0.0001f);

            heavy.Apply(5f); // half of the weight-4 target

            Assert.AreEqual(3f / 5f, calculator.Calculate(new[] { light, heavy }), 0.0001f);
        }

        [Test]
        public void EmptyProperty_IsFullyClean()
        {
            Assert.AreEqual(1f, new CleaningProgressCalculator().Calculate(new List<CleaningTarget>()), 0.0001f);
        }

        [Test]
        public void ProgressValue_ExposesBothWordings()
        {
            var progress = new CleaningProgress(0.75f);

            Assert.AreEqual(75, progress.CompletedPercent);
            Assert.AreEqual(25, progress.RemainingPercent);
            Assert.IsFalse(progress.IsComplete);
        }
    }

    public sealed class CleaningSessionTests
    {
        [Test]
        public void PropertyShouldComplete_WhenAllRequiredTargetsCompleted()
        {
            var f = new Fixture();
            f.StartSession(Fixture.Seed("t1", Fixture.Dust, health: 5f), Fixture.Seed("t2", Fixture.Stain, health: 5f));
            using var property = f.Bus.Record<PropertyCompletedEvent>();

            f.Apply.Handle(new ApplyCleaningActionCommand("t1", f.Broom));
            Assert.AreEqual(0, property.Count);

            f.Apply.Handle(new ApplyCleaningActionCommand("t2", f.Mop));

            Assert.AreEqual(1, property.Count);
            Assert.AreEqual(1f, property.Last.Result.Progress, 0.0001f);
            Assert.AreEqual(2, property.Last.Result.CompletedTargets);
        }

        [Test]
        public void PropertyCompletion_IsAnnouncedOnlyOnce()
        {
            var f = new Fixture();
            f.StartSession(Fixture.Seed("t1", Fixture.Dust, health: 5f));
            using var property = f.Bus.Record<PropertyCompletedEvent>();

            f.Apply.Handle(new ApplyCleaningActionCommand("t1", f.Broom));
            f.Apply.Handle(new ApplyCleaningActionCommand("t1", f.Broom));

            Assert.AreEqual(1, property.Count);
        }

        [Test]
        public void WrongToolAttempts_AreCountedInTheResult()
        {
            var f = new Fixture();
            f.StartSession(Fixture.Seed("t1", Fixture.Dust, health: 5f));
            using var property = f.Bus.Record<PropertyCompletedEvent>();

            f.Apply.Handle(new ApplyCleaningActionCommand("t1", f.Mop));
            f.Apply.Handle(new ApplyCleaningActionCommand("t1", f.Mop));
            f.Apply.Handle(new ApplyCleaningActionCommand("t1", f.Broom));

            Assert.AreEqual(2, property.Last.Result.WrongEquipmentAttempts);
            Assert.AreEqual(1, property.Last.Result.HitsApplied);
        }

        [Test]
        public void RoomEvents_FireWhenARoomIsFinished()
        {
            var f = new Fixture();
            f.StartSession(
                Fixture.Seed("a", Fixture.Dust, health: 5f, room: "kitchen"),
                Fixture.Seed("b", Fixture.Dust, health: 5f, room: "bedroom"));
            using var rooms = f.Bus.Record<RoomCompletedEvent>();

            f.Apply.Handle(new ApplyCleaningActionCommand("a", f.Broom));

            Assert.AreEqual(1, rooms.Count);
            Assert.AreEqual("kitchen", rooms.Last.RoomId);
        }

        [Test]
        public void ProgressChange_IsPublishedAfterEveryHit()
        {
            var f = new Fixture();
            f.StartSession(Fixture.Seed("t1", Fixture.Dust, health: 10f));
            using var progress = f.Bus.Record<PropertyCleaningProgressChangedEvent>();

            f.Apply.Handle(new ApplyCleaningActionCommand("t1", f.Broom));

            Assert.AreEqual(0.5f, progress.Last.Progress, 0.0001f);
        }

        [Test]
        public void StartingASecondSession_IsRejected()
        {
            var f = new Fixture();
            f.StartSession(Fixture.Seed("t1", Fixture.Dust));

            Assert.AreEqual(CleaningErrors.SessionAlreadyActive, f.StartSession(Fixture.Seed("t2", Fixture.Dust)).ErrorCode);
        }

        [Test]
        public void AFinishedSession_CanBeReplacedByTheNextProperty()
        {
            var f = new Fixture();
            f.StartSession(Fixture.Seed("t1", Fixture.Dust, health: 5f));
            f.Apply.Handle(new ApplyCleaningActionCommand("t1", f.Broom));

            var next = f.StartSession(Fixture.Seed("t2", Fixture.Dust));

            Assert.IsTrue(next.IsSuccess);
            Assert.IsTrue(f.Targets.Get("t1").IsFailure);
            Assert.IsTrue(f.Targets.Get("t2").IsSuccess);
        }

        [Test]
        public void DuplicateOrEmptyTargets_AreRejected()
        {
            var f = new Fixture();

            Assert.AreEqual(CleaningErrors.InvalidTargets, f.StartSession(Fixture.Seed("t1", Fixture.Dust), Fixture.Seed("t1", Fixture.Stain)).ErrorCode);
            Assert.AreEqual(CleaningErrors.InvalidTargets, f.StartSession().ErrorCode);
        }

        [Test]
        public void Cancel_ClearsTheSession()
        {
            var f = new Fixture();
            f.StartSession(Fixture.Seed("t1", Fixture.Dust));
            var cancel = new CancelCleaningHandler(f.Targets, f.Sessions, f.Bus);

            Assert.IsTrue(cancel.Handle(new CancelCleaningCommand()).IsSuccess);
            Assert.IsTrue(f.Sessions.GetCurrent().IsFailure);
            Assert.AreEqual(0, f.Targets.GetAll().Count);
        }

        [Test]
        public void JobFailure_CancelsItsSession()
        {
            var f = new Fixture();
            f.StartSession(Fixture.Seed("t1", Fixture.Dust));

            f.Bus.Publish(new JobFailedEvent(new JobId("job-1"), new JobDefinitionId("job.def"), new LocationId("location.test"), "abandoned"));

            Assert.IsTrue(f.Sessions.GetCurrent().IsFailure);
        }

        [Test]
        public void StartTarget_ValidatesCompatibility()
        {
            var f = new Fixture();
            f.StartSession(Fixture.Seed("t1", Fixture.Dust));
            var start = new StartCleaningTargetHandler(f.Targets, f.Equipment, new EquipmentCompatibilityPolicy(), f.Bus);

            Assert.IsTrue(start.Handle(new StartCleaningTargetCommand("t1", f.Broom)).IsSuccess);
            Assert.AreEqual(CleaningErrors.WrongEquipment, start.Handle(new StartCleaningTargetCommand("t1", f.Mop)).ErrorCode);
        }
    }
}
