using CleanTime.GameTime.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;
using System;

namespace CleanTime.GameTime.Application
{
    internal sealed class ConsumeGameActionHandler : ICommandHandler<ConsumeGameActionCommand>
    {
        private readonly IGameTimeRepository _time;
        private readonly IGameActionPolicy _actions;
        private readonly IDayPhasePolicy _phases;
        private readonly GameDayService _days;
        private readonly IEventBus _events;

        public ConsumeGameActionHandler(
            IGameTimeRepository time,
            IGameActionPolicy actions,
            IDayPhasePolicy phases,
            GameDayService days,
            IEventBus events)
        {
            _time = time;
            _actions = actions;
            _phases = phases;
            _days = days;
            _events = events;
        }

        public Result Handle(ConsumeGameActionCommand command)
        {
            var cost = _actions.GetCost(command.Kind);
            if (cost <= 0)
                return Result.Success();

            var calendar = _time.Get();
            var advance = calendar.ConsumeActions(cost, _phases);
            _time.Save(calendar);

            _events.Publish(new ActionConsumedEvent(command.Kind, cost, calendar.Now));

            foreach (var change in advance.PhaseChanges)
                _events.Publish(new DayPhaseChangedEvent(change.From, change.To, calendar.Now.Day));

            if (advance.DayEnded)
                _days.EndCurrentDay();

            return Result.Success();
        }
    }

    internal sealed class EndGameDayHandler : ICommandHandler<EndGameDayCommand>
    {
        private readonly GameDayService _days;

        public EndGameDayHandler(GameDayService days)
        {
            _days = days;
        }

        public Result Handle(EndGameDayCommand command)
        {
            _days.EndCurrentDay();
            return Result.Success();
        }
    }

    internal sealed class GetCurrentGameDateHandler : IQueryHandler<GetCurrentGameDateQuery, GameDate>
    {
        private readonly IGameTimeRepository _time;

        public GetCurrentGameDateHandler(IGameTimeRepository time)
        {
            _time = time;
        }

        public GameDate Handle(GetCurrentGameDateQuery query) => _time.Get().Now;
    }

    /// <summary>Each completed job consumes game time (day-end flow, architecture §138).</summary>
    internal sealed class JobTimeProjection : IDisposable
    {
        private readonly IDisposable _subscription;

        public JobTimeProjection(IEventBus events, ICommandHandler<ConsumeGameActionCommand> consume)
        {
            _subscription = events.Subscribe<JobCompletedEvent>(
                _ => consume.Handle(new ConsumeGameActionCommand(GameActionKind.JobCompleted)));
        }

        public void Dispose() => _subscription.Dispose();
    }
}
