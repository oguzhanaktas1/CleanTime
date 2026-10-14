using System;
using CleanTime.Shared.Kernel;

namespace CleanTime.Employees.Domain
{
    public static class EmployeeErrors
    {
        public const string NotFound = "employees.not_found";
        public const string UnknownDefinition = "employees.unknown_definition";
        public const string NotIdle = "employees.not_idle";
        public const string InvalidTransition = "employees.invalid_transition";
        public const string NoCompany = "employees.no_company";
        public const string CapacityReached = "employees.capacity_reached";
        public const string NoAssignment = "employees.no_assignment";
        public const string UnknownJob = "employees.unknown_job";
    }

    public enum EmployeeState
    {
        Idle,
        Assigned,
        Working,
        Completed,
    }

    public sealed class EmployeeStats
    {
        public EmployeeStats(int cleaningSkill, int speedSkill, int reliabilitySkill)
        {
            CleaningSkill = Guard.NotNegative(cleaningSkill, nameof(cleaningSkill));
            SpeedSkill = Guard.NotNegative(speedSkill, nameof(speedSkill));
            ReliabilitySkill = Guard.NotNegative(reliabilitySkill, nameof(reliabilitySkill));
        }

        public int CleaningSkill { get; }

        public int SpeedSkill { get; }

        public int ReliabilitySkill { get; }
    }

    /// <summary>A hired employee. Idle -> Assigned -> Working -> Completed -> (collect) Idle.</summary>
    public sealed class Employee
    {
        private readonly StateMachine<EmployeeState> _state;

        public Employee(
            EmployeeId id,
            EmployeeDefinitionId definitionId,
            EmployeeStats stats,
            int hiredDay,
            int level = 1,
            EmployeeState state = EmployeeState.Idle)
        {
            Id = id;
            DefinitionId = definitionId;
            Stats = Guard.NotNull(stats, nameof(stats));
            HiredDay = hiredDay;
            Level = level < 1 ? 1 : level;
            _state = new StateMachine<EmployeeState>(state)
                .Allow(EmployeeState.Idle, EmployeeState.Assigned)
                .Allow(EmployeeState.Assigned, EmployeeState.Working, EmployeeState.Idle)
                .Allow(EmployeeState.Working, EmployeeState.Completed, EmployeeState.Idle)
                .Allow(EmployeeState.Completed, EmployeeState.Idle);
        }

        public EmployeeId Id { get; }

        public EmployeeDefinitionId DefinitionId { get; }

        public EmployeeStats Stats { get; }

        public int HiredDay { get; }

        public int Level { get; }

        public EmployeeState State => _state.Current;

        public bool IsIdle => State == EmployeeState.Idle;

        public Result Assign() => _state.TransitionTo(EmployeeState.Assigned);

        public Result Start() => _state.TransitionTo(EmployeeState.Working);

        public Result Resolve() => _state.TransitionTo(EmployeeState.Completed);

        /// <summary>Hands the finished work in and returns the employee to the pool.</summary>
        public Result Collect() => _state.TransitionTo(EmployeeState.Idle);

        /// <summary>Aborts an assignment that has not been resolved yet.</summary>
        public Result CancelAssignment() => _state.TransitionTo(EmployeeState.Idle);

        /// <summary>Whole days needed for a job of the given difficulty; faster employees finish sooner (min. 1).</summary>
        public int EstimateDurationDays(int difficulty) =>
            Math.Max(1, difficulty - Stats.SpeedSkill / 25);
    }
}
