using CleanTime.Shared.Kernel;

namespace CleanTime.Employees.Domain
{
    public enum AssignmentStatus
    {
        Working,
        Resolved,
    }

    public sealed class EmployeeAssignment
    {
        public EmployeeAssignment(
            string id,
            EmployeeId employeeId,
            JobDefinitionId jobDefinitionId,
            int startedDay,
            int finishDay,
            int deterministicSeed)
        {
            Id = Guard.NotEmpty(id, nameof(id));
            EmployeeId = employeeId;
            JobDefinitionId = jobDefinitionId;
            StartedDay = startedDay;
            FinishDay = finishDay;
            DeterministicSeed = deterministicSeed;
            Status = AssignmentStatus.Working;
        }

        public string Id { get; }

        public EmployeeId EmployeeId { get; }

        public JobDefinitionId JobDefinitionId { get; }

        public int StartedDay { get; }

        public int FinishDay { get; }

        /// <summary>Seed stored at assignment time so the outcome is reproducible for debugging.</summary>
        public int DeterministicSeed { get; }

        public AssignmentStatus Status { get; private set; }

        public float? ResolvedQuality { get; private set; }

        public Money? ResolvedReward { get; private set; }

        public bool IsDue(int day) => Status == AssignmentStatus.Working && FinishDay <= day;

        public void Resolve(float quality, Money reward)
        {
            Status = AssignmentStatus.Resolved;
            ResolvedQuality = quality;
            ResolvedReward = reward;
        }
    }

    public sealed class EmployeeJobOutcome
    {
        public EmployeeJobOutcome(float quality, Money reward)
        {
            Quality = quality;
            Reward = reward;
        }

        public float Quality { get; }

        public Money Reward { get; }
    }

    /// <summary>Static, authored employee archetype that can be hired.</summary>
    public sealed class EmployeeDefinitionSnapshot
    {
        public EmployeeDefinitionSnapshot(
            EmployeeDefinitionId id,
            string displayNameKey,
            Money hireCost,
            EmployeeStats baseStats)
        {
            Id = id;
            DisplayNameKey = displayNameKey ?? string.Empty;
            HireCost = hireCost;
            BaseStats = Guard.NotNull(baseStats, nameof(baseStats));
        }

        public EmployeeDefinitionId Id { get; }

        public string DisplayNameKey { get; }

        public Money HireCost { get; }

        public EmployeeStats BaseStats { get; }
    }
}
