using System;
using System.Data;
using CleanTime.Save.Application;
using CleanTime.Save.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Save.Infrastructure
{
    /// <summary>Brings the save database up to the schema version this build expects.</summary>
    public sealed class SaveDatabaseInitializer
    {
        private readonly IDbConnection _connection;
        private readonly SaveMigrationRunner _runner;
        private readonly IEventBus _events;
        private readonly ILogger _logger;

        public SaveDatabaseInitializer(IDbConnection connection, SaveMigrationRunner runner, IEventBus events, ILogger logger)
        {
            _connection = connection;
            _runner = runner;
            _events = events;
            _logger = logger;
        }

        public Result Initialize(int targetVersion = SaveSchema.CurrentVersion)
        {
            foreach (var pragma in SqlStatements.Pragmas)
                _connection.Execute(pragma);

            var current = ReadVersion();

            if (current > targetVersion)
                return Result.Failure("save.schema_too_new", $"The save database is version {current}; this build understands up to {targetVersion}.");

            if (current == targetVersion)
                return Result.Success();

            _connection.Execute(SqlStatements.Begin);

            try
            {
                var upgraded = _runner.Upgrade(_connection, current, targetVersion);

                if (upgraded.IsFailure)
                {
                    _connection.Execute(SqlStatements.Rollback);
                    return upgraded;
                }

                _connection.Execute("DELETE FROM schema_version");
                _connection.Execute($"INSERT INTO schema_version (version) VALUES ({targetVersion})");
                _connection.Execute(SqlStatements.Commit);
            }
            catch (Exception ex)
            {
                _logger.Error("Save migration failed.", ex);
                _connection.Execute(SqlStatements.Rollback);
                return Result.Failure(SaveErrors.Failed, ex.Message);
            }

            _events.Publish(new SaveMigratedEvent(current, targetVersion));
            return Result.Success();
        }

        private int ReadVersion()
        {
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "SELECT MAX(version) FROM schema_version";
                    var value = command.ExecuteScalar();
                    return value == null || value is DBNull ? 0 : Convert.ToInt32(value);
                }
            }
            catch (Exception)
            {
                return 0; // table does not exist yet: a brand-new database
            }
        }
    }
}
