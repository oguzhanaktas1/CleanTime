using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Dialogue.Domain
{
    public static class DialogueErrors
    {
        public const string UnknownDialogue = "dialogue.unknown";
        public const string UnknownNode = "dialogue.unknown_node";
        public const string Finished = "dialogue.finished";
        public const string InvalidChoice = "dialogue.invalid_choice";
    }

    public enum DialogueConditionType
    {
        RelationshipLevel,
        StoryMilestone,
        JobCompleted,
        CompanyLevel,
        QuestStatus,
        ItemOwned,
        GameDay,
    }

    public enum DialogueActionType
    {
        ActivateQuest,
        ApplyRelationshipChange,
        UnlockDialogue,
        ApplyStoryMilestone,
        GiveMoney,
    }

    /// <summary>Data-only condition. Numeric conditions mean "at least <see cref="Value"/>".</summary>
    public sealed class DialogueCondition
    {
        public DialogueCondition(DialogueConditionType type, string key, int value = 0)
        {
            Type = type;
            Key = key ?? string.Empty;
            Value = value;
        }

        public DialogueConditionType Type { get; }

        public string Key { get; }

        public int Value { get; }
    }

    public sealed class DialogueAction
    {
        public DialogueAction(DialogueActionType type, string key, int value = 0)
        {
            Type = type;
            Key = key ?? string.Empty;
            Value = value;
        }

        public DialogueActionType Type { get; }

        public string Key { get; }

        public int Value { get; }
    }

    public sealed class DialogueChoice
    {
        public DialogueChoice(
            string textKey,
            string targetNodeId,
            IReadOnlyList<DialogueCondition> conditions = null,
            IReadOnlyList<DialogueAction> actions = null)
        {
            TextKey = textKey ?? string.Empty;
            TargetNodeId = targetNodeId ?? string.Empty;
            Conditions = conditions ?? Empty<DialogueCondition>.List;
            Actions = actions ?? Empty<DialogueAction>.List;
        }

        public string TextKey { get; }

        /// <summary>Empty ends the dialogue.</summary>
        public string TargetNodeId { get; }

        public IReadOnlyList<DialogueCondition> Conditions { get; }

        public IReadOnlyList<DialogueAction> Actions { get; }
    }

    public sealed class DialogueNode
    {
        public DialogueNode(
            string id,
            string textKey,
            NpcId speaker = default,
            string nextNodeId = "",
            IReadOnlyList<DialogueChoice> choices = null,
            IReadOnlyList<DialogueAction> actions = null)
        {
            Id = Guard.NotEmpty(id, nameof(id));
            TextKey = textKey ?? string.Empty;
            Speaker = speaker;
            NextNodeId = nextNodeId ?? string.Empty;
            Choices = choices ?? Empty<DialogueChoice>.List;
            Actions = actions ?? Empty<DialogueAction>.List;
        }

        public string Id { get; }

        public string TextKey { get; }

        public NpcId Speaker { get; }

        /// <summary>Used when the node has no choices. Empty ends the dialogue.</summary>
        public string NextNodeId { get; }

        public IReadOnlyList<DialogueChoice> Choices { get; }

        /// <summary>Executed when the node is entered.</summary>
        public IReadOnlyList<DialogueAction> Actions { get; }
    }

    public sealed class DialogueGraph
    {
        private readonly Dictionary<string, DialogueNode> _nodes = new Dictionary<string, DialogueNode>();

        public DialogueGraph(DialogueId id, NpcId npcId, string startNodeId, IEnumerable<DialogueNode> nodes)
        {
            Id = id;
            NpcId = npcId;
            StartNodeId = Guard.NotEmpty(startNodeId, nameof(startNodeId));

            foreach (var node in nodes)
                _nodes[node.Id] = node;
        }

        public DialogueId Id { get; }

        /// <summary>The NPC this conversation is with (empty for ambient / narrator dialogue).</summary>
        public NpcId NpcId { get; }

        public string StartNodeId { get; }

        public IReadOnlyCollection<DialogueNode> Nodes => _nodes.Values;

        public bool TryGetNode(string nodeId, out DialogueNode node) => _nodes.TryGetValue(nodeId, out node);
    }
}
