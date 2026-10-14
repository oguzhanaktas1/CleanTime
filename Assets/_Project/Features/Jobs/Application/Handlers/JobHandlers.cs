using System;
using System.Collections.Generic;
using System.Linq;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Jobs.Application
{
    internal sealed class AcceptJobHandler : ICommandHandler<AcceptJobCommand>
    {
        private readonly IJobDefinitionProvider _definitions;
        private readonly IJobRepository _jobs;
        private readonly IJobAvailabilityPolicy _availability;
        private readonly IIdGenerator _ids;
        private readonly IRandomSource _random;
        private readonly IEventBus _events;

        public AcceptJobHandler(
            IJobDefinitionProvider definitions,
            IJobRepository jobs,
            IJobAvailabilityPolicy availability,
            IIdGenerator ids,
            IRandomSource random,
            IEventBus events)
        {
            _definitions = definitions;
            _jobs = jobs;
            _availability = availability;
            _ids = ids;
            _random = random;
            _events = events;
        }

        public Result Handle(AcceptJobCommand command)
        {
            var definition = _definitions.Get(command.DefinitionId);
            if (definition.IsFailure)
                return Result.Failure(JobErrors.NotFound, $"Unknown job '{command.DefinitionId}'.");

            if (_jobs.GetActive().IsSuccess)
                return Result.Failure(JobErrors.AlreadyActive, "Another job is already active.");

            if (_jobs.GetPendingResult().IsSuccess)
                return Result.Failure(JobErrors.ResultPending, "Dismiss the previous job result first.");

            if (!_availability.IsAvailable(definition.Value))
                return Result.Failure(JobErrors.NotAvailable, "This job is not available.");

            var job = Job.CreateAvailable(new JobId(_ids.NewId()), definition.Value, _random.Next(0, int.MaxValue));
            var accepted = job.Accept();
            if (accepted.IsFailure)
                return accepted;

            _jobs.SaveActive(job);
            _events.Publish(new JobAcceptedEvent(job.Id, job.Definition.Id, job.Definition.LocationId));
            return Result.Success();
        }
    }

    internal sealed class StartJobHandler : ICommandHandler<StartJobCommand>
    {
        private readonly IJobRepository _jobs;
        private readonly IGameClock _clock;
        private readonly IUtcClock _utcClock;
        private readonly IEventBus _events;

        public StartJobHandler(IJobRepository jobs, IGameClock clock, IUtcClock utcClock, IEventBus events)
        {
            _jobs = jobs;
            _clock = clock;
            _utcClock = utcClock;
            _events = events;
        }

        public Result Handle(StartJobCommand command)
        {
            var active = _jobs.GetActive();
            if (active.IsFailure)
                return Result.Failure(JobErrors.NoActiveJob, "There is no accepted job to start.");

            var job = active.Value;
            if (job.Id != command.JobId)
                return Result.Failure(JobErrors.WrongJob, "The job to start is not the active job.");

            var started = job.Start(_clock.Now.Day, _utcClock.UtcNow);
            if (started.IsFailure)
                return started;

            _jobs.SaveActive(job);
            _events.Publish(new JobStartedEvent(job.Id, job.Definition.Id, job.Definition.LocationId, job.StartedDay));
            return Result.Success();
        }
    }

    internal sealed class CompleteJobHandler : ICommandHandler<CompleteJobCommand>
    {
        private readonly IJobRepository _jobs;
        private readonly ICustomerRatingCalculator _rating;
        private readonly IJobRewardCalculator _reward;
        private readonly JobEvaluationSettings _settings;
        private readonly IGameClock _clock;
        private readonly IUtcClock _utcClock;
        private readonly IEventBus _events;

        public CompleteJobHandler(
            IJobRepository jobs,
            ICustomerRatingCalculator rating,
            IJobRewardCalculator reward,
            JobEvaluationSettings settings,
            IGameClock clock,
            IUtcClock utcClock,
            IEventBus events)
        {
            _jobs = jobs;
            _rating = rating;
            _reward = reward;
            _settings = settings;
            _clock = clock;
            _utcClock = utcClock;
            _events = events;
        }

        public Result Handle(CompleteJobCommand command)
        {
            var active = _jobs.GetActive();
            if (active.IsFailure)
                return Result.Failure(JobErrors.NoActiveJob, "There is no active job to complete.");

            var job = active.Value;
            if (job.Id != command.JobId)
                return Result.Failure(JobErrors.WrongJob, "The job to complete is not the active job.");

            if (job.State != JobState.InProgress)
                return Result.Failure(JobErrors.InvalidTransition, "Only an in-progress job can be completed.");

            var duration = (float)(_utcClock.UtcNow - job.StartedAtUtc).TotalSeconds;
            var input = CustomerEvaluationInput.From(
                command.Cleaning.Progress,
                optionalCompletion: 1f,
                durationSeconds: duration,
                parDurationSeconds: job.Definition.ParDurationSeconds,
                wrongToolAttempts: command.Cleaning.WrongEquipmentAttempts,
                mistakeTolerance: _settings.MistakeTolerance);

            var evaluation = _rating.Calculate(input);
            var reward = _reward.Calculate(job.Definition, evaluation);

            var completed = job.Complete();
            if (completed.IsFailure)
                return completed;

            var result = new JobResult(
                job.Id,
                job.Definition.Id,
                job.Definition.LocationId,
                command.Cleaning.Progress,
                evaluation,
                reward,
                duration,
                _clock.Now.Day);

            _jobs.AddHistory(result);
            _jobs.SetPendingResult(result);
            _jobs.ClearActive();

            _events.Publish(new CustomerRatingCalculatedEvent(job.Id, evaluation));
            _events.Publish(new JobCompletedEvent(
                job.Id,
                job.Definition.Id,
                job.Definition.LocationId,
                reward,
                evaluation.Score,
                evaluation,
                duration,
                result.CompletedDay));

            return Result.Success();
        }
    }

    internal sealed class AbandonJobHandler : ICommandHandler<AbandonJobCommand>
    {
        private readonly IJobRepository _jobs;
        private readonly IEventBus _events;

        public AbandonJobHandler(IJobRepository jobs, IEventBus events)
        {
            _jobs = jobs;
            _events = events;
        }

        public Result Handle(AbandonJobCommand command)
        {
            var active = _jobs.GetActive();
            if (active.IsFailure)
                return Result.Failure(JobErrors.NoActiveJob, "There is no active job to abandon.");

            var job = active.Value;
            if (job.Id != command.JobId)
                return Result.Failure(JobErrors.WrongJob, "The job to abandon is not the active job.");

            var abandoned = job.Abandon();
            if (abandoned.IsFailure)
                return abandoned;

            _jobs.ClearActive();
            _events.Publish(new JobFailedEvent(job.Id, job.Definition.Id, job.Definition.LocationId, "abandoned"));
            return Result.Success();
        }
    }

    internal sealed class FinishJobResultHandler : ICommandHandler<FinishJobResultCommand>
    {
        private readonly IJobRepository _jobs;
        private readonly IEventBus _events;

        public FinishJobResultHandler(IJobRepository jobs, IEventBus events)
        {
            _jobs = jobs;
            _events = events;
        }

        public Result Handle(FinishJobResultCommand command)
        {
            var pending = _jobs.GetPendingResult();
            if (pending.IsFailure)
                return Result.Failure(JobErrors.NotFound, "There is no job result to dismiss.");

            _jobs.ClearPendingResult();
            _events.Publish(new JobResultAcknowledgedEvent(pending.Value.JobId));
            return Result.Success();
        }
    }

    internal sealed class GetAvailableJobsHandler : IQueryHandler<GetAvailableJobsQuery, IReadOnlyList<JobDefinitionSnapshot>>
    {
        private readonly IJobDefinitionProvider _definitions;
        private readonly IJobAvailabilityPolicy _availability;

        public GetAvailableJobsHandler(IJobDefinitionProvider definitions, IJobAvailabilityPolicy availability)
        {
            _definitions = definitions;
            _availability = availability;
        }

        public IReadOnlyList<JobDefinitionSnapshot> Handle(GetAvailableJobsQuery query) =>
            _definitions.GetAll()
                .Where(_availability.IsAvailable)
                .OrderBy(d => d.Difficulty)
                .ThenBy(d => d.Id.Value, StringComparer.Ordinal)
                .ToList();
    }

    internal sealed class GetActiveJobHandler : IQueryHandler<GetActiveJobQuery, Result<Job>>
    {
        private readonly IJobRepository _jobs;

        public GetActiveJobHandler(IJobRepository jobs)
        {
            _jobs = jobs;
        }

        public Result<Job> Handle(GetActiveJobQuery query) => _jobs.GetActive();
    }

    internal sealed class GetJobProgressHandler : IQueryHandler<GetJobProgressQuery, float>
    {
        private readonly IJobRepository _jobs;

        public GetJobProgressHandler(IJobRepository jobs)
        {
            _jobs = jobs;
        }

        public float Handle(GetJobProgressQuery query)
        {
            var active = _jobs.GetActive();
            return active.IsSuccess ? active.Value.CleaningProgress : 0f;
        }
    }
}
