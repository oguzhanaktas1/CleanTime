using System.Collections.Generic;
using System.Linq;
using CleanTime.NPC.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.NPC.Infrastructure
{
    public sealed class InMemoryNpcRepository : INpcRepository
    {
        private readonly INpcDefinitionProvider _definitions;
        private readonly INpcScheduleProvider _schedules;
        private Dictionary<NpcId, Npc> _npcs;

        public InMemoryNpcRepository(INpcDefinitionProvider definitions, INpcScheduleProvider schedules)
        {
            _definitions = definitions;
            _schedules = schedules;
        }

        public Result<Npc> Get(NpcId id)
        {
            EnsureSeeded();

            return _npcs.TryGetValue(id, out var npc)
                ? Result<Npc>.Success(npc)
                : Result<Npc>.Failure("npc.not_found", $"Unknown NPC '{id}'.");
        }

        public IReadOnlyList<Npc> GetAll()
        {
            EnsureSeeded();
            return _npcs.Values.ToList();
        }

        public Result Save(Npc npc)
        {
            EnsureSeeded();
            _npcs[npc.Id] = npc;
            return Result.Success();
        }

        private void EnsureSeeded()
        {
            if (_npcs != null)
                return;

            _npcs = _definitions.GetAll().ToDictionary(p => p.Id, p => new Npc(p, _schedules.GetSchedule(p.Id)));
        }
    }

    /// <summary>Everyone stays at "home" until schedules are authored.</summary>
    public sealed class StayAtHomeScheduleProvider : INpcScheduleProvider
    {
        public NpcSchedule GetSchedule(NpcId npcId) => new NpcSchedule(null, "home");
    }

    public sealed class NpcInfrastructureModule : FeatureModule
    {
        public override string Name => "NPC.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<INpcScheduleProvider>(_ => new StayAtHomeScheduleProvider());
            services.RegisterSingleton<INpcRepository>(r => new InMemoryNpcRepository(
                r.Resolve<INpcDefinitionProvider>(), r.Resolve<INpcScheduleProvider>()));
        }
    }
}
