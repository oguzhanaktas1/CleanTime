using System.Collections.Generic;
using CleanTime.Equipment.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Equipment.Application
{
    public sealed record EquipItemCommand(EquipmentId EquipmentId) : ICommand;

    public sealed record SwitchEquipmentCommand(SwitchDirection Direction) : ICommand;

    public sealed record PurchaseEquipmentCommand(EquipmentId EquipmentId) : ICommand;

    public sealed record UpgradeEquipmentCommand(EquipmentId EquipmentId) : ICommand;

    public sealed record GetCurrentEquipmentQuery : IQuery<Result<EquipmentProgress>>;

    public sealed record GetOwnedEquipmentQuery : IQuery<IReadOnlyList<EquipmentProgress>>;

    /// <summary>Cross-module read contract for "what tool is the player holding?".</summary>
    public interface ISelectedEquipmentQuery
    {
        /// <summary>The selected equipment id, or an empty id when nothing is selected.</summary>
        EquipmentId GetSelected();
    }
}
