using System.Collections.Generic;
using System.Linq;
using CleanTime.Shared.Kernel;

namespace CleanTime.Dialogue.Domain
{
    /// <summary>Resolves conditions against the rest of the game (implemented by composition, not by Dialogue).</summary>
    public interface IDialogueConditionResolver
    {
        bool IsSatisfied(DialogueCondition condition);
    }

    /// <summary>Applies dialogue consequences to the rest of the game.</summary>
    public interface IDialogueActionExecutor
    {
        Result Execute(DialogueAction action);
    }

    /// <summary>A running conversation: walks the graph, filtering choices by condition.</summary>
    public sealed class DialogueSession
    {
        private readonly DialogueGraph _graph;
        private readonly IDialogueConditionResolver _conditions;
        private readonly IDialogueActionExecutor _actions;

        private DialogueSession(DialogueGraph graph, IDialogueConditionResolver conditions, IDialogueActionExecutor actions)
        {
            _graph = graph;
            _conditions = conditions;
            _actions = actions;
        }

        public DialogueId DialogueId => _graph.Id;

        public NpcId NpcId => _graph.NpcId;

        public DialogueNode Current { get; private set; }

        public bool IsFinished => Current == null;

        public IReadOnlyList<DialogueChoice> AvailableChoices =>
            Current == null
                ? Empty<DialogueChoice>.List
                : Current.Choices.Where(c => c.Conditions.All(_conditions.IsSatisfied)).ToList();

        public static Result<DialogueSession> Start(
            DialogueGraph graph,
            IDialogueConditionResolver conditions,
            IDialogueActionExecutor actions)
        {
            var session = new DialogueSession(graph, conditions, actions);
            var entered = session.Enter(graph.StartNodeId);

            return entered.IsSuccess
                ? Result<DialogueSession>.Success(session)
                : Result<DialogueSession>.FromFailure(entered);
        }

        /// <summary>Moves on. Pass the index into <see cref="AvailableChoices"/> when the node offers choices.</summary>
        public Result Advance(int choiceIndex = -1)
        {
            if (IsFinished)
                return Result.Failure(DialogueErrors.Finished, "The dialogue has ended.");

            var choices = AvailableChoices;

            if (choices.Count == 0)
                return Enter(Current.NextNodeId);

            if (choiceIndex < 0 || choiceIndex >= choices.Count)
                return Result.Failure(DialogueErrors.InvalidChoice, "Pick one of the available choices.");

            var choice = choices[choiceIndex];

            foreach (var action in choice.Actions)
            {
                var executed = _actions.Execute(action);
                if (executed.IsFailure)
                    return executed;
            }

            return Enter(choice.TargetNodeId);
        }

        private Result Enter(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                Current = null;
                return Result.Success();
            }

            if (!_graph.TryGetNode(nodeId, out var node))
                return Result.Failure(DialogueErrors.UnknownNode, $"Dialogue '{_graph.Id}' has no node '{nodeId}'.");

            Current = node;

            foreach (var action in node.Actions)
            {
                var executed = _actions.Execute(action);
                if (executed.IsFailure)
                    return executed;
            }

            return Result.Success();
        }
    }
}
