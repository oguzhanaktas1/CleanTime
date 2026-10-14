using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CleanTime.Content;
using CleanTime.Shared.Kernel;
using KernelLogger = CleanTime.Shared.Kernel.ILogger;

namespace CleanTime.Bootstrap
{
    public enum BootStage
    {
        InitializeLogging,
        InitializePlatform,
        InitializePersistence,
        RunMigrations,
        LoadSettings,
        LoadCoreContent,
        InitializeServices,
        LoadMainMenu,
        Completed,
        Failed,
    }

    /// <summary>Mutable bag shared by the boot steps.</summary>
    public sealed class BootContext
    {
        public BootContext(BootstrapSettings settings, Transform persistentRoot)
        {
            Settings = settings;
            PersistentRoot = persistentRoot;
            Services = new ServiceContainer();
        }

        public BootstrapSettings Settings { get; }

        public Transform PersistentRoot { get; }

        public ServiceContainer Services { get; }

        public KernelLogger Logger { get; set; } = NullLogger.Instance;

        public ContentRegistry Content { get; set; }

        public string SettingsFilePath { get; set; }
    }

    public interface IBootStep
    {
        BootStage Stage { get; }

        Task<Result> RunAsync(BootContext context);
    }

    /// <summary>
    /// Runs the boot steps in order. The first failing step stops the sequence and parks the machine in
    /// <see cref="BootStage.Failed"/> so a recoverable error UI can be shown; <see cref="RunAsync"/> can be called again to retry.
    /// </summary>
    public sealed class BootStateMachine
    {
        private readonly IReadOnlyList<IBootStep> _steps;

        public BootStateMachine(IReadOnlyList<IBootStep> steps)
        {
            _steps = steps;
        }

        public BootStage Current { get; private set; } = BootStage.InitializeLogging;

        public Error? FailureReason { get; private set; }

        public BootStage FailedStage { get; private set; }

        public event Action<BootStage> StageChanged;

        public async Task<Result> RunAsync(BootContext context)
        {
            FailureReason = null;

            foreach (var step in _steps)
            {
                Enter(step.Stage);
                Result result;

                try
                {
                    result = await step.RunAsync(context);
                }
                catch (Exception ex)
                {
                    context.Logger.Error($"Boot step {step.Stage} threw.", ex);
                    result = Result.Failure("boot.exception", ex.Message);
                }

                if (result.IsFailure)
                {
                    FailedStage = step.Stage;
                    FailureReason = result.Error;
                    Enter(BootStage.Failed);
                    return result;
                }
            }

            Enter(BootStage.Completed);
            return Result.Success();
        }

        private void Enter(BootStage stage)
        {
            Current = stage;
            StageChanged?.Invoke(stage);
        }
    }
}
