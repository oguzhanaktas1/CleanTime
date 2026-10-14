using CleanTime.Dialogue.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Dialogue.Application
{
    /// <summary>Public entry point used by interaction / UI code to run conversations.</summary>
    public interface IDialogueService
    {
        Result<DialogueSession> Start(DialogueId id);

        /// <summary>Call when the session finished (or was cancelled) so listeners such as quests are notified.</summary>
        void End(DialogueSession session);
    }

    internal sealed class DialogueService : IDialogueService
    {
        private readonly IDialogueDefinitionProvider _definitions;
        private readonly IDialogueConditionResolver _conditions;
        private readonly IDialogueActionExecutor _actions;
        private readonly IDialogueRepository _repository;
        private readonly IEventBus _events;

        public DialogueService(
            IDialogueDefinitionProvider definitions,
            IDialogueConditionResolver conditions,
            IDialogueActionExecutor actions,
            IDialogueRepository repository,
            IEventBus events)
        {
            _definitions = definitions;
            _conditions = conditions;
            _actions = actions;
            _repository = repository;
            _events = events;
        }

        public Result<DialogueSession> Start(DialogueId id)
        {
            var graph = _definitions.Get(id);
            if (graph.IsFailure)
                return Result<DialogueSession>.Failure(DialogueErrors.UnknownDialogue, $"Unknown dialogue '{id}'.");

            var session = DialogueSession.Start(graph.Value, _conditions, _actions);
            if (session.IsSuccess)
                _events.Publish(new DialogueStartedEvent(id, graph.Value.NpcId));

            return session;
        }

        public void End(DialogueSession session)
        {
            _repository.MarkSeen(session.DialogueId);
            _events.Publish(new DialogueEndedEvent(session.DialogueId, session.NpcId));
        }
    }

    /// <summary>Default until modules contribute real resolvers: conditions fail closed, actions are rejected.</summary>
    internal sealed class FailClosedDialogueConditionResolver : IDialogueConditionResolver
    {
        public bool IsSatisfied(DialogueCondition condition) => false;
    }

    internal sealed class NoOpDialogueActionExecutor : IDialogueActionExecutor
    {
        public Result Execute(DialogueAction action) =>
            Result.Failure("dialogue.action_not_supported", $"No executor registered for {action.Type}.");
    }

    public sealed class DialogueModule : FeatureModule
    {
        public override string Name => "Dialogue";

        public override void Register(IServiceRegistry services)
        {
            // Defaults; a module registered later overrides them (the last registration wins).
            services.RegisterSingleton<IDialogueConditionResolver>(_ => new FailClosedDialogueConditionResolver());
            services.RegisterSingleton<IDialogueActionExecutor>(_ => new NoOpDialogueActionExecutor());

            services.RegisterSingleton<IDialogueService>(r => new DialogueService(
                r.Resolve<IDialogueDefinitionProvider>(),
                r.Resolve<IDialogueConditionResolver>(),
                r.Resolve<IDialogueActionExecutor>(),
                r.Resolve<IDialogueRepository>(),
                r.Resolve<IEventBus>()));
        }
    }
}
