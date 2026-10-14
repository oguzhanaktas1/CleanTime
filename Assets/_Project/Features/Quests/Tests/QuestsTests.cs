using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CleanTime.Cleaning.Domain;
using CleanTime.Dialogue.Domain;
using CleanTime.Economy.Application;
using CleanTime.Economy.Domain;
using CleanTime.Gifts.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Quests.Application;
using CleanTime.Quests.Domain;
using CleanTime.Quests.Infrastructure;
using CleanTime.Relationships.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;

namespace CleanTime.Quests.Tests
{
    internal sealed class FakeWallet : IWalletService
    {
        public Money Balance { get; set; }
        public bool CanAfford(Money price) => Balance >= price;
        public Result Credit(Money amount, TransactionReason reason, string referenceId = "") { Balance += amount; return Result.Success(); }
        public Result Debit(Money amount, TransactionReason reason, string referenceId = "") => Result.Success();
        public Result Pay(Money price, TransactionReason reason, string referenceId = "") => Result.Success();
    }

    internal sealed class QuestCatalog : IQuestDefinitionProvider
    {
        public static readonly QuestId Dust = new QuestId("quest.clean_dust");
        public static readonly QuestId Mother = new QuestId("quest.mother_level_2");
        public static readonly QuestId Starter = new QuestId("quest.starter");

        private readonly List<QuestDefinitionSnapshot> _items = new List<QuestDefinitionSnapshot>
        {
            new QuestDefinitionSnapshot(Dust, "k",
                new[] { new QuestObjective(QuestObjectiveType.CleanTargetCount, "dirt.dust", 3) },
                new QuestReward(new Money(1500))),
            new QuestDefinitionSnapshot(Mother, "k",
                new[] { new QuestObjective(QuestObjectiveType.ReachRelationshipLevel, "npc.mother", 2) },
                QuestReward.None),
            new QuestDefinitionSnapshot(Starter, "k",
                new[] { new QuestObjective(QuestObjectiveType.CompleteJob, string.Empty, 1) },
                new QuestReward(new Money(100)), activeByDefault: true),
        };

        public Result<QuestDefinitionSnapshot> Get(QuestId id)
        {
            var found = _items.FirstOrDefault(i => i.Id == id);
            return found != null ? Result<QuestDefinitionSnapshot>.Success(found) : Result<QuestDefinitionSnapshot>.Failure("x", "x");
        }

        public IReadOnlyCollection<QuestDefinitionSnapshot> GetAll() => _items;
    }

    internal sealed class QuestFixture
    {
        public EventBus Bus { get; } = new EventBus();
        public FakeWallet Wallet { get; } = new FakeWallet();
        public InMemoryQuestRepository Repository { get; }
        public ActivateQuestHandler Activate { get; }
        public ClaimQuestRewardHandler Claim { get; }
        public QuestProgressProjection Projection { get; }

        public QuestFixture()
        {
            Repository = new InMemoryQuestRepository(new QuestCatalog(), new InMemorySnapshotStore());
            Activate = new ActivateQuestHandler(new QuestCatalog(), Repository, Bus);
            Claim = new ClaimQuestRewardHandler(Repository, Wallet, Bus);
            Projection = new QuestProgressProjection(Bus, Repository, new DefaultQuestObjectiveEvaluator());
        }

        public void CleanDust(int times = 1)
        {
            for (var i = 0; i < times; i++)
                Bus.Publish(new CleaningTargetCompletedEvent("t" + i, new DirtTypeId("dirt.dust"), 1f));
        }
    }

    public sealed class QuestObjectiveTests
    {
        [Test]
        public void Evaluator_OnlyCountsMatchingSignals()
        {
            var evaluator = new DefaultQuestObjectiveEvaluator();
            var objective = new QuestObjective(QuestObjectiveType.CleanTargetCount, "dirt.dust", 3);

            Assert.AreEqual(1, evaluator.Evaluate(objective, 0, QuestSignal.CleanTarget("dirt.dust")));
            Assert.AreEqual(0, evaluator.Evaluate(objective, 0, QuestSignal.CleanTarget("dirt.mold")));
            Assert.AreEqual(0, evaluator.Evaluate(objective, 0, QuestSignal.CompleteJob("dirt.dust")));
        }

