using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Equipment.Domain
{
    public interface IEquipmentDefinitionProvider : IDefinitionProvider<EquipmentId, EquipmentDefinitionSnapshot>
    {
    }

    public interface IEquipmentRepository
    {
        /// <summary>Progress for any defined equipment, owned or not.</summary>
        Result<EquipmentProgress> Get(EquipmentId id);

        /// <summary>Progress for an equipment the player owns; fails with <see cref="EquipmentErrors.NotOwned"/> otherwise.</summary>
        Result<EquipmentProgress> GetOwned(EquipmentId id);

        IReadOnlyList<EquipmentProgress> GetAll();

        IReadOnlyList<EquipmentProgress> GetAllOwned();

        Result Save(EquipmentProgress equipment);

        EquipmentLoadout GetLoadout();

        Result SaveLoadout(EquipmentLoadout loadout);
    }

    public interface IEquipmentUpgradePolicy
    {
        bool CanUpgrade(EquipmentProgress equipment);

        Money GetUpgradeCost(EquipmentProgress equipment);
    }

    public interface IEquipmentPricePolicy
    {
        Money GetPurchasePrice(EquipmentDefinitionSnapshot definition);
    }
}
