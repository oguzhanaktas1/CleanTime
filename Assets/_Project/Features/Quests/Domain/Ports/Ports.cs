using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Quests.Domain
{
    public interface IQuestDefinitionProvider : IDefinitionProvider<QuestId, QuestDefinitionSnapshot>
    {
    }

    public interface IQuestRepository
    {
        Result<QuestInstance> Get(QuestId id);

        IReadOnlyList<QuestInstance> GetActive();

        IReadOnlyList<QuestInstance> GetCompleted();

        Result Save(QuestInstance quest);
    }
}
