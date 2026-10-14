using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Equipment.Domain
{
    /// <summary>Immutable, authored description of a piece of equipment (the runtime copy of the ScriptableObject).</summary>
    public sealed class EquipmentDefinitionSnapshot
    {
        private readonly HashSet<string> _supportedTargetIds;

        public EquipmentDefinitionSnapshot(
            EquipmentId id,
            string displayNameKey,
            Money price,
            float baseCleaningPower,
            IEnumerable<string> supportedTargetIds,
            bool isStarter = false,
            int maxUpgradeLevel = 0,
            float powerPerUpgradeLevel = 0f)
        {
            if (id.IsEmpty)
                throw new System.ArgumentException("Equipment id must not be empty.", nameof(id));

            Id = id;
            DisplayNameKey = displayNameKey ?? string.Empty;
            Price = price;
            BaseCleaningPower = Guard.Positive(baseCleaningPower, nameof(baseCleaningPower));
            _supportedTargetIds = new HashSet<string>(supportedTargetIds ?? System.Array.Empty<string>());
            IsStarter = isStarter;
            MaxUpgradeLevel = Guard.NotNegative(maxUpgradeLevel, nameof(maxUpgradeLevel));
            PowerPerUpgradeLevel = powerPerUpgradeLevel < 0f ? 0f : powerPerUpgradeLevel;
        }

        public EquipmentId Id { get; }

        public string DisplayNameKey { get; }

        public Money Price { get; }

        public float BaseCleaningPower { get; }

        /// <summary>Dirt / repair target ids this equipment can work on.</summary>
        public IReadOnlyCollection<string> SupportedTargetIds => _supportedTargetIds;

        /// <summary>Owned from the start of a new game.</summary>
        public bool IsStarter { get; }

        public int MaxUpgradeLevel { get; }

        public float PowerPerUpgradeLevel { get; }
    }
}
