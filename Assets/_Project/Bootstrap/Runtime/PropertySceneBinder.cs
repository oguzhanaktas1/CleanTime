using UnityEngine;
using CleanTime.Cleaning.Application;
using CleanTime.Cleaning.Presentation;
using CleanTime.Jobs.Application;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;
using KernelLogger = CleanTime.Shared.Kernel.ILogger;

namespace CleanTime.Bootstrap
{
    /// <summary>
    /// Put in every property scene next to its <see cref="CleaningSceneRoot"/>. When the scene is
    /// loaded it starts the cleaning session for the active job (or free-roam in test scenes).
    /// Lives here because it is the one place allowed to know both Jobs and Cleaning.
    /// </summary>
    public sealed class PropertySceneBinder : MonoBehaviour, ISceneService
    {
        [SerializeField] private CleaningSceneRoot cleaningRoot;

        public void Construct(IServiceResolver services)
        {
            var logger = services.Resolve<KernelLogger>();

            if (cleaningRoot == null)
                cleaningRoot = FindAnyObjectByType<CleaningSceneRoot>();

            if (cleaningRoot == null)
            {
                logger.Warning($"'{name}' has no CleaningSceneRoot; no cleaning session started.");
                return;
            }

            var seeds = cleaningRoot.BuildSeeds(out var problems);
            if (problems.Length > 0)
                logger.Warning($"Cleaning targets skipped in '{name}': {problems}");

            var location = cleaningRoot.LocationId;
            var jobId = default(JobId);

            var active = services.Resolve<IQueryHandler<GetActiveJobQuery, Result<Job>>>().Handle(new GetActiveJobQuery());
            if (active.IsSuccess && active.Value.State == JobState.InProgress)
            {
                location = active.Value.Definition.LocationId;
                jobId = active.Value.Id;
            }

            var started = services.Resolve<ICommandHandler<StartCleaningSessionCommand>>()
                .Handle(new StartCleaningSessionCommand(location, jobId, seeds));

            if (started.IsFailure)
                logger.Error($"Could not start the cleaning session in '{name}': {started.Error}");
        }
    }
}
