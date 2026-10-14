using System.Collections.Generic;
using CleanTime.Economy.Application;
using CleanTime.Economy.Domain;
using CleanTime.Quests.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Quests.Application
{
    internal sealed class ActivateQuestHandler : ICommandHandler<ActivateQuestCommand>
    {
        private readonly IQuestDefinitionProvider _definitions;
        private readonly IQuestRepository _quests;
        private readonly IEventBus _events;

        public ActivateQuestHandler(IQuestDefinitionProvider definitions, IQuestRepository quests, IEventBus events)
        {
            _definitions = definitions;
            _quests = quests;
            _events = events;
        }

        public Result Handle(ActivateQuestCommand command)
        {
            var definition = _definitions.Get(command.QuestId);
            if (definition.IsFailure)
                return Result.Failure(QuestErrors.UnknownQuest, $"Unknown quest '{command.QuestId}'.");

            if (_quests.Get(command.QuestId).IsSuccess)
                return Result.Failure(QuestErrors.AlreadyActive, $"Quest '{command.QuestId}' was already activated.");

            _quests.Save(new QuestInstance(definition.Value));
            _events.Publish(new QuestActivatedEvent(command.QuestId));
            return Result.Success();
        }
    }

    internal sealed class ClaimQuestRewardHandler : ICommandHandler<ClaimQuestRewardCommand>
    {
        private readonly IQuestRepository _quests;
        private readonly IWalletService _wallet;
        private readonly IEventBus _events;

        public ClaimQuestRewardHandler(IQuestRepository quests, IWalletService wallet, IEventBus events)
        {
            _quests = quests;
            _wallet = wallet;
            _events = events;
        }

        public Result Handle(ClaimQuestRewardCommand command)
        {
            var found = _quests.Get(command.QuestId);
            if (found.IsFailure)
                return Result.Failure(QuestErrors.NotActive, $"Quest '{command.QuestId}' is not known.");

            var quest = found.Value;
            var claimed = quest.Claim();
            if (claimed.IsFailure)
                return claimed;

            var reward = quest.Definition.Reward;

            if (reward.Money.Cents > 0)
            {
                var credit = _wallet.Credit(reward.Money, TransactionReason.QuestReward, command.QuestId.Value);
                if (credit.IsFailure)
                    return credit;
            }

            _quests.Save(quest);
            _events.Publish(new QuestRewardGrantedEvent(command.QuestId, reward.Money, reward.UnlockKey));
            return Result.Success();
        }
    }

    internal sealed class GetActiveQuestsHandler : IQueryHandler<GetActiveQuestsQuery, IReadOnlyList<QuestInstance>>
    {
        private readonly IQuestRepository _quests;

        public GetActiveQuestsHandler(IQuestRepository quests)
        {
            _quests = quests;
        }

        public IReadOnlyList<QuestInstance> Handle(GetActiveQuestsQuery query) => _quests.GetActive();
    }

    internal sealed class GetCompletedQuestsHandler : IQueryHandler<GetCompletedQuestsQuery, IReadOnlyList<QuestInstance>>
    {
        private readonly IQuestRepository _quests;

        public GetCompletedQuestsHandler(IQuestRepository quests)
        {
            _quests = quests;
        }

        public IReadOnlyList<QuestInstance> Handle(GetCompletedQuestsQuery query) => _quests.GetCompleted();
    }
}
