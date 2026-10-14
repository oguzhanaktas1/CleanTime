using System.Collections.Generic;
using System.Data;
using System.Linq;
using CleanTime.Save.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Save.Application
{
    /// <summary>Applies migrations in order until the target schema version is reached.</summary>
    public sealed class SaveMigrationRunner
    {
        private readonly IReadOnlyList<ISaveMigration> _migrations;

        public SaveMigrationRunner(IReadOnlyList<ISaveMigration> migrations)
        {
            _migrations = migrations;
        }

        public Result Upgrade(
            IDbConnection connection,
            int currentVersion,
            int targetVersion)
        {
            int version = currentVersion;

            while (version < targetVersion)
            {
                var migration = _migrations.SingleOrDefault(
                    x => x.FromVersion == version);

                if (migration is null)
                    return Result.Failure(
                        SaveErrors.MigrationMissing,
                        $"Missing migration from version {version}.");

                var result = migration.Migrate(connection);

                if (!result.IsSuccess)
                    return result;

                version = migration.ToVersion;
            }

            return Result.Success();
        }
    }
}
