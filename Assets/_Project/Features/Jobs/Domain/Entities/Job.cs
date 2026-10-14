using System;
using CleanTime.Shared.Kernel;

namespace CleanTime.Jobs.Domain
{
    public enum JobState
    {
        Available,
        Accepted,
        InProgress,
        Completed,
        Abandoned,
    }

    public static class JobErrors
    {
        public const string InvalidTransition = "jobs.invalid_transition";
        public const string NotFound = "jobs.not_found";
        public const string NoActiveJob = "jobs.no_active_job";
        public const string AlreadyActive = "jobs.already_active";
        public const string ResultPending = "jobs.result_pending";
        public const string NotAvailable = "jobs.not_available";
        public const string WrongJob = "jobs.wrong_job";
    }

    /// <summary>A concrete run of a job definition: Available -> Accepted -> InProgress -> Completed | Abandoned.</summary>
    public sealed class Job
    {
        private readonly StateMachine<JobState> _state;

        private Job(JobId id, JobDefinitionSnapshot definition, int seed, JobState state)
        {
            Id = id;
            Definition = Guard.NotNull(definition, nameof(definition));
            Seed = seed;
            _state = new StateMachine<JobState>(state)
                .Allow(JobState.Available, JobState.Accepted)
                .Allow(JobState.Accepted, JobState.InProgress, JobState.Abandoned)
                .Allow(JobState.InProgress, JobState.Completed, JobState.Abandoned);
        }

        public JobId Id { get; }

        public JobDefinitionSnapshot Definition { get; }

        /// <summary>Deterministic seed for any randomised outcome tied to this run.</summary>
        public int Seed { get; }

        public JobState State => _state.Current;

        public int StartedDay { get; private set; }

        public DateTime StartedAtUtc { get; private set; }

        /// <summary>Latest property cleaning progress, 0..1.</summary>
        public float CleaningProgress { get; private set; }

        public static Job CreateAvailable(JobId id, JobDefinitionSnapshot definition, int seed) =>
            new Job(id, definition, seed, JobState.Available);

        public Result Accept() => _state.TransitionTo(JobState.Accepted);

        public Result Start(int gameDay, DateTime utcNow)
        {
            var result = _state.TransitionTo(JobState.InProgress);
            if (result.IsFailure)
                return result;

            StartedDay = gameDay;
            StartedAtUtc = utcNow;
            return Result.Success();
        }

        public Result UpdateProgress(float cleaningProgress)
        {
            if (State != JobState.InProgress)
                return Result.Failure(JobErrors.InvalidTransition, "Progress can only change while the job is in progress.");

            CleaningProgress = Math.Max(0f, Math.Min(1f, cleaningProgress));
            return Result.Success();
        }

        public Result Complete() => _state.TransitionTo(JobState.Completed);

        public Result Abandon() => _state.TransitionTo(JobState.Abandoned);
    }
}
