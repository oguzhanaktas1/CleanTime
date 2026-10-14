using System;
using System.Collections.Generic;
using System.Linq;
using CleanTime.Shared.Kernel;

namespace CleanTime.Quests.Domain
{
    public enum QuestState
    {
        Active,
        Completed,
        Claimed,
    }

    /// <summary>A fact observed in the game that may advance quest objectives.</summary>
    public sealed class QuestSignal
    {
        public QuestSignal(QuestObjectiveType type, string key, int amount = 1)
        {
            Type = type;
            Key = key ?? string.Empty;
            Amount = amount;
        }

        public QuestObjectiveType Type { get; }

        public string Key { get; }

        public int Amount { get; }

        public static QuestSignal CleanTarget(string dirtTypeId, int amount = 1) => new QuestSignal(QuestObjectiveType.CleanTargetCount, dirtTypeId, amount);

        public static QuestSignal RepairTarget(string dirtTypeId, int amount = 1) => new QuestSignal(QuestObjectiveType.RepairTargetCount, dirtTypeId, amount);

        public static QuestSignal CleanProperty(string locationId) => new QuestSignal(QuestObjectiveType.CleanProperty, locationId);

        public static QuestSignal CompleteJob(string jobDefinitionId) => new QuestSignal(QuestObjectiveType.CompleteJob, jobDefinitionId);

        public static QuestSignal EarnMoney(long cents) => new QuestSignal(QuestObjectiveType.EarnMoney, string.Empty, (int)Math.Min(cents, int.MaxValue));

        public static QuestSignal TalkToNpc(string npcId) => new QuestSignal(QuestObjectiveType.TalkToNpc, npcId);

        public static QuestSignal GiveGift(string npcId) => new QuestSignal(QuestObjectiveType.GiveGift, npcId);

        public static QuestSignal HireEmployee(string definitionId) => new QuestSignal(QuestObjectiveType.HireEmployee, definitionId);

        public static QuestSignal BuyEquipment(string equipmentId) => new QuestSignal(QuestObjectiveType.BuyEquipment, equipmentId);

        /// <summary>Carries the NPC's current level in <see cref="Amount"/>; the objective compares, not accumulates.</summary>
        public static QuestSignal RelationshipLevel(string npcId, int level) => new QuestSignal(QuestObjectiveType.ReachRelationshipLevel, npcId, level);
    }

    public interface IQuestObjectiveEvaluator
    {
        /// <summary>
        /// New progress for the objective after the signal, or the unchanged progress when the signal does not apply.
        /// </summary>
        int Evaluate(QuestObjective objective, int currentProgress, QuestSignal signal);
    }

    public sealed class DefaultQuestObjectiveEvaluator : IQuestObjectiveEvaluator
    {
        public int Evaluate(QuestObjective objective, int currentProgress, QuestSignal signal)
        {
            if (objective.Type != signal.Type)
                return currentProgress;

            if (objective.TargetKey.Length > 0 && !string.Equals(objective.TargetKey, signal.Key, StringComparison.Ordinal))
                return currentProgress;

            if (objective.Type == QuestObjectiveType.ReachRelationshipLevel)
                return Math.Min(objective.RequiredCount, Math.Max(currentProgress, signal.Amount));

            return Math.Min(objective.RequiredCount, currentProgress + Math.Max(0, signal.Amount));
        }
    }

    /// <summary>Player's progress through one quest.</summary>
    public sealed class QuestInstance
    {
        private readonly int[] _progress;

        public QuestInstance(QuestDefinitionSnapshot definition, QuestState state = QuestState.Active, IReadOnlyList<int> progress = null)
        {
            Definition = Guard.NotNull(definition, nameof(definition));
            State = state;
            _progress = new int[definition.Objectives.Count];

            if (progress != null)
            {
                for (var i = 0; i < _progress.Length && i < progress.Count; i++)
                    _progress[i] = progress[i];
            }
        }

        public QuestDefinitionSnapshot Definition { get; }

        public QuestId Id => Definition.Id;

        public QuestState State { get; private set; }

        public IReadOnlyList<int> Progress => _progress;

        public bool AllObjectivesMet
        {
            get
            {
                for (var i = 0; i < _progress.Length; i++)
                {
                    if (_progress[i] < Definition.Objectives[i].RequiredCount)
                        return false;
                }

                return true;
            }
        }

        /// <summary>Applies a signal. Returns true when any objective progress changed.</summary>
        public bool TryApply(QuestSignal signal, IQuestObjectiveEvaluator evaluator)
        {
            if (State != QuestState.Active)
                return false;

            var changed = false;

            for (var i = 0; i < _progress.Length; i++)
            {
                var next = evaluator.Evaluate(Definition.Objectives[i], _progress[i], signal);
                if (next == _progress[i])
                    continue;

                _progress[i] = next;
                changed = true;
            }

            return changed;
        }

        /// <summary>Moves Active -> Completed once every objective is met. Returns true on the transition.</summary>
        public bool TryComplete()
        {
            if (State != QuestState.Active || !AllObjectivesMet)
                return false;

            State = QuestState.Completed;
            return true;
        }

        public Result Claim()
        {
            if (State == QuestState.Claimed)
                return Result.Failure(QuestErrors.AlreadyClaimed, "The reward was already claimed.");

            if (State != QuestState.Completed)
                return Result.Failure(QuestErrors.NotCompleted, "The quest is not completed yet.");

            State = QuestState.Claimed;
            return Result.Success();
        }

        public string SerializeProgress() => string.Join(",", _progress);

        public static IReadOnlyList<int> ParseProgress(string text) =>
            string.IsNullOrEmpty(text)
                ? Array.Empty<int>()
                : text.Split(',').Select(p => int.TryParse(p, out var v) ? v : 0).ToArray();
    }
}
