using System;
using UnityEngine;
using CleanTime.Equipment.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Content
{
    /// <summary>
    /// Authoring data for a cleaning tool. Configuration only; owned / upgrade state lives in the save.
    /// Prefab and icon references are added with the Addressables package once art exists.
    /// </summary>
    [CreateAssetMenu(menuName = "Clean Time/Equipment Definition", fileName = "SO_Equipment_")]
    public sealed class EquipmentDefinition : ScriptableObject
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayNameKey = "";
        [SerializeField] private long priceCents;
        [Min(0.01f)] [SerializeField] private float baseCleaningPower = 1f;
        [Tooltip("Ids of the dirt / repair targets this tool can work on (see DirtDefinition ids).")]
        [SerializeField] private string[] supportedTargetIds = Array.Empty<string>();
        [SerializeField] private bool isStarter;
        [Min(0)] [SerializeField] private int maxUpgradeLevel;
        [Min(0f)] [SerializeField] private float powerPerUpgradeLevel;

        public string Id => id;

        public string DisplayNameKey => displayNameKey;

        public long PriceCents => priceCents;

        public float BaseCleaningPower => baseCleaningPower;

        public System.Collections.Generic.IReadOnlyList<string> SupportedTargetIds => supportedTargetIds;

        public EquipmentDefinitionSnapshot ToSnapshot() => new EquipmentDefinitionSnapshot(
            new EquipmentId(id),
            displayNameKey,
            new Money(priceCents),
            baseCleaningPower,
            supportedTargetIds,
            isStarter,
            maxUpgradeLevel,
            powerPerUpgradeLevel);
    }
}
