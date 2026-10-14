using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Locations.Domain
{
    public enum PropertyTier
    {
        SmallApartment = 1,
        LargeApartment = 2,
        Villa = 3,
        Restaurant = 4,
        Mansion = 5,
    }

    public static class LocationRequirementTypes
    {
        public const string CompletionCount = "location_completion_count";
    }

    /// <summary>Unlock condition authored as data, e.g. "complete location X six times".</summary>
    public sealed class LocationUnlockRequirement
    {
        public LocationUnlockRequirement(string type, LocationId requiredLocationId, int requiredCompletionCount)
        {
            Type = Guard.NotEmpty(type, nameof(type));
            RequiredLocationId = requiredLocationId;
            RequiredCompletionCount = Guard.NotNegative(requiredCompletionCount, nameof(requiredCompletionCount));
        }

        public string Type { get; }

        public LocationId RequiredLocationId { get; }

        public int RequiredCompletionCount { get; }
    }

    /// <summary>
    /// Static location definition. <see cref="SceneKey"/> is an address/key resolved by the scene loader,
    /// never a hard-coded Unity scene name, so content can be remapped without breaking saves.
    /// </summary>
    public sealed class LocationDefinitionSnapshot
    {
        public LocationDefinitionSnapshot(
            LocationId id,
            string displayNameKey,
            PropertyTier tier,
            string sceneKey,
            bool unlockedByDefault,
            IReadOnlyList<LocationUnlockRequirement> unlockRequirements)
        {
            Id = id;
            DisplayNameKey = displayNameKey ?? string.Empty;
            Tier = tier;
            SceneKey = sceneKey ?? string.Empty;
            UnlockedByDefault = unlockedByDefault;
            UnlockRequirements = unlockRequirements ?? Empty<LocationUnlockRequirement>.List;
        }

        public LocationId Id { get; }

        public string DisplayNameKey { get; }

        public PropertyTier Tier { get; }

        public string SceneKey { get; }

        public bool UnlockedByDefault { get; }

        public IReadOnlyList<LocationUnlockRequirement> UnlockRequirements { get; }
    }
}
