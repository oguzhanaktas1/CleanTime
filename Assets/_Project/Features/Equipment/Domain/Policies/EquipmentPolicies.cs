using CleanTime.Shared.Kernel;

namespace CleanTime.Equipment.Domain
{
    public sealed class DefaultEquipmentPricePolicy : IEquipmentPricePolicy
    {
        public Money GetPurchasePrice(EquipmentDefinitionSnapshot definition) => definition.Price;
    }

    /// <summary>Each upgrade costs a growing fraction of the purchase price: price * factor * (level + 1).</summary>
    public sealed class LinearEquipmentUpgradePolicy : IEquipmentUpgradePolicy
    {
        private readonly decimal _costFactor;

        public LinearEquipmentUpgradePolicy(decimal costFactor = 0.5m)
        {
            _costFactor = costFactor;
        }

        public bool CanUpgrade(EquipmentProgress equipment) =>
            equipment.IsOwned && equipment.UpgradeLevel < equipment.Definition.MaxUpgradeLevel;

        public Money GetUpgradeCost(EquipmentProgress equipment) =>
            equipment.Definition.Price.Scale(_costFactor * (equipment.UpgradeLevel + 1));
    }
}
