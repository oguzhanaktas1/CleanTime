using System;
using System.Linq;
using UnityEngine;
using CleanTime.Locations.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Content
{
    [CreateAssetMenu(menuName = "Clean Time/Location Definition", fileName = "SO_Location_")]
    public sealed class LocationDefinitionAsset : ScriptableObject
    {
        [Serializable]
        public struct UnlockRequirement
        {
            public string type;
            public string requiredLocationId;
            public int requiredCompletionCount;
        }

        [SerializeField] private string id = "";
        [SerializeField] private string displayNameKey = "";
        [SerializeField] private PropertyTier tier = PropertyTier.SmallApartment;
        [Tooltip("Key the scene loader resolves to a scene (e.g. Property.SmallApartment01). Not a hard-coded scene path.")]
        [SerializeField] private string sceneKey = "";
        [SerializeField] private bool unlockedByDefault;
        [SerializeField] private UnlockRequirement[] unlockRequirements = Array.Empty<UnlockRequirement>();

        public string Id => id;

        public string SceneKey => sceneKey;

        public System.Collections.Generic.IEnumerable<(string Type, string LocationId)> RequirementReferences =>
            unlockRequirements.Select(r => (r.type, r.requiredLocationId));

        public LocationDefinitionSnapshot ToSnapshot() => new LocationDefinitionSnapshot(
            new LocationId(id),
            displayNameKey,
            tier,
            sceneKey,
            unlockedByDefault,
            unlockRequirements
                .Select(r => new LocationUnlockRequirement(
                    string.IsNullOrEmpty(r.type) ? LocationRequirementTypes.CompletionCount : r.type,
                    new LocationId(r.requiredLocationId ?? string.Empty),
                    r.requiredCompletionCount))
                .ToList());
    }
}
