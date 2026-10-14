using System.Linq;
using NUnit.Framework;
using CleanTime.Bootstrap;
using CleanTime.Cleaning.Application;
using CleanTime.Cleaning.Domain;
using CleanTime.Company.Application;
using CleanTime.Company.Domain;
using CleanTime.Economy.Application;
using CleanTime.Economy.Domain;
using CleanTime.Employees.Application;
using CleanTime.Employees.Domain;
using CleanTime.Equipment.Application;
using CleanTime.Equipment.Domain;
using CleanTime.GameTime.Application;
using CleanTime.GameTime.Domain;
using CleanTime.Gifts.Application;
using CleanTime.Gifts.Domain;
using CleanTime.Jobs.Application;
using CleanTime.Jobs.Domain;
using CleanTime.Locations.Domain;
using CleanTime.Progression.Application;
using CleanTime.Progression.Domain;
using CleanTime.Quests.Application;
using CleanTime.Quests.Domain;
using CleanTime.Relationships.Application;
using CleanTime.Relationships.Domain;
using CleanTime.Save.Application;
using CleanTime.Save.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;
using CleanTime.World.Application;

namespace CleanTime.Bootstrap.Tests
{
    internal static class Scenario
    {
        public static readonly EquipmentId Broom = new EquipmentId(Ids.Broom);
        public static readonly EquipmentId Mop = new EquipmentId(Ids.Mop);
        public static readonly LocationId Small = new LocationId(Ids.SmallLocation);
        public static readonly LocationId Large = new LocationId(Ids.LargeLocation);
        public static readonly NpcId Mother = new NpcId(Ids.Mother);

        /// <summary>Accepts the job, starts it, enters the property and cleans everything with the right tools.</summary>
        public static void CleanProperty(GameHarness game, string jobId = Ids.SmallJob, string location = Ids.SmallLocation)
        {
            Assert.IsTrue(game.Send(new AcceptJobCommand(new JobDefinitionId(jobId))).IsSuccess, "accept");
            var job = game.Ask<GetActiveJobQuery, Result<Job>>(new GetActiveJobQuery()).Value;
            Assert.IsTrue(game.Send(new StartJobCommand(job.Id)).IsSuccess, "start");

            Assert.IsTrue(game.Send(new StartCleaningSessionCommand(
                new LocationId(location),
                job.Id,
                new[]
                {
                    new CleaningTargetSeed("dust-1", new DirtTypeId(Ids.Dust), 10f, 1f, "kitchen"),
                    new CleaningTargetSeed("stain-1", new DirtTypeId(Ids.Stain), 10f, 2f, "kitchen"),
                })).IsSuccess, "session");

            var cleaning = game.Resolve<ICleaningCommandService>();
            cleaning.ApplyAction("dust-1", Broom);
            cleaning.ApplyAction("dust-1", Broom);
            cleaning.ApplyAction("stain-1", Mop);
            cleaning.ApplyAction("stain-1", Mop);
        }

        public static void RunFullJob(GameHarness game, string jobId = Ids.SmallJob, string location = Ids.SmallLocation)
        {
            CleanProperty(game, jobId, location);
            Assert.IsTrue(game.Send(new FinishJobResultCommand()).IsSuccess, "finish result");
        }

        public static Money Wallet(GameHarness game) => game.Resolve<IWalletService>().Balance;
    }

    public sealed class CompositionTests
    {
        [Test]
        public void EveryModule_RegistersAndInitialisesWithoutErrors()
        {
            using var game = GameHarness.Create();

            Assert.IsEmpty(game.Log.Errors);
        }

        [Test]
        public void PublicContracts_AreResolvable()
        {
            using var game = GameHarness.Create();

            Assert.IsNotNull(game.Resolve<IWalletService>());
            Assert.IsNotNull(game.Resolve<ISaveCoordinator>());
            Assert.IsNotNull(game.Resolve<ISelectedEquipmentQuery>());
            Assert.IsNotNull(game.Resolve<ICleaningReadService>());
            Assert.IsNotNull(game.Resolve<ICleaningCommandService>());
            Assert.IsNotNull(game.Resolve<IGameClock>());
            Assert.IsNotNull(game.Resolve<GameFlowStateMachine>());
            Assert.IsNotNull(game.Resolve<CleanTime.DebugTools.Application.DebugCommandRegistry>());
            Assert.IsNotNull(game.Resolve<CleanTime.Analytics.Application.IAnalyticsService>());
        }

