using System;
using System.Linq;
using UnityEngine;
using CleanTime.Quests.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Content
{
    [CreateAssetMenu(menuName = "Clean Time/Quest Definition", fileName = "SO_Quest_")]
    public sealed class QuestDefinitionAsset : ScriptableObject
    {
        [Serializable]
        public struct Objective
        {
            public QuestObjectiveType type;
            [Tooltip("What the objective applies to (dirt id, npc id, location id...). Empty = anything.")]
            public string targetKey;
            [Min(1)] public int requiredCount;
        }

        [SerializeField] private string id = "";
        [SerializeField] private string displayNameKey = "";
        [SerializeField] private Objective[] objectives = Array.Empty<Objective>();
        [SerializeField] private long rewardCents;
        [SerializeField] private string unlockKey = "";
        [SerializeField] private bool activeByDefault;

        public string Id => id;

        public QuestDefinitionSnapshot ToSnapshot() => new QuestDefinitionSnapshot(
            new QuestId(id),
            displayNameKey,
            objectives.Select(o => new QuestObjective(o.type, o.targetKey, o.requiredCount)).ToList(),
            new QuestReward(new Money(rewardCents), unlockKey),
            activeByDefault);
    }
}
