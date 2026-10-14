using CleanTime.Relationships.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Relationships.Application
{
    public sealed class RelationshipsModule : FeatureModule
    {
        private readonly RelationshipThresholdSet _thresholds;

        public RelationshipsModule(RelationshipThresholdSet thresholds = null)
        {
            _thresholds = thresholds ?? RelationshipThresholdSet.Default;
        }

        public override string Name => "Relationships";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<IRelationshipLevelPolicy>(_ => new ThresholdRelationshipLevelPolicy(_thresholds));

            services.RegisterSingleton<ICommandHandler<ApplyRelationshipChangeCommand>>(r => new ApplyRelationshipChangeHandler(
                r.Resolve<IRelationshipRepository>(),
                r.Resolve<IRelationshipLevelPolicy>(),
                r.Resolve<IGameClock>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<IQueryHandler<GetRelationshipQuery, Result<Relationship>>>(
                r => new GetRelationshipHandler(r.Resolve<IRelationshipRepository>()));

            services.RegisterSingleton(r => new GiftRelationshipProjection(
                r.Resolve<IEventBus>(),
                r.Resolve<ICommandHandler<ApplyRelationshipChangeCommand>>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            services.Resolve<GiftRelationshipProjection>();
        }
    }
}
