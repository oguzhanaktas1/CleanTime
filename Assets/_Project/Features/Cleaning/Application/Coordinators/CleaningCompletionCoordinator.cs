using System;
using System.Linq;
using CleanTime.Cleaning.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Application
{
    /// <summary>
    /// Derives room / property progress and completion from target events, and tears the session
    /// down when its job fails. Keeps the per-hit handler small and single-purpose.
    /// </summary>
    internal sealed class CleaningCompletionCoordinator : IDisposable
    {
        private readonly ICleaningTargetRepository _targets;
        private readonly ICleaningSessionRepository _sessions;
        private readonly ICleaningProgressCalculator _progress;
        private readonly IEventBus _events;
        private readonly IDisposable[] _subscriptions;

        public CleaningCompletionCoordinator(
            ICleaningTargetRepository targets,
            ICleaningSessionRepository sessions,
            ICleaningProgressCalculator progress,
            IEventBus events)
        {
            _targets = targets;
            _sessions = sessions;
            _progress = progress;
            _events = events;

            _subscriptions = new[]
            {
                events.Subscribe<CleaningHitAppliedEvent>(OnHitApplied),
                events.Subscribe<CleaningTargetCompletedEvent>(OnTargetCompleted),
                events.Subscribe<WrongEquipmentUsedEvent>(OnWrongEquipment),
                events.Subscribe<JobFailedEvent>(OnJobFailed),
            };
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }

        private void OnHitApplied(CleaningHitAppliedEvent evt)
        {
            var session = _sessions.GetCurrent();
            if (session.IsFailure)
                return;

            session.Value.RegisterHit();

            _events.Publish(new PropertyCleaningProgressChangedEvent(
                session.Value.LocationId,
                _progress.Calculate(_targets.GetAll())));

            var target = _targets.Get(evt.TargetInstanceId);
            if (target.IsSuccess && target.Value.RoomId.Length > 0)
                _events.Publish(new RoomCleaningProgressChangedEvent(target.Value.RoomId, RoomProgress(target.Value.RoomId)));
        }

        private void OnTargetCompleted(CleaningTargetCompletedEvent evt)
        {
            var session = _sessions.GetCurrent();
            if (session.IsFailure)
                return;

            var target = _targets.Get(evt.TargetInstanceId);
            if (target.IsSuccess && target.Value.RoomId.Length > 0 &&
                _targets.GetAll().Where(t => t.RoomId == target.Value.RoomId).All(t => t.IsCompleted))
            {
                _events.Publish(new RoomCompletedEvent(target.Value.RoomId));
            }

            var all = _targets.GetAll();
            if (all.All(t => t.IsCompleted) && session.Value.MarkCompleted())
            {
                var result = session.Value.ToResult(_progress.Calculate(all), all.Count);
                _events.Publish(new PropertyCompletedEvent(session.Value.LocationId, result));
            }
        }

        private void OnWrongEquipment(WrongEquipmentUsedEvent evt)
        {
            var session = _sessions.GetCurrent();
            if (session.IsSuccess)
                session.Value.RegisterMistake();
        }

        private void OnJobFailed(JobFailedEvent evt)
        {
            var session = _sessions.GetCurrent();
            if (session.IsFailure || session.Value.JobId != evt.JobId)
                return;

            var location = session.Value.LocationId;
            _targets.Clear();
            _sessions.Clear();
            _events.Publish(new CleaningCancelledEvent(location));
        }

        private float RoomProgress(string roomId)
        {
            var room = _targets.GetAll().Where(t => t.RoomId == roomId).ToList();
            return _progress.Calculate(room);
        }
    }
}
