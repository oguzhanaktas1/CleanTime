using System;
using System.Threading.Tasks;
using UnityEngine;
using CleanTime.Shared.Kernel;

namespace CleanTime.Bootstrap
{
    /// <summary>
    /// Entry point of the game, placed in the Bootstrap scene. Runs the boot state machine
    /// (logging -> platform -> persistence -> migrations -> settings -> content -> services -> main menu)
    /// and shows a recoverable error screen when a critical step fails.
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private BootstrapSettings settings = new BootstrapSettings();

        private BootStateMachine _boot;
        private BootErrorPresenter _errorPresenter;

        public BootStage Stage => _boot?.Current ?? BootStage.InitializeLogging;

        private void Awake()
        {
            if (FindObjectsByType<GameBootstrapper>().Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            if (GetComponent<ProjectLifetimeScope>() == null)
                gameObject.AddComponent<ProjectLifetimeScope>();
        }

        private void Start()
        {
            _ = BootAsync();
        }

        private async Task BootAsync()
        {
            var context = new BootContext(settings, transform);

            _boot = new BootStateMachine(new IBootStep[]
            {
                new InitializeLoggingStep(),
                new InitializePlatformStep(),
                new InitializePersistenceStep(),
                new RunMigrationsStep(),
                new LoadSettingsStep(),
                new LoadCoreContentStep(),
                new InitializeServicesStep(),
                new LoadMainMenuStep(),
            });

            try
            {
                var result = await _boot.RunAsync(context);

                if (result.IsFailure)
                    ShowError(context, result);
            }
            catch (Exception ex)
            {
                context.Logger.Fatal("Boot crashed.", ex);
                ShowError(context, Result.Failure("boot.exception", ex.Message));
            }
        }

        private void ShowError(BootContext context, Result failure)
        {
            context.Logger.Error($"Boot failed at {_boot.FailedStage}: {failure.Error}");

            if (_errorPresenter == null)
                _errorPresenter = gameObject.AddComponent<BootErrorPresenter>();

            _errorPresenter.Show(_boot.FailedStage.ToString(), failure.Error.HasValue ? failure.Error.Value.Message : string.Empty, Retry);
        }

        private void Retry()
        {
            _errorPresenter.Hide();
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);
        }
    }
}
