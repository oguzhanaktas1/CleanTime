using System;
using CleanTime.Jobs.Domain;
using CleanTime.Locations.Domain;
using CleanTime.Progression.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Progression.Application
{
    /// <summary>
    /// JobCompleted -> total jobs += 1 and rules re-evaluated;
    /// LocationCompletionChanged -> location unlock requirements re-evaluated.
    /// </summary>
    internal sealed class ProgressionProjection : IDisposable
    {
        private readonly IProgressionRepository _progression;
        private readonly ICommandHandler<EvaluateProgressionCommand> _evaluate;
        private readonly IEventBus _events;
        private readonly IDisposable _jobs;
        private readonly IDisposable _locations;

        public ProgressionProjection(
            IProgressionRepository progression,
            ICommandHandler<EvaluateProgressionCommand> evaluate,
            IEventBus events)
        {
            _progression = progression;
            _evaluate = evaluate;
            _events = events;
            _jobs = events.Subscribe<JobCompletedEvent>(OnJobCompleted);
            _locations = events.Subscribe<LocationCompletionChangedEvent>(_ => _evaluate.Handle(new EvaluateProgressionCommand()));
        }

        public void Dispose()
        {
            _jobs.Dispose();
            _locations.Dispose();
        }

        private void OnJobCompleted(JobCompletedEvent evt)
        {
            var current = _progression.Get();
            if (current.IsFailure)
                return;

            current.Value.RegisterJobCompleted();
            _progression.Save(current.Value);
            _events.Publish(new PlayerProgressChangedEvent(current.Value.TotalJobsCompleted));
            _evaluate.Handle(new EvaluateProgressionCommand());
        }
    }
}
