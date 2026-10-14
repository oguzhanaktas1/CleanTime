using System;
using System.Linq;
using UnityEngine;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Content
{
    [CreateAssetMenu(menuName = "Clean Time/Job Definition", fileName = "SO_Job_")]
    public sealed class JobDefinitionAsset : ScriptableObject
    {
        [Serializable]
        public struct Objective
        {
            public string type;
            public float requiredProgress;
            public bool optional;
        }

        [SerializeField] private string id = "";
        [SerializeField] private string locationId = "";
        [SerializeField] private string customerId = "";
        [Min(1)] [SerializeField] private int difficulty = 1;
        [SerializeField] private long baseRewardCents;
        [Tooltip("Target duration in seconds for a full efficiency score. 0 disables the efficiency penalty.")]
        [Min(0f)] [SerializeField] private float parDurationSeconds;
        [SerializeField] private Objective[] objectives = { new Objective { type = JobObjectiveTypes.PropertyCleaning, requiredProgress = 1f } };

        public string Id => id;

        public string LocationId => locationId;

        public JobDefinitionSnapshot ToSnapshot() => new JobDefinitionSnapshot(
            new JobDefinitionId(id),
            new LocationId(locationId),
            new NpcId(customerId),
            difficulty,
            new Money(baseRewardCents),
            objectives.Select(o => new JobObjective(o.type, o.requiredProgress <= 0f ? 1f : o.requiredProgress, o.optional)).ToList(),
            parDurationSeconds);
    }
}
