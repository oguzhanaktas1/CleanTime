using System.Collections.Generic;
using System.Linq;

namespace CleanTime.Locations.Domain
{
    public interface ILocationUnlockPolicy
    {
        /// <summary>The <see cref="LocationUnlockRequirement.Type"/> this policy evaluates.</summary>
        string RequirementType { get; }

        bool IsSatisfied(LocationUnlockRequirement requirement, ProgressionSnapshot snapshot);
    }

    public sealed class CompletionCountUnlockPolicy : ILocationUnlockPolicy
    {
        public string RequirementType => LocationRequirementTypes.CompletionCount;

        public bool IsSatisfied(LocationUnlockRequirement requirement, ProgressionSnapshot snapshot)
        {
            var completed = snapshot.GetCompletionCount(requirement.RequiredLocationId);

            return completed >= requirement.RequiredCompletionCount;
        }
    }

    /// <summary>Dispatches each requirement to the policy that owns its type. Unknown types never unlock.</summary>
    public sealed class LocationUnlockEvaluator
    {
        private readonly IReadOnlyList<ILocationUnlockPolicy> _policies;

        public LocationUnlockEvaluator(IReadOnlyList<ILocationUnlockPolicy> policies)
        {
            _policies = policies;
        }

        public bool IsUnlockable(LocationDefinitionSnapshot location, ProgressionSnapshot snapshot)
        {
            if (location.UnlockRequirements.Count == 0)
                return location.UnlockedByDefault;

            return location.UnlockRequirements.All(requirement =>
            {
                var policy = _policies.FirstOrDefault(p => p.RequirementType == requirement.Type);
                return policy != null && policy.IsSatisfied(requirement, snapshot);
            });
        }
    }
}
