using System;
using UnityEngine;
using CleanTime.Cleaning.Application;
using CleanTime.Cleaning.Domain;
using CleanTime.Content;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Presentation
{
    /// <summary>
    /// Placed on scene objects to declare a cleaning target. The instance id must stay stable once
    /// assigned (mid-job saves refer to it), so it is generated once and never regenerated implicitly.
    /// </summary>
    [RequireComponent(typeof(CleaningTargetView))]
    public sealed class CleaningTargetAuthoring : MonoBehaviour
    {
        [SerializeField] private string stableInstanceId = "";
        [SerializeField] private DirtDefinition dirtDefinition;
        [SerializeField] private string roomId = "";
        [SerializeField] private float weightOverride = -1f;
        [SerializeField] private float healthOverride = -1f;

        public string StableInstanceId => stableInstanceId;

        public DirtDefinition DirtDefinition => dirtDefinition;

        public string RoomId => roomId;

        public CleaningTargetView View => GetComponent<CleaningTargetView>();

        public bool IsValid(out string problem)
        {
            if (string.IsNullOrWhiteSpace(stableInstanceId))
            {
                problem = "missing stable instance id";
                return false;
            }

            if (dirtDefinition == null)
            {
                problem = "no dirt definition assigned";
                return false;
            }

            problem = string.Empty;
            return true;
        }

        public CleaningTarget BuildDomain()
        {
            var health = healthOverride > 0
                ? healthOverride
                : dirtDefinition.DefaultHealth;

            var weight = weightOverride > 0
                ? weightOverride
                : dirtDefinition.DefaultWeight;

            return new CleaningTarget(
                stableInstanceId,
                new DirtTypeId(dirtDefinition.Id),
                health,
                weight,
                roomId);
        }

        public CleaningTargetSeed ToSeed()
        {
            var target = BuildDomain();
            return new CleaningTargetSeed(target.InstanceId, target.TypeId, target.MaxHealth, target.Weight, target.RoomId);
        }

        /// <summary>Called by editor tooling only.</summary>
        public void AssignNewStableId() => stableInstanceId = Guid.NewGuid().ToString("N");

        private void Reset()
        {
            if (string.IsNullOrEmpty(stableInstanceId))
                AssignNewStableId();
        }
    }
}
