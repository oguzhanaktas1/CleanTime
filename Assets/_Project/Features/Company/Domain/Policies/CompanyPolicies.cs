using System.Collections.Generic;
using System.Linq;
using CleanTime.Shared.Kernel;

namespace CleanTime.Company.Domain
{
    public interface ICompanyUpgradePolicy
    {
        Money GetCost(CompanyUpgradeDefinition definition, int currentLevel);

        /// <summary>Company level implied by its current standing.</summary>
        int CalculateLevel(CompanyProfile company);
    }

    /// <summary>Upgrade cost grows geometrically; company level follows reputation thresholds.</summary>
    public sealed class ReputationCompanyUpgradePolicy : ICompanyUpgradePolicy
    {
        private readonly int[] _reputationThresholds;

        public ReputationCompanyUpgradePolicy(IEnumerable<int> reputationThresholds = null)
        {
            _reputationThresholds = (reputationThresholds ?? new[] { 50, 150, 400, 900 }).OrderBy(t => t).ToArray();
        }

        public Money GetCost(CompanyUpgradeDefinition definition, int currentLevel)
        {
            decimal factor = 1m;
            for (var i = 0; i < currentLevel; i++)
                factor *= definition.CostGrowth;

            return definition.BaseCost.Scale(factor);
        }

        public int CalculateLevel(CompanyProfile company) =>
            1 + _reputationThresholds.Count(t => company.Reputation >= t);
    }
}