        [Test]
        public void SaveParticipants_CoverTheStatefulModules()
        {
            using var game = GameHarness.Create();

            var keys = game.Services.ResolveAll<ISaveParticipant>().Select(p => p.Key).ToList();

            foreach (var expected in new[]
            {
                "economy.wallet", "locations.progress", "progression.player", "equipment.progress",
                "relationships.all", "quests.progress", "time.calendar",
            })
            {
                CollectionAssert.Contains(keys, expected);
            }
        }

        [Test]
        public void ModuleNames_AreUnique_AndSaveInitialisesLastAmongApplicationModules()
        {
            var modules = FeatureRegistration.CreateGameplayModules(new BootstrapSettings(), null).ToList();

            CollectionAssert.AllItemsAreUnique(modules.Select(m => m.Name));

            var applicationModules = modules.TakeWhile(m => !m.Name.EndsWith(".Infrastructure")).ToList();
            Assert.AreEqual("Save", applicationModules.Last().Name);
        }

        [Test]
        public void TheCatalog_IsValidated_BeforeBootContinues()
        {
            var duplicate = TestAssets.Catalog(dirt: new[] { TestAssets.Dirt("dirt.dust", 1, 1), TestAssets.Dirt("dirt.dust", 2, 2) });

            var built = ContentRegistry_Build(duplicate);

            Assert.AreEqual("content.invalid", built.ErrorCode);
            StringAssert.Contains("Duplicate dirt id", built.Error.Value.Message);
        }

        private static Result<CleanTime.Content.ContentRegistry> ContentRegistry_Build(CleanTime.Content.GameContentCatalog catalog) =>
            CleanTime.Content.ContentRegistry.Build(catalog);
    }

    public sealed class CompleteJobScenarioTests
    {
        [Test]
        public void CompletingAJob_RewardsEveryInterestedModule()
        {
            using var game = GameHarness.Create();
            using var completed = game.Bus.Record<JobCompletedEvent>();
            var before = Scenario.Wallet(game);

            Scenario.CleanProperty(game);

            Assert.AreEqual(1, completed.Count, "job completed");
            Assert.AreEqual(1, game.Resolve<IJobRepository>().GetHistory().Count);
            Assert.IsTrue(game.Resolve<IJobRepository>().GetPendingResult().IsSuccess, "result screen data");

            // reward credited with a ledger entry
            Assert.Greater(Scenario.Wallet(game).Cents, before.Cents);
            var ledger = game.Resolve<ITransactionRepository>().GetAll();
            Assert.AreEqual(TransactionReason.JobReward, ledger.Last().Reason);
            Assert.AreEqual(completed.Last.Reward.Cents, ledger.Last().Amount.Cents);

            // customer evaluation is generated and drives the reward
            Assert.AreEqual(1f, completed.Last.Evaluation.Cleaning, 0.001f);
            Assert.AreEqual(31250, completed.Last.Reward.Cents);

            // completion counted, quest advanced, time consumed
            Assert.AreEqual(1, game.Resolve<ILocationProgressRepository>().Get(Scenario.Small).Value.CompletionCount);
            Assert.AreEqual(QuestState.Completed, game.Resolve<IQuestRepository>().Get(new QuestId(Ids.StarterQuest)).Value.State);
            Assert.AreEqual(DayPhase.Afternoon, game.Resolve<IGameClock>().Now.Phase);
            Assert.AreEqual(1, game.Resolve<IProgressionRepository>().Get().Value.TotalJobsCompleted);
        }

        [Test]
        public void TheSceneFlow_FollowsTheJob()
        {
            using var game = GameHarness.Create();
            var flow = game.Resolve<GameFlowStateMachine>();

            Scenario.CleanProperty(game);
            Assert.AreEqual(GameFlowState.JobResult, flow.Current);
            CollectionAssert.Contains(game.Scenes.Calls, "property:" + Ids.SmallLocation);

            game.Send(new FinishJobResultCommand());
            Assert.AreEqual(GameFlowState.Town, flow.Current);
            Assert.AreEqual("town", game.Scenes.Calls.Last());
        }

