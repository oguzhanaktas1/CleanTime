using System;
using CleanTime.Cleaning.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Jobs.Application
{
    /// <summary>
    /// Bridges the cleaning feature to the active job: forwards property progress to the job and
    /// completes the job when its property is fully cleaned. Primary behaviour stays in the handlers.
    /// </summary>
    internal sealed class JobCoordinator : IDisposable
    {
        private readonly IJobRepository _jobs;
        private readonly ICommandHandler<CompleteJobCommand> _complete;
        private readonly IEventBus _events;
        private readonly ILogger _logger;
        private readonly IDisposable _progress;
        private readonly IDisposable _completed;

        public JobCoordinator(
            IJobRepository jobs,
            ICommandHandler<CompleteJobCommand> complete,
            IEventBus events,
            ILogger logger)
        {
            _jobs = jobs;
            _complete = complete;
            _events = events;
            _logger = logger;
            _progress = events.Subscribe<PropertyCleaningProgressChangedEvent>(OnProgressChanged);
            _completed = events.Subscribe<PropertyCompletedEvent>(OnPropertyCompleted);
        }

        public void Dispose()
        {
            _progress.Dispose();
            _completed.Dispose();
        }

        private void OnProgressChanged(PropertyCleaningProgressChangedEvent evt)
        {
            if (!TryGetJobFor(evt.LocationId, out var job))
                return;

            job.UpdateProgress(evt.Progress);
            _jobs.SaveActive(job);
            _events.Publish(new JobObjectiveProgressChangedEvent(job.Id, JobObjectiveTypes.PropertyCleaning, evt.Progress));
        }

        private void OnPropertyCompleted(PropertyCompletedEvent evt)
        {
            if (!TryGetJobFor(evt.LocationId, out var job))
                return;

            var result = _complete.Handle(new CompleteJobCommand(job.Id, evt.Result));
            if (result.IsFailure)
                _logger.Warning($"Could not complete job {job.Id}: {result.Error}");
        }

        private bool TryGetJobFor(LocationId locationId, out Job job)
        {
            var active = _jobs.GetActive();

            if (active.IsSuccess &&
                active.Value.State == JobState.InProgress &&
                active.Value.Definition.LocationId == locationId)
            {
                job = active.Value;
                return true;
            }

            job = null;
            return false;
        }
    }
}
