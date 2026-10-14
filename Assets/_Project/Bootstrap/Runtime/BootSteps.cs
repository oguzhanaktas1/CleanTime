using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using CleanTime.Content;
using CleanTime.Settings.Application;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;
using CleanTime.World.Application;
using KernelLogger = CleanTime.Shared.Kernel.ILogger;

namespace CleanTime.Bootstrap
{
    internal abstract class BootStepBase : IBootStep
    {
        public abstract BootStage Stage { get; }

        public Task<Result> RunAsync(BootContext context) => Task.FromResult(Run(context));

        protected abstract Result Run(BootContext context);
    }

    internal sealed class InitializeLoggingStep : BootStepBase
    {
        public override BootStage Stage => BootStage.InitializeLogging;

        protected override Result Run(BootContext context)
        {
            context.Logger = new UnityLogger(context.Settings.logLevel);
            context.Logger.Event("boot_started", ("version", UnityEngine.Application.version));
            return Result.Success();
        }
    }

    internal sealed class InitializePlatformStep : BootStepBase
    {
        public override BootStage Stage => BootStage.InitializePlatform;

        protected override Result Run(BootContext context)
        {
            context.Logger.Event(
                "platform",
                ("os", SystemInfo.operatingSystem),
                ("unity", UnityEngine.Application.unityVersion));
            return Result.Success();
        }
    }

    /// <summary>Verifies the player can actually write save data; if not, say so now instead of failing silently later.</summary>
    internal sealed class InitializePersistenceStep : BootStepBase
    {
        public override BootStage Stage => BootStage.InitializePersistence;

        protected override Result Run(BootContext context)
        {
            try
            {
                var directory = UnityEngine.Application.persistentDataPath;
                Directory.CreateDirectory(directory);

                var probe = Path.Combine(directory, ".write_test");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);

                context.SettingsFilePath = Path.Combine(directory, context.Settings.settingsFileName);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure("boot.storage_unavailable", "Save data cannot be written: " + ex.Message);
            }
        }
    }

    internal sealed class RunMigrationsStep : BootStepBase
    {
        public override BootStage Stage => BootStage.RunMigrations;

        protected override Result Run(BootContext context)
        {
            // Persistence is in-memory until the SQLite provider is chosen (ADR-004). When it lands,
            // this step opens the database and runs SaveDatabaseInitializer.
            context.Logger.Info("Save migrations: nothing to do (in-memory persistence).");
            return Result.Success();
        }
    }

    internal sealed class LoadSettingsStep : BootStepBase
    {
        public override BootStage Stage => BootStage.LoadSettings;

        protected override Result Run(BootContext context)
        {
            // The settings repository is created with the other services; this step only reports the target.
            context.Logger.Info("Settings file: " + (context.Settings.persistSettings ? context.SettingsFilePath : "(memory only)"));
            return Result.Success();
        }
    }

    internal sealed class LoadCoreContentStep : BootStepBase
    {
        public override BootStage Stage => BootStage.LoadCoreContent;

        protected override Result Run(BootContext context)
        {
            var catalog = context.Settings.contentCatalog;

            if (catalog == null)
            {
                context.Logger.Warning("No content catalog assigned; booting with empty content.");
                catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            }

            var built = ContentRegistry.Build(catalog);
            if (built.IsFailure)
                return Result.Failure("boot.invalid_content", built.Error.Value.Message);

            context.Content = built.Value;
            return Result.Success();
        }
    }

    /// <summary>Registers every module, then initialises them. Also publishes the project scope.</summary>
    internal sealed class InitializeServicesStep : BootStepBase
    {
        public override BootStage Stage => BootStage.InitializeServices;

        protected override Result Run(BootContext context)
        {
            var services = context.Services;
            var root = context.PersistentRoot;

            CoreServices.Register(services, context.Logger, new SystemUtcClock(), new SeededRandomSource(Environment.TickCount), new GuidIdGenerator());
            services.RegisterInstance<IPayloadSerializer>(new UnityJsonPayloadSerializer());

            var ticker = root.gameObject.GetComponent<UnityFrameTicker>() ?? root.gameObject.AddComponent<UnityFrameTicker>();
            services.RegisterInstance(ticker);

            var input = root.gameObject.GetComponent<CleanTime.GameInput.Presentation.UnityGameplayInput>()
                        ?? root.gameObject.AddComponent<CleanTime.GameInput.Presentation.UnityGameplayInput>();
            services.RegisterInstance<CleanTime.GameInput.Application.IGameplayInput>(input);

            var modules = FeatureRegistration.CreateModules(context.Settings, context.Content, context.SettingsFilePath, root);

            foreach (var module in modules)
                module.Register(services);

            foreach (var module in modules)
            {
                try
                {
                    module.Initialize(services);
                }
                catch (Exception ex)
                {
                    return Result.Failure("boot.module_failed", $"Module '{module.Name}' failed to initialise: {ex.Message}");
                }
            }

            ProjectLifetimeScope.Publish(services);
            context.Logger.Event("services_ready", ("modules", modules.Count));
            return Result.Success();
        }
    }

    internal sealed class LoadMainMenuStep : IBootStep
    {
        public BootStage Stage => BootStage.LoadMainMenu;

        public async Task<Result> RunAsync(BootContext context)
        {
            if (!context.Settings.loadMainMenuOnBoot)
                return Result.Success();

            await context.Services.Resolve<ISceneLoader>().LoadMainMenuAsync();
            return Result.Success();
        }
    }
}
