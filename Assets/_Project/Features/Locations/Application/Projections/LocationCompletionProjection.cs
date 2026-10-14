using System;
using CleanTime.Jobs.Domain;
using CleanTime.Locations.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Locations.Application
{
    /// <summary>JobCompleted -> completion_count += 1 (read model owned by Locations).</summary>
    internal sealed class LocationCompletionProjection : IDisposable
    {
        private readonly ILocationProgressRepository _progress;
        private readonly IEventBus _events;
        private readonly ILogger _logger;
        private readonly IDisposable _subscription;

        public LocationCompletionProjection(ILocationProgressRepository progress, IEventBus events, ILogger logger)
        {
            _progress = progress;
            _events = events;
            _logger = logger;
            _subscription = events.Subscribe<JobCompletedEvent>(OnJobCompleted);
        }

        public void Dispose() => _subscription.Dispose();

        private void OnJobCompleted(JobCompletedEvent evt)
        {
            var progressResult = _progress.Get(evt.LocationId);
            if (progressResult.IsFailure)
            {
                _logger.Warning($"Job completed for unknown location '{evt.LocationId}'.");
                return;
            }

            var progress = progressResult.Value;
            progress.RegisterCompletion(evt.CustomerScore, evt.DurationSeconds, evt.GameDay);
            _progress.Save(progress);

            _events.Publish(new LocationCompletionChangedEvent(progress.LocationId, progress.CompletionCount));
        }
    }
}
