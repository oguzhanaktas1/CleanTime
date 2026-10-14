using System;
using CleanTime.GameTime.Domain;
using CleanTime.NPC.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.NPC.Application
{
    public sealed record UpdateNpcSchedulesCommand(DayPhase Phase) : ICommand;

    internal sealed class UpdateNpcSchedulesHandler : ICommandHandler<UpdateNpcSchedulesCommand>
    {
        private readonly INpcRepository _npcs;
        private readonly IEventBus _events;

        public UpdateNpcSchedulesHandler(INpcRepository npcs, IEventBus events)
        {
            _npcs = npcs;
            _events = events;
        }

        public Result Handle(UpdateNpcSchedulesCommand command)
        {
            foreach (var npc in _npcs.GetAll())
            {
                if (!npc.MoveToPhase(command.Phase))
                    continue;

                _npcs.Save(npc);
                _events.Publish(new NpcScheduleNodeChangedEvent(npc.Id, npc.CurrentNodeId));
            }

            return Result.Success();
        }
    }

    /// <summary>DayPhaseChanged / GameDayStarted -> every NPC follows its schedule.</summary>
    internal sealed class NpcScheduleProjection : IDisposable
    {
        private readonly IDisposable _phases;
        private readonly IDisposable _days;

        public NpcScheduleProjection(IEventBus events, ICommandHandler<UpdateNpcSchedulesCommand> update)
        {
            _phases = events.Subscribe<DayPhaseChangedEvent>(e => update.Handle(new UpdateNpcSchedulesCommand(e.Current)));
            _days = events.Subscribe<GameDayStartedEvent>(_ => update.Handle(new UpdateNpcSchedulesCommand(DayPhase.Morning)));
        }

        public void Dispose()
        {
            _phases.Dispose();
            _days.Dispose();
        }
    }

    public sealed class NpcModule : FeatureModule
    {
        public override string Name => "NPC";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<ICommandHandler<UpdateNpcSchedulesCommand>>(
                r => new UpdateNpcSchedulesHandler(r.Resolve<INpcRepository>(), r.Resolve<IEventBus>()));

            services.RegisterSingleton(r => new NpcScheduleProjection(
                r.Resolve<IEventBus>(), r.Resolve<ICommandHandler<UpdateNpcSchedulesCommand>>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            services.Resolve<NpcScheduleProjection>();
        }
    }
}
