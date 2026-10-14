using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Progression.Domain
{
    /// <summary>Player-wide progress that is not owned by a more specific module.</summary>
    public sealed class PlayerProgression
    {
        private readonly HashSet<string> _milestones;

        public PlayerProgression(
            int totalJobsCompleted = 0,
            bool companyUnlocked = false,
            int storyLevel = 0,
            int jobTier = 1,
            IEnumerable<string> milestones = null)
        {
            TotalJobsCompleted = Guard.NotNegative(totalJobsCompleted, nameof(totalJobsCompleted));
            CompanyUnlocked = companyUnlocked;
            StoryLevel = Guard.NotNegative(storyLevel, nameof(storyLevel));
            JobTier = jobTier < 1 ? 1 : jobTier;
            _milestones = new HashSet<string>(milestones ?? System.Array.Empty<string>());
        }

        public int TotalJobsCompleted { get; private set; }

        public bool CompanyUnlocked { get; private set; }

        public int StoryLevel { get; private set; }

        /// <summary>Highest job difficulty tier the player may take (1-based).</summary>
        public int JobTier { get; private set; }

        public IReadOnlyCollection<string> ReachedMilestones => _milestones;

        public void RegisterJobCompleted() => TotalJobsCompleted++;

        public bool UnlockCompany()
        {
            if (CompanyUnlocked)
                return false;

            CompanyUnlocked = true;
            return true;
        }

        public bool RaiseJobTier(int tier)
        {
            if (tier <= JobTier)
                return false;

            JobTier = tier;
            return true;
        }

        public bool ReachMilestone(string milestoneId)
        {
            if (!_milestones.Add(milestoneId))
                return false;

            StoryLevel++;
            return true;
        }
    }
}
