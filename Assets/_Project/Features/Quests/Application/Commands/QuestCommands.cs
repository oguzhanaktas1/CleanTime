using System.Collections.Generic;
using CleanTime.Quests.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Quests.Application
{
    public sealed record ActivateQuestCommand(QuestId QuestId) : ICommand;

    public sealed record ClaimQuestRewardCommand(QuestId QuestId) : ICommand;

    public sealed record GetActiveQuestsQuery : IQuery<IReadOnlyList<QuestInstance>>;

    public sealed record GetCompletedQuestsQuery : IQuery<IReadOnlyList<QuestInstance>>;
}
