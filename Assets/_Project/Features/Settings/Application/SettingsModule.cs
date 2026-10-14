using CleanTime.Settings.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Settings.Application
{
    public sealed record UpdateSettingsCommand(GameSettings Settings) : ICommand;

    public sealed record GetSettingsQuery : IQuery<GameSettings>;

    internal sealed class SettingsStore
    {
        private readonly IGameSettingsRepository _repository;

        public SettingsStore(IGameSettingsRepository repository)
        {
            _repository = repository;
            Current = repository.Load();
        }

        public GameSettings Current { get; private set; }

        public Result Update(GameSettings settings)
        {
            var saved = _repository.Save(settings);
            if (saved.IsSuccess)
                Current = settings;

            return saved;
        }
    }

    internal sealed class UpdateSettingsHandler : ICommandHandler<UpdateSettingsCommand>
    {
        private readonly SettingsStore _store;
        private readonly IEventBus _events;

        public UpdateSettingsHandler(SettingsStore store, IEventBus events)
        {
            _store = store;
            _events = events;
        }

        public Result Handle(UpdateSettingsCommand command)
        {
            if (command.Settings == null)
                return Result.Failure("settings.invalid", "Settings are required.");

            var saved = _store.Update(command.Settings);
            if (saved.IsSuccess)
                _events.Publish(new SettingsChangedEvent(command.Settings));

            return saved;
        }
    }

    internal sealed class GetSettingsHandler : IQueryHandler<GetSettingsQuery, GameSettings>
    {
        private readonly SettingsStore _store;

        public GetSettingsHandler(SettingsStore store)
        {
            _store = store;
        }

        public GameSettings Handle(GetSettingsQuery query) => _store.Current;
    }

    public sealed class SettingsModule : FeatureModule
    {
        public override string Name => "Settings";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton(r => new SettingsStore(r.Resolve<IGameSettingsRepository>()));
            services.RegisterSingleton<ICommandHandler<UpdateSettingsCommand>>(
                r => new UpdateSettingsHandler(r.Resolve<SettingsStore>(), r.Resolve<IEventBus>()));
            services.RegisterSingleton<IQueryHandler<GetSettingsQuery, GameSettings>>(
                r => new GetSettingsHandler(r.Resolve<SettingsStore>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            // Announce the loaded settings so audio / graphics adapters can apply them at boot.
            var settings = services.Resolve<IQueryHandler<GetSettingsQuery, GameSettings>>().Handle(new GetSettingsQuery());
            services.Resolve<IEventBus>().Publish(new SettingsChangedEvent(settings));
        }
    }
}
