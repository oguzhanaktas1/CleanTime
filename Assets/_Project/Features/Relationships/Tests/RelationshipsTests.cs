using System.Collections.Generic;
using NUnit.Framework;
using CleanTime.Gifts.Domain;
using CleanTime.Relationships.Application;
using CleanTime.Relationships.Domain;
using CleanTime.Relationships.Infrastructure;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;

namespace CleanTime.Relationships.Tests
{
    internal sealed class RelationshipFixture
    {
        public static readonly NpcId Mother = new NpcId("npc.mother");
        public static readonly NpcId Vendor = new NpcId("npc.equipment_vendor");

        public EventBus Bus { get; } = new EventBus();
        public InMemorySnapshotStore Snapshots { get; } = new InMemorySnapshotStore();
        public InMemoryRelationshipRepository Repository { get; }
        public ApplyRelationshipChangeHandler Apply { get; }

        public RelationshipFixture(RelationshipThresholdSet thresholds = null)
        {
            Repository = new InMemoryRelationshipRepository(Snapshots);
            Apply = new ApplyRelationshipChangeHandler(
                Repository,
                new ThresholdRelationshipLevelPolicy(thresholds ?? RelationshipThresholdSet.Default),
                new FakeGameClock { Now = new GameDate(3, DayPhase.Morning) },
                Bus);
        }

        public static RelationshipThresholdSet WithMotherStory() => new RelationshipThresholdSet(
            new[] { new RelationshipThreshold(1, 20), new RelationshipThreshold(2, 60) },
            new Dictionary<NpcId, IReadOnlyList<RelationshipThreshold>>
            {
                {
                    Mother, new[]
                    {
                        new RelationshipThreshold(1, 10),
                        new RelationshipThreshold(2, 30),
                        new RelationshipThreshold(3, 60, "story.mother.final"),
                    }
                },
            });
    }

    public sealed class RelationshipTests
    {
        [Test]
        public void Gift_ShouldIncreaseRelationship()
        {
            var f = new RelationshipFixture();
            using var projection = new GiftRelationshipProjection(f.Bus, f.Apply);
            using var points = f.Bus.Record<RelationshipPointsChangedEvent>();

            f.Bus.Publish(new GiftGivenEvent(RelationshipFixture.Mother, new GiftId("gift.cup"), 15, 1));

            Assert.AreEqual(15, f.Repository.Get(RelationshipFixture.Mother).Value.Points);
            Assert.AreEqual(15, points.Last.Delta);
        }

        [Test]
        public void RelationshipLevel_ShouldIncrease_AtThreshold()
        {
            var f = new RelationshipFixture();
            using var levels = f.Bus.Record<RelationshipLevelChangedEvent>();

            f.Apply.Handle(new ApplyRelationshipChangeCommand(RelationshipFixture.Vendor, 19));
            Assert.AreEqual(0, levels.Count);

            f.Apply.Handle(new ApplyRelationshipChangeCommand(RelationshipFixture.Vendor, 1));

            Assert.AreEqual(1, levels.Count);
            Assert.AreEqual(1, f.Repository.Get(RelationshipFixture.Vendor).Value.Level);
        }

        [Test]
        public void FinalMotherMilestone_ShouldTriggerStoryEvent()
        {
            var f = new RelationshipFixture(RelationshipFixture.WithMotherStory());
            using var milestones = f.Bus.Record<RelationshipMilestoneReachedEvent>();

            f.Apply.Handle(new ApplyRelationshipChangeCommand(RelationshipFixture.Mother, 59));
            Assert.AreEqual(0, milestones.Count);

            f.Apply.Handle(new ApplyRelationshipChangeCommand(RelationshipFixture.Mother, 1));

            Assert.AreEqual(1, milestones.Count);
            Assert.AreEqual("story.mother.final", milestones.Last.MilestoneId);
            Assert.AreEqual(3, milestones.Last.Level);
        }

        [Test]
        public void OtherNpcs_DoNotUseTheMothersTable()
        {
            var f = new RelationshipFixture(RelationshipFixture.WithMotherStory());
            using var milestones = f.Bus.Record<RelationshipMilestoneReachedEvent>();

            f.Apply.Handle(new ApplyRelationshipChangeCommand(RelationshipFixture.Vendor, 500));

            Assert.AreEqual(0, milestones.Count);
            Assert.AreEqual(2, f.Repository.Get(RelationshipFixture.Vendor).Value.Level); // default table tops out at level 2
        }

        [Test]
        public void SkippingLevels_StillFiresEveryCrossedMilestone()
        {
            var thresholds = new RelationshipThresholdSet(new[]
            {
                new RelationshipThreshold(1, 10, "m1"),
                new RelationshipThreshold(2, 20, "m2"),
            });
            var f = new RelationshipFixture(thresholds);
            using var milestones = f.Bus.Record<RelationshipMilestoneReachedEvent>();

            f.Apply.Handle(new ApplyRelationshipChangeCommand(RelationshipFixture.Vendor, 100));

            Assert.AreEqual(2, milestones.Count);
        }

        [Test]
        public void Points_NeverGoBelowZero()
        {
            var f = new RelationshipFixture();

            f.Apply.Handle(new ApplyRelationshipChangeCommand(RelationshipFixture.Vendor, -50));

            Assert.AreEqual(0, f.Repository.Get(RelationshipFixture.Vendor).Value.Points);
        }

        [Test]
        public void EmptyNpc_IsRejected()
        {
            var f = new RelationshipFixture();

            Assert.IsTrue(f.Apply.Handle(new ApplyRelationshipChangeCommand(default, 5)).IsFailure);
        }

        [Test]
        public void SaveLoadRoundTrip_PreservesRelationships()
        {
            var f = new RelationshipFixture();
            f.Apply.Handle(new ApplyRelationshipChangeCommand(RelationshipFixture.Mother, 65));
            var slot = SaveId.New();
            f.Repository.Flush(slot);

            f.Repository.Reset();
            Assert.AreEqual(0, f.Repository.Get(RelationshipFixture.Mother).Value.Points);

            Assert.IsTrue(f.Repository.Load(slot).IsSuccess);
            Assert.AreEqual(65, f.Repository.Get(RelationshipFixture.Mother).Value.Points);
            Assert.AreEqual(2, f.Repository.Get(RelationshipFixture.Mother).Value.Level);
            Assert.AreEqual(3, f.Repository.Get(RelationshipFixture.Mother).Value.LastChangedDay);
        }
    }
}