        [Test]
        public void EmptyTargetKey_MeansAnything()
        {
            var evaluator = new DefaultQuestObjectiveEvaluator();
            var objective = new QuestObjective(QuestObjectiveType.CleanTargetCount, string.Empty, 3);

            Assert.AreEqual(1, evaluator.Evaluate(objective, 0, QuestSignal.CleanTarget("dirt.anything")));
        }

        [Test]
        public void Progress_NeverExceedsTheRequiredCount()
        {
            var evaluator = new DefaultQuestObjectiveEvaluator();
            var objective = new QuestObjective(QuestObjectiveType.EarnMoney, string.Empty, 100);

            Assert.AreEqual(100, evaluator.Evaluate(objective, 90, QuestSignal.EarnMoney(5000)));
        }

        [Test]
        public void RelationshipLevel_ComparesInsteadOfAccumulating()
        {
            var evaluator = new DefaultQuestObjectiveEvaluator();
            var objective = new QuestObjective(QuestObjectiveType.ReachRelationshipLevel, "npc.mother", 3);

            Assert.AreEqual(2, evaluator.Evaluate(objective, 0, QuestSignal.RelationshipLevel("npc.mother", 2)));
            Assert.AreEqual(2, evaluator.Evaluate(objective, 2, QuestSignal.RelationshipLevel("npc.mother", 1)));
        }
    }

    public sealed class QuestFlowTests
    {
        [Test]
        public void Activating_CreatesAnActiveQuest()
        {
            var f = new QuestFixture();
            using var activated = f.Bus.Record<QuestActivatedEvent>();

            Assert.IsTrue(f.Activate.Handle(new ActivateQuestCommand(QuestCatalog.Dust)).IsSuccess);

            Assert.AreEqual(QuestState.Active, f.Repository.Get(QuestCatalog.Dust).Value.State);
            Assert.AreEqual(1, activated.Count);
        }

        [Test]
        public void Activating_Twice_OrUnknown_Fails()
        {
            var f = new QuestFixture();
            f.Activate.Handle(new ActivateQuestCommand(QuestCatalog.Dust));

            Assert.AreEqual(QuestErrors.AlreadyActive, f.Activate.Handle(new ActivateQuestCommand(QuestCatalog.Dust)).ErrorCode);
            Assert.AreEqual(QuestErrors.UnknownQuest, f.Activate.Handle(new ActivateQuestCommand(new QuestId("nope"))).ErrorCode);
        }

        [Test]
        public void DefaultQuests_AreActiveFromTheStart()
        {
            var f = new QuestFixture();

            Assert.AreEqual(QuestCatalog.Starter, f.Repository.GetActive().Single().Id);
        }

        [Test]
        public void CleaningEvents_AdvanceTheQuest_UntilItCompletes()
        {
            var f = new QuestFixture();
            f.Activate.Handle(new ActivateQuestCommand(QuestCatalog.Dust));
            using var progress = f.Bus.Record<QuestProgressChangedEvent>();
            using var completed = f.Bus.Record<QuestCompletedEvent>();

            f.CleanDust(2);
            Assert.AreEqual(0, completed.Count);
            Assert.AreEqual(2, f.Repository.Get(QuestCatalog.Dust).Value.Progress[0]);

            f.CleanDust(1);
            Assert.AreEqual(1, completed.Count);
            Assert.AreEqual(QuestState.Completed, f.Repository.Get(QuestCatalog.Dust).Value.State);
            Assert.GreaterOrEqual(progress.Count, 3);
        }

        [Test]
        public void CompletedQuests_IgnoreFurtherSignals()
        {
            var f = new QuestFixture();
            f.Activate.Handle(new ActivateQuestCommand(QuestCatalog.Dust));
            using var completed = f.Bus.Record<QuestCompletedEvent>();

            f.CleanDust(10);

            Assert.AreEqual(1, completed.Count);
            Assert.AreEqual(3, f.Repository.Get(QuestCatalog.Dust).Value.Progress[0]);
        }

