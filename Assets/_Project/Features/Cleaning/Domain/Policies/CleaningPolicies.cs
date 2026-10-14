using System.Collections.Generic;
using System.Linq;
using CleanTime.Equipment.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Domain
{
    /// <summary>Weighted progress: 1 - (sum of remaining weighted health) / (sum of weights).</summary>
    public sealed class CleaningProgressCalculator : ICleaningProgressCalculator
    {
        public float Calculate(IReadOnlyCollection<CleaningTarget> targets)
        {
            if (targets.Count == 0)
                return 1f;

            float total = targets.Sum(x => x.Weight);
            float remaining = targets.Sum(
                x => x.Weight * (x.CurrentHealth / x.MaxHealth));

            return 1f - (remaining / total);
        }
    }

    /// <summary>Data-driven matching: equipment lists the target ids it supports.</summary>
    public sealed class EquipmentCompatibilityPolicy : IEquipmentCompatibilityPolicy
    {
        public bool CanHandle(
            EquipmentDefinitionSnapshot equipment,
            DirtTypeId dirtType)
        {
            return equipment.SupportedTargetIds.Contains(dirtType.Value);
        }
    }
}
