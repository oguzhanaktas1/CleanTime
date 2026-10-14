using CleanTime.GameTime.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.GameTime.Application
{
    public sealed class GameTimeModule : FeatureModule
    {
        private readonly IDayPhasePolicy _phasePolicy;
        private readonly IGameActionPolicy _actionPolicy;

        public GameTimeModule(IDayPhasePolicy phasePolicy = null, IGameActionPolicy actionPolicy = null)
        {
            _phasePolicy = phasePolicy ?? new ActionsPerPhaseDayPhasePolicy();
            _actionPolicy = actionPolicy ?? new DefaultGameActionPolicy();
        }

        public override string Name => "GameTime";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterInstance(_phasePolicy);
            services.RegisterInstance(_actionPolicy);

            services.RegisterSingleton<IGameClock>(r => new GameClockAdapter(r.Resolve<IGameTimeRepository>()));
            services.RegisterSingleton(r => new DaySummaryProjection(r.Resolve<IEventBus>()));
            services.RegisterSingleton(r => new GameDayService(
                r.Resolve<IGameTimeRepository>(),
                r.Resolve<DaySummaryProjection>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<ICommandHandler<ConsumeGameActionCommand>>(r => new ConsumeGameActionHandler(
                r.Resolve<IGameTimeRepository>(),
                r.Resolve<IGameActionPolicy>(),
                r.Resolve<IDayPhasePolicy>(),
                r.Resolve<GameDayService>(),
                r.Resolve<IEventBus>()));
            services.RegisterSingleton<ICommandHandler<EndGameDayCommand>>(
                r => new EndGameDayHandler(r.Resolve<GameDayService>()));
            services.RegisterSingleton<IQueryHandler<GetCurrentGameDateQuery, GameDate>>(
                r => new GetCurrentGameDateHandler(r.Resolve<IGameTimeRepository>()));

            services.RegisterSingleton(r => new JobTimeProjection(
                r.Resolve<IEventBus>(),
                r.Resolve<ICommandHandler<ConsumeGameActionCommand>>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            services.Resolve<DaySummaryProjection>();
            services.Resolve<JobTimeProjection>();
        }
    }
}
