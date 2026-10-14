using CleanTime.Shared.Kernel;

namespace CleanTime.Equipment.Domain
{
    public static class EquipmentErrors
    {
        public const string NotFound = "equipment.not_found";
        public const string NotOwned = "equipment.not_owned";
        public const string AlreadyOwned = "equipment.already_owned";
        public const string MaxUpgradeReached = "equipment.max_upgrade_reached";
        public const string LoadoutEmpty = "equipment.loadout_empty";
    }

    /// <summary>Player progress for one piece of equipment (owned flag + upgrade level) plus its definition.</summary>
    public sealed class EquipmentProgress
    {
        public EquipmentProgress(EquipmentDefinitionSnapshot definition, bool isOwned = false, int upgradeLevel = 0)
        {
            Definition = Guard.NotNull(definition, nameof(definition));
            IsOwned = isOwned;
            UpgradeLevel = Guard.NotNegative(upgradeLevel, nameof(upgradeLevel));
        }

        public EquipmentDefinitionSnapshot Definition { get; }

        public EquipmentId Id => Definition.Id;

        public bool IsOwned { get; private set; }

        public int UpgradeLevel { get; private set; }

        public float EffectiveCleaningPower =>
            Definition.BaseCleaningPower + UpgradeLevel * Definition.PowerPerUpgradeLevel;

        public Result MarkOwned()
        {
            if (IsOwned)
                return Result.Failure(EquipmentErrors.AlreadyOwned, $"'{Id}' is already owned.");

            IsOwned = true;
            return Result.Success();
        }

        public Result Upgrade()
        {
            if (!IsOwned)
                return Result.Failure(EquipmentErrors.NotOwned, $"'{Id}' is not owned.");

            if (UpgradeLevel >= Definition.MaxUpgradeLevel)
                return Result.Failure(EquipmentErrors.MaxUpgradeReached, $"'{Id}' is fully upgraded.");

            UpgradeLevel++;
            return Result.Success();
        }
    }
}