        [Test]
        public void Abandoning_ReturnsToTown_AndFreesTheCleaningSession()
        {
            using var game = GameHarness.Create();
            game.Send(new AcceptJobCommand(new JobDefinitionId(Ids.SmallJob)));
            var job = game.Ask<GetActiveJobQuery, Result<Job>>(new GetActiveJobQuery()).Value;
            game.Send(new StartJobCommand(job.Id));
            game.Send(new StartCleaningSessionCommand(Scenario.Small, job.Id,
                new[] { new CleaningTargetSeed("d", new DirtTypeId(Ids.Dust), 10f, 1f) }));

            Assert.IsTrue(game.Send(new AbandonJobCommand(job.Id)).IsSuccess);

            Assert.AreEqual(GameFlowState.Town, game.Resolve<GameFlowStateMachine>().Current);
            Assert.AreEqual(0f, game.Resolve<ICleaningReadService>().GetCurrentPropertyProgress());
            Assert.IsTrue(game.Send(new AcceptJobCommand(new JobDefinitionId(Ids.SmallJob))).IsSuccess);
        }

        [Test]
        public void WrongTools_CostCustomerSatisfaction()
        {
            using var game = GameHarness.Create();
            game.Send(new AcceptJobCommand(new JobDefinitionId(Ids.SmallJob)));
            var job = game.Ask<GetActiveJobQuery, Result<Job>>(new GetActiveJobQuery()).Value;
            game.Send(new StartJobCommand(job.Id));
            game.Send(new StartCleaningSessionCommand(Scenario.Small, job.Id,
                new[] { new CleaningTargetSeed("d", new DirtTypeId(Ids.Dust), 10f, 1f) }));
            using var completed = game.Bus.Record<JobCompletedEvent>();
            var cleaning = game.Resolve<ICleaningCommandService>();

            for (var i = 0; i < 5; i++)
                Assert.IsTrue(cleaning.ApplyAction("d", Scenario.Mop).IsFailure);

            cleaning.ApplyAction("d", Scenario.Broom);
            cleaning.ApplyAction("d", Scenario.Broom);

            Assert.AreEqual(0.5f, completed.Last.Evaluation.Mistakes, 0.001f);
            Assert.Less(completed.Last.CustomerScore, 1f);
        }

        [Test]
        public void SixCompletions_UnlockTheNextLocation_AndItsJobs()
        {
            using var game = GameHarness.Create();
            var locations = game.Resolve<ILocationProgressRepository>();
            using var unlocked = game.Bus.Record<LocationUnlockedEvent>();

            Assert.IsTrue(game.Send(new AcceptJobCommand(new JobDefinitionId(Ids.LargeJob))).IsFailure, "locked before");

            for (var i = 0; i < 5; i++)
                Scenario.RunFullJob(game);

            Assert.IsFalse(locations.Get(Scenario.Large).Value.Unlocked);

            Scenario.RunFullJob(game);

            Assert.IsTrue(locations.Get(Scenario.Large).Value.Unlocked);
            Assert.AreEqual(1, unlocked.Count);
            Assert.IsTrue(game.Send(new AcceptJobCommand(new JobDefinitionId(Ids.LargeJob))).IsSuccess, "unlocked after");
        }
    }

    public sealed class CrossFeatureScenarioTests
    {
        [Test]
        public void BuyingEquipment_SpendsMoney_AndFeedsTheLedger()
        {
            using var game = GameHarness.Create(startingMoneyCents: 6000);

            Assert.IsTrue(game.Send(new PurchaseEquipmentCommand(new EquipmentId(Ids.Vacuum))).IsSuccess);

            Assert.AreEqual(1000, Scenario.Wallet(game).Cents);
            Assert.AreEqual(TransactionReason.EquipmentPurchase, game.Resolve<ITransactionRepository>().GetAll().Single().Reason);
            Assert.IsTrue(game.Resolve<IEquipmentRepository>().GetOwned(new EquipmentId(Ids.Vacuum)).IsSuccess);
        }

        [Test]
        public void Gifts_RaiseTheRelationship_AndCompleteQuestsAboutIt()
        {
            using var game = GameHarness.Create();
            using var levels = game.Bus.Record<RelationshipLevelChangedEvent>();

            Assert.IsTrue(game.Send(new PurchaseGiftCommand(new GiftId(Ids.Cup))).IsSuccess);
            Assert.IsTrue(game.Send(new GiveGiftCommand(Scenario.Mother, new GiftId(Ids.Cup))).IsSuccess);

            Assert.AreEqual(25, game.Resolve<IRelationshipRepository>().Get(Scenario.Mother).Value.Points);
            Assert.AreEqual(1, levels.Count);
            Assert.AreEqual(QuestState.Completed, game.Resolve<IQuestRepository>().Get(new QuestId(Ids.MotherQuest)).Value.State);
        }

