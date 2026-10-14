using UnityEngine;
using CleanTime.Company.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Content
{
    [CreateAssetMenu(menuName = "Clean Time/Company Upgrade Definition", fileName = "SO_CompanyUpgrade_")]
    public sealed class CompanyUpgradeDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayNameKey = "";
        [Min(0)] [SerializeField] private int maxLevel = 3;
        [SerializeField] private long baseCostCents;
        [Min(1f)] [SerializeField] private float costGrowth = 1.5f;

        public string Id => id;

        public CompanyUpgradeDefinition ToDefinition() => new CompanyUpgradeDefinition(
            new CompanyUpgradeId(id), displayNameKey, maxLevel, new Money(baseCostCents), (decimal)costGrowth);
    }
}
