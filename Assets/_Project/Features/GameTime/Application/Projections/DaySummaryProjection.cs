using System;
using CleanTime.Economy.Domain;
using CleanTime.Employees.Domain;
using CleanTime.GameTime.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Progression.Domain;
using CleanTime.Quests.Domain;
using CleanTime.Relationships.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.GameTime.Application
{
    /// <summary>Accumulates the facts of the current day so a <see cref="DaySummary"/> can be built when it ends.</summary>
    internal sealed class DaySummaryProjection : IDisposable
    {
        private readonly IDisposable[] _subscriptions;
        private int _jobs;
        private int _relationships;
        private int _quests;
        private int _locations;
        private Money _earned;
        private Money _employeeRevenue;
        private Money _spent;

        public DaySummaryProjection(IEventBus events)
        {
            _subscriptions = new[]
            {
                events.Subscribe<JobCompletedEvent>(e => { _jobs++; _earned += e.Reward; }),
                events.Subscribe<EmployeeRevenueGeneratedEvent>(e => _employeeRevenue += e.Revenue),
                events.Subscribe<MoneyDebitedEvent>(e => _spent += e.Amount),
                events.Subscribe<RelationshipLevelChangedEvent>(_ => _relationships++),
                events.Subscribe<QuestCompletedEvent>(_ => _quests++),
                events.Subscribe<LocationUnlockedEvent>(_ => _locations++),
            };
        }

        public DaySummary BuildAndReset(int day)
        {
            var summary = new DaySummary(day, _jobs, _earned, _employeeRevenue, _spent, _relationships, _quests, _locations);

            _jobs = _relationships = _quests = _locations = 0;
            _earned = _employeeRevenue = _spent = Money.Zero;
            return summary;
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }
    }
}
