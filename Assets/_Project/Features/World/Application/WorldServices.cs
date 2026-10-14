using System;
using System.Threading.Tasks;
using CleanTime.Jobs.Domain;
using CleanTime.Locations.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.World.Application
{
    /// <summary>
    /// Scene loading port. The domain only knows a <see cref="LocationId"/>; resolving that to a scene
    /// (and loading it, via Addressables later) is an infrastructure concern.
    /// </summary>
    public interface ISceneLoader
    {
        Task LoadMainMenuAsync();

        Task LoadTownAsync();

        Task LoadPropertyAsync(LocationId locationId);

        Task UnloadCurrentGameplaySceneAsync();
    }

    /// <summary>Maps a business location id to a scene key (never a hard-coded scene name).</summary>
    public interface ILocationSceneResolver
    {
        Result<string> ResolveSceneKey(LocationId locationId);
    }

    internal sealed class DefinitionLocationSceneResolver : ILocationSceneResolver
    {
        private readonly ILocationDefinitionProvider _locations;

        public DefinitionLocationSceneResolver(ILocationDefinitionProvider locations)
        {
            _locations = locations;
        }

        public Result<string> ResolveSceneKey(LocationId locationId)
        {
            var definition = _locations.Get(locationId);

            if (definition.IsFailure)
                return Result<string>.Failure("world.unknown_location", $"Unknown location '{locationId}'.");

            return string.IsNullOrEmpty(definition.Value.SceneKey)
                ? Result<string>.Failure("world.no_scene", $"Location '{locationId}' has no scene key yet.")
                : Result<string>.Success(definition.Value.SceneKey);
        }
    }

    public enum GameFlowState
    {
        Boot,
        MainMenu,
        Loading,
        Town,
        Job,
        JobResult,
        Paused,
        Saving,
        Error,
    }

    /// <summary>High-level game flow (architecture §79): Boot -> MainMenu -> Loading -> Town -> Job -> JobResult -> Town.</summary>
    public sealed class GameFlowStateMachine
    {
        private readonly StateMachine<GameFlowState> _machine;

        public GameFlowStateMachine()
        {
            _machine = new StateMachine<GameFlowState>(GameFlowState.Boot)
                .Allow(GameFlowState.Boot, GameFlowState.MainMenu, GameFlowState.Loading, GameFlowState.Error)
                .Allow(GameFlowState.MainMenu, GameFlowState.Loading, GameFlowState.Error)
                .Allow(GameFlowState.Loading, GameFlowState.MainMenu, GameFlowState.Town, GameFlowState.Job, GameFlowState.Error)
                .Allow(GameFlowState.Town, GameFlowState.Loading, GameFlowState.MainMenu, GameFlowState.Paused, GameFlowState.Saving)
                .Allow(GameFlowState.Job, GameFlowState.JobResult, GameFlowState.Loading, GameFlowState.Paused, GameFlowState.Saving)
                .Allow(GameFlowState.JobResult, GameFlowState.Loading, GameFlowState.Town)
                .Allow(GameFlowState.Paused, GameFlowState.Town, GameFlowState.Job, GameFlowState.MainMenu)
                .Allow(GameFlowState.Saving, GameFlowState.Town, GameFlowState.Job)
                .Allow(GameFlowState.Error, GameFlowState.MainMenu, GameFlowState.Boot);
        }

        public GameFlowState Current => _machine.Current;

        public event Action<GameFlowState, GameFlowState> Changed
        {
            add => _machine.Changed += value;
            remove => _machine.Changed -= value;
        }

        public Result TransitionTo(GameFlowState next) => _machine.TransitionTo(next);
    }

    /// <summary>Drives scene transitions from job events so no UI or scene script owns the flow.</summary>
    internal sealed class GameFlowCoordinator : IDisposable
    {
        private readonly GameFlowStateMachine _flow;
        private readonly ISceneLoader _scenes;
        private readonly ILogger _logger;
        private readonly IDisposable[] _subscriptions;

        public GameFlowCoordinator(GameFlowStateMachine flow, ISceneLoader scenes, IEventBus events, ILogger logger)
        {
            _flow = flow;
            _scenes = scenes;
            _logger = logger;

            _subscriptions = new[]
            {
                events.Subscribe<JobStartedEvent>(e => Run(() => EnterPropertyAsync(e.LocationId))),
                events.Subscribe<JobCompletedEvent>(_ => _flow.TransitionTo(GameFlowState.JobResult)),
                events.Subscribe<JobResultAcknowledgedEvent>(_ => Run(ReturnToTownAsync)),
                events.Subscribe<JobFailedEvent>(_ => Run(ReturnToTownAsync)),
            };
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }

        private async Task EnterPropertyAsync(LocationId locationId)
        {
            _flow.TransitionTo(GameFlowState.Loading);
            await _scenes.UnloadCurrentGameplaySceneAsync();
            await _scenes.LoadPropertyAsync(locationId);
            _flow.TransitionTo(GameFlowState.Job);
        }

        private async Task ReturnToTownAsync()
        {
            _flow.TransitionTo(GameFlowState.Loading);
            await _scenes.UnloadCurrentGameplaySceneAsync();
            await _scenes.LoadTownAsync();
            _flow.TransitionTo(GameFlowState.Town);
        }

        // Event handlers are synchronous; scene work continues asynchronously and reports failures to the log.
        private async void Run(Func<Task> work)
        {
            try
            {
                await work();
            }
            catch (Exception ex)
            {
                _logger.Error("Scene transition failed.", ex);
                _flow.TransitionTo(GameFlowState.Error);
            }
        }
    }

    public sealed class WorldModule : FeatureModule
    {
        public override string Name => "World";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton(_ => new GameFlowStateMachine());
            services.RegisterSingleton<ILocationSceneResolver>(r => new DefinitionLocationSceneResolver(r.Resolve<ILocationDefinitionProvider>()));
            services.RegisterSingleton(r => new GameFlowCoordinator(
                r.Resolve<GameFlowStateMachine>(), r.Resolve<ISceneLoader>(), r.Resolve<IEventBus>(), r.Resolve<ILogger>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            services.Resolve<GameFlowCoordinator>();
        }
    }
}
