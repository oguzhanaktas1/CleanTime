using CleanTime.GameTime.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.GameTime.Application
{
    /// <summary>
    /// Day-end flow (architecture §138): GameDayEnded -> subscribers resolve employees and revenue ->
    /// summary -> next day starts. Autosave listens to GameDayStarted, when state is consistent.
    /// </summary>
    internal sealed class GameDayService
    {
        private readonly IGameTimeRepository _time;
        private readonly DaySummaryProjection _summary;
        private readonly IEventBus _events;

        public GameDayService(IGameTimeRepository time, DaySummaryProjection summary, IEventBus events)
        {
            _time = time;
            _summary = summary;
            _events = events;
        }

        public void EndCurrentDay()
        {
            var calendar = _time.Get();
            var day = calendar.Now.Day;

            _events.Publish(new GameDayEndedEvent(day));
            _events.Publish(new DaySummaryCreatedEvent(_summary.BuildAndReset(day)));

            calendar.StartNextDay();
            _time.Save(calendar);
            _events.Publish(new GameDayStartedEvent(calendar.Now.Day));
        }
    }

    internal sealed class GameClockAdapter : IGameClock
    {
        private readonly IGameTimeRepository _time;

        public GameClockAdapter(IGameTimeRepository time)
        {
            _time = time;
        }

        public GameDate Now => _time.Get().Now;
    }
}
