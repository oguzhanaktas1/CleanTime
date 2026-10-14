using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Jobs.Domain
{
    public static class JobObjectiveTypes
    {
        public const string PropertyCleaning = "property_cleaning";
    }

    public sealed class JobObjective
    {
        public JobObjective(string type, float requiredProgress, bool isOptional = false)
        {
            Type = Guard.NotEmpty(type, nameof(type));
            RequiredProgress = Guard.Positive(requiredProgress, nameof(requiredProgress));
            IsOptional = isOptional;
        }

        public string Type { get; }

        public float RequiredProgress { get; }

        public bool IsOptional { get; }
    }

    /// <summary>Static, authored description of a job a customer offers.</summary>
    public sealed class JobDefinitionSnapshot
    {
        public JobDefinitionSnapshot(
            JobDefinitionId id,
            LocationId locationId,
            NpcId customerId,
            int difficulty,
            Money baseReward,
            IReadOnlyList<JobObjective> objectives,
            float parDurationSeconds = 0f)
        {
            Id = id;
            LocationId = locationId;
            CustomerId = customerId;
            Difficulty = difficulty < 1 ? 1 : difficulty;
            BaseReward = baseReward;
            Objectives = objectives ?? Empty<JobObjective>.List;
            ParDurationSeconds = parDurationSeconds < 0f ? 0f : parDurationSeconds;
        }

        public JobDefinitionId Id { get; }

        public LocationId LocationId { get; }

        public NpcId CustomerId { get; }

        public int Difficulty { get; }

        public Money BaseReward { get; }

        public IReadOnlyList<JobObjective> Objectives { get; }

        /// <summary>Target time for full efficiency score; 0 disables the efficiency penalty.</summary>
        public float ParDurationSeconds { get; }
    }
}
