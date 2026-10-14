using CleanTime.Shared.Kernel;

namespace CleanTime.Equipment.Domain
{
    public sealed record EquipmentUnlockedEvent(EquipmentId EquipmentId) : IDomainEvent;

    public sealed record EquipmentPurchasedEvent(EquipmentId EquipmentId, Money Price) : IDomainEvent;

    public sealed record EquipmentEquippedEvent(EquipmentId EquipmentId) : IDomainEvent;

    /// <summary>The selected equipment changed (switching or equipping).</summary>
    public sealed record EquipmentChangedEvent(EquipmentId Previous, EquipmentId Current) : IDomainEvent;

    public sealed record EquipmentUpgradedEvent(EquipmentId EquipmentId, int UpgradeLevel) : IDomainEvent;
}
