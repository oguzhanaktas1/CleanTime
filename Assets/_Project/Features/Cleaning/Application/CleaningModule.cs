using CleanTime.Cleaning.Domain;
using CleanTime.Equipment.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Application
{
    public sealed class CleaningModule : FeatureModule
    {
        public override string Name => "Cleaning";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<ICleaningProgressCalculator>(_ => new CleaningProgressCalculator());
            services.RegisterSingleton<IEquipmentCompatibilityPolicy>(_ => new EquipmentCompatibilityPolicy());

            services.RegisterSingleton<ICommandHandler<StartCleaningSessionCommand>>(r => new StartCleaningSessionHandler(
                r.Resolve<ICleaningTargetRepository>(),
                r.Resolve<ICleaningSessionRepository>(),
                r.Resolve<ICleaningProgressCalculator>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<ICommandHandler<StartCleaningTargetCommand>>(r => new StartCleaningTargetHandler(
                r.Resolve<ICleaningTargetRepository>(),
                r.Resolve<IEquipmentRepository>(),
                r.Resolve<IEquipmentCompatibilityPolicy>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<ICommandHandler<ApplyCleaningActionCommand>>(r => new ApplyCleaningActionHandler(
                r.Resolve<ICleaningTargetRepository>(),
                r.Resolve<IEquipmentRepository>(),
                r.Resolve<IEquipmentCompatibilityPolicy>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<ICommandHandler<CancelCleaningCommand>>(r => new CancelCleaningHandler(
                r.Resolve<ICleaningTargetRepository>(),
                r.Resolve<ICleaningSessionRepository>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<IQueryHandler<GetPropertyCleaningProgressQuery, float>>(r => new GetPropertyCleaningProgressHandler(
                r.Resolve<ICleaningTargetRepository>(),
                r.Resolve<ICleaningSessionRepository>(),
                r.Resolve<ICleaningProgressCalculator>()));

            services.RegisterSingleton(r => new CleaningFacade(
                r.Resolve<IQueryHandler<GetPropertyCleaningProgressQuery, float>>(),
                r.Resolve<ICommandHandler<ApplyCleaningActionCommand>>()));
            services.Forward<ICleaningReadService, CleaningFacade>();
            services.Forward<ICleaningCommandService, CleaningFacade>();

            services.RegisterSingleton(r => new CleaningCompletionCoordinator(
                r.Resolve<ICleaningTargetRepository>(),
                r.Resolve<ICleaningSessionRepository>(),
                r.Resolve<ICleaningProgressCalculator>(),
                r.Resolve<IEventBus>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            services.Resolve<CleaningCompletionCoordinator>();
        }
    }
}
