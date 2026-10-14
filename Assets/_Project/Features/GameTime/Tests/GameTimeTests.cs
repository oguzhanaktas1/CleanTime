using System.Collections.Generic;
using NUnit.Framework;
using CleanTime.GameTime.Application;
using CleanTime.GameTime.Domain;
using CleanTime.GameTime.Infrastructure;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;

namespace CleanTime.GameTime.Tests
{
    public sealed class GameCalendarTests
    {
        [Test]
        public void ConsumingActions_AdvancesThroughThePhases()
        {
            var calendar = new GameCalendar();
            var policy = new ActionsPerPhaseDayPhasePolicy(2);

            Assert.AreEqual(0, calendar.ConsumeActions(1, policy).PhaseChanges.Count);
            Assert.AreEqual(DayPhase.Morning, calendar.Now.Phase);

            var advance = calendar.ConsumeActions(1, policy);

            Assert.AreEqual(1, advance.PhaseChanges.Count);
            Assert.AreEqual(DayPhase.Afternoon, calendar.Now.Phase);
        }

        [Test]
        public void ConsumingManyActions_CanCrossSeveralPhasesAtOnce()
        {
            var calendar = new GameCalendar();

            var advance = calendar.ConsumeActions(3, new ActionsPerPhaseDayPhasePolicy(1));

            Assert.AreEqual(3, advance.PhaseChanges.Count);
            Assert.AreEqual(DayPhase.Night, calendar.Now.Phase);
            Assert.IsFalse(advance.DayEnded);
        }

        [Test]
        public void ExhaustingTheLastPhase_EndsTheDay()
        {
            var calendar = new GameCalendar();

            var advance = calendar.ConsumeActions(4, new ActionsPerPhaseDayPhasePolicy(1));

            Assert.IsTrue(advance.DayEnded);
        }

        [Test]
        public void StartNextDay_ResetsToMorning()
        {
            var calendar = new GameCalendar(new GameDate(3, DayPhase.Night), 0);

            calendar.StartNextDay();

            Assert.AreEqual(new GameDate(4, DayPhase.Morning), calendar.Now);
        }

        [Test]
        public void Phases_MustHaveAtLeastOneAction()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new ActionsPerPhaseDayPhasePolicy(new[] { 1, 0, 1, 1 }));
        }

        [Test]
        public void OnlyConfiguredActions_CostTime()
        {
            var policy = new DefaultGameActionPolicy();

            Assert.AreEqual(1, policy.GetCost(GameActionKind.JobCompleted));
            Assert.AreEqual(0, policy.GetCost(GameActionKind.Interaction));
        }
    }

    public sealed class GameDayFlowTests
    {
        private sealed class Flow
        {
            public EventBus Bus { get; } = new EventBus();
            public InMemoryGameTimeRepository Repository { get; }
            public ConsumeGameActionHandler Consume { get; }
            public EndGameDayHandler EndDay { get; }
            public DaySummaryProjection Summary { get; }

            public Flow(int actionsPerPhase = 1)
            {
                Repository = new InMemoryGameTimeRepository(new InMemorySnapshotStore());
                Summary = new DaySummaryProjection(Bus);
                var days = new GameDayService(Repository, Summary, Bus);
                Consume = new ConsumeGameActionHandler(
                    Repository, new DefaultGameActionPolicy(), new ActionsPerPhaseDayPhasePolicy(actionsPerPhase), days, Bus);
                EndDay = new EndGameDayHandler(days);
            }

            public void CompleteJobs(int count)
            {
                for (var i = 0; i < count; i++)
                    Consume.Handle(new ConsumeGameActionCommand(GameActionKind.JobCompleted));
            }
        }

        [Test]
        public void Consuming_PublishesActionAndPhaseEvents()
        {
            var flow = new Flow();
            using var phases = flow.Bus.Record<DayPhaseChangedEvent>();
            using var actions = flow.Bus.Record<ActionConsumedEvent>();

            flow.CompleteJobs(1);

            Assert.AreEqual(1, actions.Count);
            Assert.AreEqual(DayPhase.Morning, phases.Last.Previous);
            Assert.AreEqual(DayPhase.Afternoon, phases.Last.Current);
        }

        [Test]
        public void FullDay_EndsAndStartsTheNextOne_InOrder()
        {
            var flow = new Flow();
            var order = new List<string>();
            flow.Bus.Subscribe<GameDayEndedEvent>(_ => order.Add("ended"));
            flow.Bus.Subscribe<DaySummaryCreatedEvent>(_ => order.Add("summary"));
            flow.Bus.Subscribe<GameDayStartedEvent>(_ => order.Add("started"));

            flow.CompleteJobs(4);

            CollectionAssert.AreEqual(new[] { "ended", "summary", "started" }, order);
            Assert.AreEqual(new GameDate(2, DayPhase.Morning), flow.Repository.Get().Now);
        }

        [Test]
        public void EndDay_Command_SkipsTheRestOfTheDay()
        {
            var flow = new Flow();

            flow.EndDay.Handle(new EndGameDayCommand());

            Assert.AreEqual(2, flow.Repository.Get().Now.Day);
        }

        [Test]
        public void Summary_CollectsTheDaysFacts_AndResets()
        {
            var flow = new Flow();
            using var summaries = flow.Bus.Record<DaySummaryCreatedEvent>();

            flow.Bus.Publish(new CleanTime.Jobs.Domain.JobCompletedEvent(
                new JobId("j"), new JobDefinitionId("d"), new LocationId("l"), new Money(500), 1f,
                new CleanTime.Jobs.Domain.CustomerEvaluation(1, 1, 1, 1, 1f), 10f, 1));
            flow.Bus.Publish(new CleanTime.Economy.Domain.MoneyDebitedEvent(new Money(120), new Money(0), CleanTime.Economy.Domain.TransactionReason.GiftPurchase, "g"));
            flow.EndDay.Handle(new EndGameDayCommand());
            flow.EndDay.Handle(new EndGameDayCommand());

            Assert.AreEqual(2, summaries.Count);
            Assert.AreEqual(1, summaries.Events[0].Summary.JobsCompleted);
            Assert.AreEqual(500, summaries.Events[0].Summary.MoneyEarned.Cents);
            Assert.AreEqual(120, summaries.Events[0].Summary.MoneySpent.Cents);
            Assert.AreEqual(0, summaries.Events[1].Summary.JobsCompleted);
        }

        [Test]
        public void Clock_ReflectsTheCalendar()
        {
            var flow = new Flow();
            IGameClock clock = new GameClockAdapter(flow.Repository);

            flow.CompleteJobs(1);

            Assert.AreEqual(DayPhase.Afternoon, clock.Now.Phase);
        }

        [Test]
        public void SaveLoadRoundTrip_PreservesTheCalendar()
        {
            var store = new InMemorySnapshotStore();
            var repository = new InMemoryGameTimeRepository(store);
            repository.Save(new GameCalendar(new GameDate(12, DayPhase.Evening), 1));
            var slot = SaveId.New();
            repository.Flush(slot);

            repository.Reset();
            Assert.AreEqual(GameDate.Start, repository.Get().Now);

            Assert.IsTrue(repository.Load(slot).IsSuccess);
            Assert.AreEqual(new GameDate(12, DayPhase.Evening), repository.Get().Now);
            Assert.AreEqual(1, repository.Get().ActionsInPhase);
        }
    }
}