        [Test]
        public void QuestRewards_ArePaidThroughTheWallet()
        {
            using var game = GameHarness.Create();
            Scenario.CleanProperty(game);
            var before = Scenario.Wallet(game);

            Assert.IsTrue(game.Send(new ClaimQuestRewardCommand(new QuestId(Ids.StarterQuest))).IsSuccess);

            Assert.AreEqual(before.Cents + 1000, Scenario.Wallet(game).Cents);
            Assert.AreEqual(TransactionReason.QuestReward, game.Resolve<ITransactionRepository>().GetAll().Last().Reason);
        }

        [Test]
        public void EmployeesWork_WhileTheDayPasses_AndTheirRevenueReachesTheWallet()
        {
            using var game = GameHarness.Create(startingMoneyCents: 100000);
            game.Send(new UnlockCompanyCommand());
            Assert.IsTrue(game.Send(new CreateCompanyCommand("Sparkle")).IsSuccess);
            Assert.IsTrue(game.Send(new HireEmployeeCommand(new EmployeeDefinitionId(Ids.Rookie))).IsSuccess);
            var employee = game.Ask<GetEmployeesQuery, System.Collections.Generic.IReadOnlyList<Employee>>(new GetEmployeesQuery()).Single();
            Assert.IsTrue(game.Send(new AssignEmployeeCommand(employee.Id, new JobDefinitionId(Ids.SmallJob))).IsSuccess);
            using var summaries = game.Bus.Record<DaySummaryCreatedEvent>();
            using var revenue = game.Bus.Record<EmployeeRevenueGeneratedEvent>();
            var before = Scenario.Wallet(game);

            game.Send(new EndGameDayCommand()); // day 1 ends; the 1-day job finishes on day 2
            Assert.AreEqual(0, revenue.Count);
            game.Send(new EndGameDayCommand());

            Assert.AreEqual(1, revenue.Count);
            Assert.AreEqual(before.Cents + revenue.Last.Revenue.Cents, Scenario.Wallet(game).Cents);
            Assert.AreEqual(revenue.Last.Revenue, summaries.Last.Summary.EmployeeRevenue);
            Assert.AreEqual(revenue.Last.Revenue, game.Ask<GetCompanyOverviewQuery, Result<CompanyProfile>>(new GetCompanyOverviewQuery()).Value.LifetimeRevenue);
            Assert.IsTrue(game.Resolve<IEmployeeRepository>().Get(employee.Id).Value.IsIdle);
        }

        [Test]
        public void TheCompany_UnlocksAfterEnoughJobs()
        {
            using var game = GameHarness.Create();
            using var unlocked = game.Bus.Record<CompanyUnlockedEvent>();

            for (var i = 0; i < 17; i++)
                Scenario.RunFullJob(game);
            Assert.AreEqual(0, unlocked.Count);

            Scenario.RunFullJob(game);
            Assert.AreEqual(1, unlocked.Count);
        }

        [Test]
        public void ADayEndsAfterFourJobs()
        {
            using var game = GameHarness.Create();
            using var started = game.Bus.Record<GameDayStartedEvent>();

            for (var i = 0; i < 4; i++)
                Scenario.RunFullJob(game);

            Assert.AreEqual(1, started.Count);
            Assert.AreEqual(new GameDate(2, DayPhase.Morning), game.Resolve<IGameClock>().Now);
        }
    }

    public sealed class SaveIntegrationTests
    {
        [Test]
        public void ANewGame_CanBeSavedAndReloaded_WithAllModulesRestored()
        {
            using var game = GameHarness.Create(startingMoneyCents: 6000);
            var save = game.Resolve<ISaveCoordinator>();
            var id = save.StartNewGame("Slot A").Value;

            game.Send(new PurchaseEquipmentCommand(new EquipmentId(Ids.Vacuum)));
            Scenario.RunFullJob(game);
            game.Send(new PurchaseGiftCommand(new GiftId(Ids.Cup)));
            game.Send(new GiveGiftCommand(Scenario.Mother, new GiftId(Ids.Cup)));
            Assert.IsTrue(save.SaveCurrentGame().IsSuccess);

            var wallet = Scenario.Wallet(game);
            var points = game.Resolve<IRelationshipRepository>().Get(Scenario.Mother).Value.Points;
            var date = game.Resolve<IGameClock>().Now;

            // Diverge the live state without publishing events, so no autosave checkpoint can overwrite the slot.
            game.Resolve<IWalletRepository>().GetCurrent().Value.Credit(new Money(999999));
            game.Resolve<ILocationProgressRepository>().Get(Scenario.Small).Value.RegisterCompletion(0.1f, 1f, 1);
            game.Resolve<IRelationshipRepository>().Get(Scenario.Mother).Value.AddPoints(500, 1);
            game.Resolve<IProgressionRepository>().Get().Value.RegisterJobCompleted();
            game.Resolve<IGameTimeRepository>().Get().StartNextDay();
            Assert.AreNotEqual(wallet, Scenario.Wallet(game));

            Assert.IsTrue(save.LoadGame(id).IsSuccess);

            Assert.AreEqual(wallet, Scenario.Wallet(game));
            Assert.AreEqual(points, game.Resolve<IRelationshipRepository>().Get(Scenario.Mother).Value.Points);
            Assert.AreEqual(date, game.Resolve<IGameClock>().Now);
            Assert.AreEqual(1, game.Resolve<ILocationProgressRepository>().Get(Scenario.Small).Value.CompletionCount);
            Assert.IsTrue(game.Resolve<IEquipmentRepository>().GetOwned(new EquipmentId(Ids.Vacuum)).IsSuccess);
            Assert.AreEqual(1, game.Resolve<IProgressionRepository>().Get().Value.TotalJobsCompleted);
            Assert.AreEqual(QuestState.Completed, game.Resolve<IQuestRepository>().Get(new QuestId(Ids.StarterQuest)).Value.State);
        }

