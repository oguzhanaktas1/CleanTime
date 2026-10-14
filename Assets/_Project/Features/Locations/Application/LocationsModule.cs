using System.Collections.Generic;
using CleanTime.Locations.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Locations.Application
{
    public sealed class LocationsModule : FeatureModule
    {
        public override string Name => "Locations";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<ILocationUnlockPolicy>(_ => new CompletionCountUnlockPolicy());

            services.RegisterSingleton(r => new LocationUnlockEvaluator(r.ResolveAll<ILocationUnlockPolicy>()));

            services.RegisterSingleton<IQueryHandler<GetUnlockedLocationsQuery, IReadOnlyList<LocationDefinitionSnapshot>>>(
                r => new GetUnlockedLocationsHandler(
                    r.Resolve<ILocationDefinitionProvider>(),
                    r.Resolve<ILocationProgressRepository>()));

            services.RegisterSingleton<IQueryHandler<GetLocationProgressQuery, Result<LocationProgress>>>(
                r => new GetLocationProgressHandler(r.Resolve<ILocationProgressRepository>()));

            services.RegisterSingleton(r => new LocationCompletionProjection(
                r.Resolve<ILocationProgressRepository>(),
                r.Resolve<IEventBus>(),
                r.Resolve<ILogger>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            services.Resolve<LocationCompletionProjection>();
        }
    }
}
