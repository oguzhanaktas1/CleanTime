using System.Collections.Generic;
using System.Linq;
using CleanTime.Shared.Kernel;

namespace CleanTime.Progression.Domain
{
    public enum ProgressionUnlockKind
    {
        Company,
        JobTier,
        StoryMilestone,
    }

    /// <summary>Something a rule decided should be unlocked right now.</summary>
    public readonly struct ProgressionUnlock
    {
        public ProgressionUnlock(ProgressionUnlockKind kind, string key = "", int value = 0)
        {
            Kind = kind;
            Key = key ?? string.Empty;
            Value = value;
        }

        public ProgressionUnlockKind Kind { get; }

        public string Key { get; }

        public int Value { get; }
    }

    public interface IProgressionRule
    {
        /// <summary>Unlocks that are currently earned (idempotent; already-held unlocks are omitted).</summary>
        IReadOnlyList<ProgressionUnlock> Evaluate(PlayerProgression progression);
    }

    /// <summary>Unlocks company creation after a configured number of completed jobs.</summary>
    public sealed class CompanyUnlockRule : IProgressionRule
    {
        private readonly int _requiredJobs;

        public CompanyUnlockRule(int requiredJobs)
        {
            _requiredJobs = Guard.NotNegative(requiredJobs, nameof(requiredJobs));
        }

        public IReadOnlyList<ProgressionUnlock> Evaluate(PlayerProgression progression) =>
            !progression.CompanyUnlocked && progression.TotalJobsCompleted >= _requiredJobs
                ? new[] { new ProgressionUnlock(ProgressionUnlockKind.Company) }
                : Empty<ProgressionUnlock>.List;
    }

    /// <summary>Job tier N+1 opens once total completed jobs reach the Nth threshold.</summary>
    public sealed class JobTierRule : IProgressionRule
    {
        private readonly int[] _thresholds;

        public JobTierRule(IEnumerable<int> thresholds)
        {
            _thresholds = thresholds.OrderBy(t => t).ToArray();
        }

        public IReadOnlyList<ProgressionUnlock> Evaluate(PlayerProgression progression)
        {
            var tier = 1 + _thresholds.Count(t => progression.TotalJobsCompleted >= t);

            return tier > progression.JobTier
                ? new[] { new ProgressionUnlock(ProgressionUnlockKind.JobTier, value: tier) }
                : Empty<ProgressionUnlock>.List;
        }
    }

    /// <summary>Story milestone reached after a configured number of completed jobs.</summary>
    public sealed class JobCountMilestoneRule : IProgressionRule
    {
        private readonly string _milestoneId;
        private readonly int _requiredJobs;

        public JobCountMilestoneRule(string milestoneId, int requiredJobs)
        {
            _milestoneId = Guard.NotEmpty(milestoneId, nameof(milestoneId));
            _requiredJobs = Guard.NotNegative(requiredJobs, nameof(requiredJobs));
        }

        public IReadOnlyList<ProgressionUnlock> Evaluate(PlayerProgression progression) =>
            progression.TotalJobsCompleted >= _requiredJobs && !progression.ReachedMilestones.Contains(_milestoneId)
                ? new[] { new ProgressionUnlock(ProgressionUnlockKind.StoryMilestone, _milestoneId) }
                : Empty<ProgressionUnlock>.List;
    }
}
