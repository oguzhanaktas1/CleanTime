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
    /// Saves at checkpoints (architecture §53). Must be initialised after every module that mutates
    /// state in response to these events, so a checkpoint never captures a half-applied change.
    /// Day-end is checkpointed on GameDayStarted, once the previous day has been fully settled.
    /// </summary>
    internal sealed class AutosaveService : IDisposable
    {
        private readonly ISaveCoordinator _coordinator;
        private readonly SaveDirtyTracker _dirty;
        private readonly ILogger _logger;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public AutosaveService(IEventBus events, ISaveCoordinator coordinator, SaveDirtyTracker dirty, ILogger logger)
        {
            _coordinator = coordinator;
            _dirty = dirty;
            _logger = logger;

            Checkpoint<JobCompletedEvent>(events);
            Checkpoint<GameDayStartedEvent>(events);
            Checkpoint<PurchaseCompletedEvent>(events);
            Checkpoint<GiftGivenEvent>(events);
            Checkpoint<CompanyCreatedEvent>(events);
            Checkpoint<EmployeeJobCompletedEvent>(events);
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }

        /// <summary>Saves now if a slot is active and something changed.</summary>
        public Result SaveIfDirty()
        {
            if (_coordinator.CurrentSaveId.IsEmpty || !_dirty.IsDirty)
                return Result.Success();

            var result = _coordinator.SaveCurrentGame();
            if (result.IsFailure)
                _logger.Error($"Autosave failed: {result.Error}");

            return result;
        }

        private void Checkpoint<T>(IEventBus events)
            where T : IDomainEvent
        {
            _subscriptions.Add(events.Subscribe<T>(_ => SaveIfDirty()));
        }
    }
}
