using System.Collections.Generic;
using CleanTime.Save.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Save.Application
{
    /// <summary>
    /// Register this module after every module that owns saved state, and initialise it last, so
    /// autosave checkpoints observe fully-applied changes.
    /// </summary>
    public sealed class SaveModule : FeatureModule
    {
        public override string Name => "Save";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton(r => new SaveDirtyTracker(r.Resolve<IEventBus>()));

            services.RegisterSingleton(r => new SaveCoordinator(
                r.Resolve<IUnitOfWork>(),
                r.Resolve<ISaveSlotRepository>(),
                r.ResolveAll<ISaveParticipant>(),
                r.Resolve<SaveDirtyTracker>(),
                r.Resolve<IUtcClock>(),
                r.Resolve<IEventBus>(),
                r.Resolve<ILogger>()));
            services.Forward<ISaveCoordinator, SaveCoordinator>();

            services.RegisterSingleton(r => new AutosaveService(
                r.Resolve<IEventBus>(), r.Resolve<ISaveCoordinator>(), r.Resolve<SaveDirtyTracker>(), r.Resolve<ILogger>()));
            services.RegisterSingleton<IAutosaveTrigger>(r => new AutosaveTrigger(r.Resolve<AutosaveService>()));

            services.RegisterSingleton<ICommandHandler<CreateSaveSlotCommand>>(r => new CreateSaveSlotHandler(r.Resolve<ISaveCoordinator>()));
            services.RegisterSingleton<ICommandHandler<SaveGameCommand>>(r => new SaveGameHandler(r.Resolve<ISaveCoordinator>()));
            services.RegisterSingleton<ICommandHandler<LoadSaveCommand>>(r => new LoadSaveHandler(r.Resolve<ISaveCoordinator>()));
            services.RegisterSingleton<ICommandHandler<DeleteSaveSlotCommand>>(
                r => new DeleteSaveSlotHandler(r.Resolve<ISaveSlotRepository>(), r.Resolve<ISaveCoordinator>()));
            services.RegisterSingleton<ICommandHandler<RenameSaveSlotCommand>>(r => new RenameSaveSlotHandler(r.Resolve<ISaveSlotRepository>()));
            services.RegisterSingleton<IQueryHandler<GetSaveSlotsQuery, IReadOnlyList<SaveSlotSummary>>>(
                r => new GetSaveSlotsHandler(r.Resolve<ISaveSlotRepository>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            services.Resolve<SaveDirtyTracker>();
            services.Resolve<AutosaveService>();
        }
    }

    /// <summary>Lets presentation (focus lost, quit, scene transition) request a checkpoint save.</summary>
    public interface IAutosaveTrigger
    {
        Result SaveIfDirty();
    }

    internal sealed class AutosaveTrigger : IAutosaveTrigger
    {
        private readonly AutosaveService _service;

        public AutosaveTrigger(AutosaveService service)
        {
            _service = service;
        }

        public Result SaveIfDirty() => _service.SaveIfDirty();
    }
}
