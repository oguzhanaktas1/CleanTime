using System;
using System.Collections.Generic;
using CleanTime.Company.Domain;
using CleanTime.Economy.Domain;
using CleanTime.Employees.Domain;
using CleanTime.GameTime.Domain;
using CleanTime.Gifts.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Save.Application
{
    /// <summary>
    /// Remembers that gameplay state changed since the last successful save, so checkpoints only
    /// write when needed. Individual cleaning hits are deliberately not tracked.
    /// </summary>
    internal sealed class SaveDirtyTracker : IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public SaveDirtyTracker(IEventBus events)
        {
            Track<MoneyCreditedEvent>(events);
            Track<MoneyDebitedEvent>(events);
            Track<JobCompletedEvent>(events);
            Track<ActionConsumedEvent>(events);
            Track<GameDayStartedEvent>(events);
            Track<GiftGivenEvent>(events);
            Track<GiftPurchasedEvent>(events);
            Track<CompanyCreatedEvent>(events);
            Track<EmployeeHiredEvent>(events);
            Track<EmployeeAssignedEvent>(events);
            Track<EmployeeJobCompletedEvent>(events);
        }

        public bool IsDirty { get; private set; }

        public void MarkDirty() => IsDirty = true;

        public void Clear() => IsDirty = false;

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }

        private void Track<T>(IEventBus events)
            where T : IDomainEvent
        {
            _subscriptions.Add(events.Subscribe<T>(_ => IsDirty = true));
        }
    }
}
