using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using CleanTime.Shared.Kernel;
using CleanTime.World.Application;
using KernelLogger = CleanTime.Shared.Kernel.ILogger;

namespace CleanTime.World.Infrastructure
{
    /// <summary>Names of the base scenes. Property scenes are resolved from their location's scene key.</summary>
    public sealed class SceneNames
    {
        public string MainMenu { get; set; } = "MainMenu";

        public string Town { get; set; } = "Town";
    }

    /// <summary>
    /// SceneManager based loader. Town and menu replace the gameplay scene; property scenes load additively
    /// next to the persistent bootstrap scene. Swap for an Addressables loader behind the same port later.
    /// </summary>
    public sealed class UnitySceneLoader : ISceneLoader
    {
        private readonly SceneNames _names;
        private readonly ILocationSceneResolver _resolver;
        private readonly KernelLogger _logger;
        private Scene _currentGameplayScene;

        public UnitySceneLoader(SceneNames names, ILocationSceneResolver resolver, KernelLogger logger)
        {
            _names = names;
            _resolver = resolver;
            _logger = logger;
        }

        public Task LoadMainMenuAsync() => LoadAdditiveAsync(_names.MainMenu);

        public Task LoadTownAsync() => LoadAdditiveAsync(_names.Town);

        public async Task LoadPropertyAsync(LocationId locationId)
        {
            var key = _resolver.ResolveSceneKey(locationId);

            if (key.IsFailure)
                throw new System.InvalidOperationException(key.Error.Value.Message);

            await LoadAdditiveAsync(key.Value);
        }

        public async Task UnloadCurrentGameplaySceneAsync()
        {
            if (!_currentGameplayScene.IsValid() || !_currentGameplayScene.isLoaded)
                return;

            var scene = _currentGameplayScene;
            _currentGameplayScene = default;
            await Await(SceneManager.UnloadSceneAsync(scene));
        }

        private async Task LoadAdditiveAsync(string sceneName)
        {
            await UnloadCurrentGameplaySceneAsync();

            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
                throw new System.InvalidOperationException($"Scene '{sceneName}' is not in the build settings.");

            await Await(operation);

            _currentGameplayScene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(_currentGameplayScene);
            _logger.Event("scene_loaded", ("scene", sceneName));
        }

        private static Task Await(AsyncOperation operation)
        {
            var source = new TaskCompletionSource<bool>();
            operation.completed += _ => source.TrySetResult(true);
            return source.Task;
        }
    }

    public sealed class WorldInfrastructureModule : FeatureModule
    {
        private readonly SceneNames _names;

        public WorldInfrastructureModule(SceneNames names = null)
        {
            _names = names ?? new SceneNames();
        }

        public override string Name => "World.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<ISceneLoader>(r => new UnitySceneLoader(
                _names, r.Resolve<ILocationSceneResolver>(), r.Resolve<KernelLogger>()));
        }
    }
}