        [Test]
        public void ClaimingTheReward_PaysOnce()
        {
            var f = new QuestFixture();
            f.Activate.Handle(new ActivateQuestCommand(QuestCatalog.Dust));
            f.CleanDust(3);
            using var granted = f.Bus.Record<QuestRewardGrantedEvent>();

            Assert.IsTrue(f.Claim.Handle(new ClaimQuestRewardCommand(QuestCatalog.Dust)).IsSuccess);
            Assert.AreEqual(1500, f.Wallet.Balance.Cents);
            Assert.AreEqual(1, granted.Count);

            Assert.AreEqual(QuestErrors.AlreadyClaimed, f.Claim.Handle(new ClaimQuestRewardCommand(QuestCatalog.Dust)).ErrorCode);
            Assert.AreEqual(1500, f.Wallet.Balance.Cents);
        }

        [Test]
        public void ClaimingAnUnfinishedQuest_Fails()
        {
            var f = new QuestFixture();
            f.Activate.Handle(new ActivateQuestCommand(QuestCatalog.Dust));
            f.CleanDust(1);

            Assert.AreEqual(QuestErrors.NotCompleted, f.Claim.Handle(new ClaimQuestRewardCommand(QuestCatalog.Dust)).ErrorCode);
            Assert.AreEqual(0, f.Wallet.Balance.Cents);
        }

        [Test]
        public void JobsAndRelationshipsAndDialogue_AllFeedTheSameMachinery()
        {
            var f = new QuestFixture();
            f.Activate.Handle(new ActivateQuestCommand(QuestCatalog.Mother));
            using var completed = f.Bus.Record<QuestCompletedEvent>();

            f.Bus.Publish(new JobCompletedEvent(new JobId("j"), new JobDefinitionId("d"), new LocationId("l"), new Money(1), 1f,
                new CustomerEvaluation(1, 1, 1, 1, 1f), 1f, 1));
            f.Bus.Publish(new RelationshipLevelChangedEvent(new NpcId("npc.mother"), 0, 2));

            Assert.IsTrue(f.Repository.Get(QuestCatalog.Starter).Value.State == QuestState.Completed);
            Assert.IsTrue(f.Repository.Get(QuestCatalog.Mother).Value.State == QuestState.Completed);
            Assert.AreEqual(2, completed.Count);
        }

        [Test]
        public void TalkingToAnNpc_CountsAsASignal()
        {
            var evaluator = new DefaultQuestObjectiveEvaluator();
            var objective = new QuestObjective(QuestObjectiveType.TalkToNpc, "npc.mother", 1);

            Assert.AreEqual(1, evaluator.Evaluate(objective, 0, QuestSignal.TalkToNpc("npc.mother")));

            var f = new QuestFixture();
            f.Bus.Publish(new DialogueEndedEvent(new DialogueId("d"), default)); // ambient dialogue: no NPC, no signal
            Assert.AreEqual(QuestState.Active, f.Repository.Get(QuestCatalog.Starter).Value.State);
        }

        [Test]
        public void SaveLoadRoundTrip_PreservesProgressAndState()
        {
            var store = new InMemorySnapshotStore();
            var repository = new InMemoryQuestRepository(new QuestCatalog(), store);
            var quest = new QuestInstance(new QuestCatalog().Get(QuestCatalog.Dust).Value);
            quest.TryApply(QuestSignal.CleanTarget("dirt.dust", 2), new DefaultQuestObjectiveEvaluator());
            repository.Save(quest);
            var slot = SaveId.New();
            repository.Flush(slot);

            repository.Reset();
            Assert.IsTrue(repository.Get(QuestCatalog.Dust).IsFailure);

            Assert.IsTrue(repository.Load(slot).IsSuccess);
            Assert.AreEqual(2, repository.Get(QuestCatalog.Dust).Value.Progress[0]);
            Assert.AreEqual(QuestState.Active, repository.Get(QuestCatalog.Dust).Value.State);
        }
    }
}