        [Test]
        public void Checkpoints_AutosaveTheActiveSlot()
        {
            using var game = GameHarness.Create();
            var save = game.Resolve<ISaveCoordinator>();
            save.StartNewGame("Slot A");
            using var completed = game.Bus.Record<SaveCompletedEvent>();

            Scenario.CleanProperty(game);

            Assert.GreaterOrEqual(completed.Count, 1);
            Assert.IsEmpty(game.Log.Errors);
        }

        [Test]
        public void StartingANewGame_ResetsEverything()
        {
            using var game = GameHarness.Create();
            Scenario.RunFullJob(game);
            Assert.AreNotEqual(20000, Scenario.Wallet(game).Cents);

            game.Resolve<ISaveCoordinator>().StartNewGame("Fresh");

            Assert.AreEqual(20000, Scenario.Wallet(game).Cents);
            Assert.AreEqual(0, game.Resolve<ILocationProgressRepository>().Get(Scenario.Small).Value.CompletionCount);
            Assert.AreEqual(GameDate.Start, game.Resolve<IGameClock>().Now);
        }
    }

    public sealed class ContentValidationTests
    {
        [Test]
        public void ValidContent_HasNoIssues()
        {
            Assert.IsEmpty(CleanTime.Content.ContentValidation.Validate(TestAssets.StandardCatalog()));
        }

        [Test]
        public void EquipmentForUnknownDirt_IsReported()
        {
            var catalog = TestAssets.Catalog(
                dirt: new[] { TestAssets.Dirt("dirt.dust", 1, 1) },
                equipment: new[] { TestAssets.Tool("equipment.x", 0, 1f, true, "dirt.ghost") });

            var issues = CleanTime.Content.ContentValidation.Validate(catalog);

            Assert.IsTrue(issues.Any(i => i.Contains("unknown target 'dirt.ghost'")));
        }

        [Test]
        public void JobsAtUnknownLocations_AreReported()
        {
            var catalog = TestAssets.Catalog(jobs: new[] { TestAssets.Job("job.a", "location.nowhere", 1) });

            Assert.IsTrue(CleanTime.Content.ContentValidation.Validate(catalog).Any(i => i.Contains("unknown location")));
        }

        [Test]
        public void UnlockCycles_AreReported()
        {
            var catalog = TestAssets.Catalog(locations: new[]
            {
                TestAssets.Location("location.a", "A", false, ("location.b", 1)),
                TestAssets.Location("location.b", "B", false, ("location.a", 1)),
            });

            Assert.IsTrue(CleanTime.Content.ContentValidation.Validate(catalog).Any(i => i.Contains("cycle")));
        }

        [Test]
        public void EmptyEntriesAndIds_AreReported()
        {
            var catalog = TestAssets.Catalog(dirt: new CleanTime.Content.DirtDefinition[] { null, TestAssets.Dirt("", 1, 1) });

            var issues = CleanTime.Content.ContentValidation.Validate(catalog);

            Assert.IsTrue(issues.Any(i => i.Contains("empty")));
            Assert.IsTrue(issues.Any(i => i.Contains("has no id")));
        }

        [Test]
        public void ARegistry_IsBuiltFromValidContent()
        {
            var built = CleanTime.Content.ContentRegistry.Build(TestAssets.StandardCatalog());

            Assert.IsTrue(built.IsSuccess);
        }
    }
}
