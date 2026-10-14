using CleanTime.Save.Application;
using CleanTime.Save.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Save.Infrastructure
{
    /// <summary>
    /// In-memory persistence for now. Phase 3 swaps these registrations for the SQLite adapters
    /// (AdoUnitOfWork, SaveDatabaseInitializer, SQL repositories) without touching any gameplay code.
    /// </summary>
    public sealed class SaveInfrastructureModule : FeatureModule
    {
        public override string Name => "Save.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton(_ => new InMemorySnapshotStore());
            services.Forward<ISnapshotStore, InMemorySnapshotStore>();

            services.RegisterSingleton<IUnitOfWork>(r => new InMemoryUnitOfWork(r.Resolve<InMemorySnapshotStore>()));
            services.RegisterSingleton<ISaveSlotRepository>(
                r => new InMemorySaveSlotRepository(r.Resolve<InMemorySnapshotStore>(), r.Resolve<IUtcClock>()));

            services.RegisterSingleton(_ => new SaveMigrationRunner(new ISaveMigration[] { new SaveSchemaV1Migration() }));
        }
    }
}
