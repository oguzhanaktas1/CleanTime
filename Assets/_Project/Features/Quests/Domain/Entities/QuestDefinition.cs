using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Quests.Domain
{
    public static class QuestErrors
    {
        public const string UnknownQuest = "quests.unknown_quest";
        public const string AlreadyActive = "quests.already_active";
        public const string NotCompleted = "quests.not_completed";
        public const string AlreadyClaimed = "quests.already_claimed";
        public const string NotActive = "quests.not_active";
    }

    /// <summary>Generic objective kinds; no quest has bespoke code.</summary>
    public enum QuestObjectiveType
    {
        CleanTargetCount,
        CleanProperty,
        RepairTargetCount,
        CompleteJob,
        EarnMoney,
        TalkToNpc,
        GiveGift,
        HireEmployee,
        BuyEquipment,
        ReachRelationshipLevel,
    }

    public sealed class QuestObjective
    {
        public QuestObjective(QuestObjectiveType type, string targetKey, int requiredCount)
        {
            Type = type;
            TargetKey = targetKey ?? string.Empty;
            RequiredCount = requiredCount < 1 ? 1 : requiredCount;
        }

        public QuestObjectiveType Type { get; }

        /// <summary>What the objective applies to (dirt type, npc, location...). Empty means "anything".</summary>
        public string TargetKey { get; }

        public int RequiredCount { get; }
    }

    public sealed class QuestReward
    {
        public QuestReward(Money money, string unlockKey = "")
        {
            Money = money;
            UnlockKey = unlockKey ?? string.Empty;
        }

        public Money Money { get; }

        public string UnlockKey { get; }

        public static QuestReward None => new QuestReward(Money.Zero);
    }

    public sealed class QuestDefinitionSnapshot
    {
        public QuestDefinitionSnapshot(
            QuestId id,
            string displayNameKey,
            IReadOnlyList<QuestObjective> objectives,
            QuestReward reward,
            bool activeByDefault = false)
        {
            Id = id;
            DisplayNameKey = displayNameKey ?? string.Empty;
            Objectives = objectives ?? Empty<QuestObjective>.List;
            Reward = reward ?? QuestReward.None;
            ActiveByDefault = activeByDefault;
        }

        public QuestId Id { get; }

        public string DisplayNameKey { get; }

        public IReadOnlyList<QuestObjective> Objectives { get; }

        public QuestReward Reward { get; }

        public bool ActiveByDefault { get; }
    }
}
