using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.NPC.Domain
{
    public interface INpcDefinitionProvider : IDefinitionProvider<NpcId, NpcProfile>
    {
    }

    public interface INpcScheduleProvider
    {
        NpcSchedule GetSchedule(NpcId npcId);
    }

    public interface INpcRepository
    {
        Result<Npc> Get(NpcId id);

        IReadOnlyList<Npc> GetAll();

        Result Save(Npc npc);
    }
}
