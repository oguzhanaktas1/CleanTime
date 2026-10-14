using System;
using System.Collections.Generic;
using System.Data;
using CleanTime.Save.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Save.Infrastructure
{
    /// <summary>Provider-agnostic ADO.NET helpers. The concrete SQLite provider is chosen in ADR-004.</summary>
    public static class SqlStatements
    {
        public const string Begin = "BEGIN";
        public const string Commit = "COMMIT";
        public const string Rollback = "ROLLBACK";

        /// <summary>Connection level settings. They cannot run inside a transaction, so apply them once after opening.</summary>
        public static readonly IReadOnlyList<string> Pragmas = new[]
        {
            "PRAGMA foreign_keys = ON;",
            "PRAGMA journal_mode = WAL;",
            "PRAGMA synchronous = NORMAL;",
        };

        public static void Execute(this IDbConnection connection, string sql)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }
    }

    /// <summary>
    /// Unit of work using explicit SQL transaction statements, so every command issued on the shared
    /// connection (repositories, migrations) participates without needing the IDbTransaction object.
    /// </summary>
    public sealed class AdoUnitOfWork : IUnitOfWork
    {
        private readonly IDbConnection _connection;
        private readonly ILogger _logger;

        public AdoUnitOfWork(IDbConnection connection, ILogger logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public Result ExecuteInTransaction(Func<Result> action)
        {
            _connection.Execute(SqlStatements.Begin);

            try
            {
                var result = action();

                _connection.Execute(result.IsSuccess ? SqlStatements.Commit : SqlStatements.Rollback);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error("Transaction failed; rolling back.", ex);
                TryRollback();
                throw;
            }
        }

        private void TryRollback()
        {
            try
            {
                _connection.Execute(SqlStatements.Rollback);
            }
            catch (Exception ex)
            {
                _logger.Error("Rollback failed.", ex);
            }
        }
    }

    /// <summary>Schema v1: the tables described in architecture §55-§71 (plus the version table).</summary>
    public sealed class SaveSchemaV1Migration : ISaveMigration
    {
        public int FromVersion => 0;

        public int ToVersion => 1;

        public Result Migrate(IDbConnection connection)
        {
            try
            {
                foreach (var statement in Statements)
                    connection.Execute(statement);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(SaveErrors.Failed, "Schema v1 migration failed: " + ex.Message);
            }
        }

        private const string Cascade = "FOREIGN KEY (save_id) REFERENCES save_slot(id) ON DELETE CASCADE";

        public static readonly IReadOnlyList<string> Statements = new[]
        {
            "CREATE TABLE schema_version (version INTEGER NOT NULL)",

            @"CREATE TABLE save_slot (
                id TEXT PRIMARY KEY,
                slot_number INTEGER NOT NULL UNIQUE,
                display_name TEXT NOT NULL,
                save_version INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL,
                play_time_seconds INTEGER NOT NULL DEFAULT 0,
                last_scene_key TEXT,
                is_corrupted INTEGER NOT NULL DEFAULT 0)",

            $@"CREATE TABLE player_state (
                save_id TEXT PRIMARY KEY,
                player_name TEXT NOT NULL,
                money_cents INTEGER NOT NULL DEFAULT 0,
                current_day INTEGER NOT NULL DEFAULT 1,
                current_day_phase TEXT NOT NULL DEFAULT 'Morning',
                company_unlocked INTEGER NOT NULL DEFAULT 0,
                total_jobs_completed INTEGER NOT NULL DEFAULT 0,
                story_level INTEGER NOT NULL DEFAULT 0,
                {Cascade})",

            $@"CREATE TABLE location_progress (
                save_id TEXT NOT NULL,
                location_id TEXT NOT NULL,
                unlocked INTEGER NOT NULL DEFAULT 0,
                completion_count INTEGER NOT NULL DEFAULT 0,
                best_score REAL,
                best_completion_seconds INTEGER,
                last_completed_day INTEGER,
                PRIMARY KEY (save_id, location_id),
                {Cascade})",
            "CREATE INDEX idx_location_progress_save ON location_progress(save_id)",

            $@"CREATE TABLE inventory_item (
                save_id TEXT NOT NULL,
                item_id TEXT NOT NULL,
                quantity INTEGER NOT NULL DEFAULT 0,
                acquired_at_day INTEGER,
                PRIMARY KEY (save_id, item_id),
                {Cascade})",

            $@"CREATE TABLE equipment_progress (
                save_id TEXT NOT NULL,
                equipment_id TEXT NOT NULL,
                owned INTEGER NOT NULL DEFAULT 0,
                upgrade_level INTEGER NOT NULL DEFAULT 0,
                equipped INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (save_id, equipment_id),
                {Cascade})",

            $@"CREATE TABLE job_history (
                id TEXT PRIMARY KEY,
                save_id TEXT NOT NULL,
                job_definition_id TEXT NOT NULL,
                location_id TEXT NOT NULL,
                customer_id TEXT,
                started_day INTEGER NOT NULL,
                completed_day INTEGER,
                status TEXT NOT NULL,
                score REAL,
                cleaning_progress REAL,
                reward_cents INTEGER,
                duration_seconds INTEGER,
                {Cascade})",
            "CREATE INDEX idx_job_history_save ON job_history(save_id)",
            "CREATE INDEX idx_job_history_definition ON job_history(save_id, job_definition_id)",

            $@"CREATE TABLE active_job (
                save_id TEXT PRIMARY KEY,
                job_instance_id TEXT NOT NULL,
                job_definition_id TEXT NOT NULL,
                location_id TEXT NOT NULL,
                started_day INTEGER NOT NULL,
                serialized_state TEXT NOT NULL,
                {Cascade})",

            $@"CREATE TABLE relationship (
                save_id TEXT NOT NULL,
                npc_id TEXT NOT NULL,
                points INTEGER NOT NULL DEFAULT 0,
                level INTEGER NOT NULL DEFAULT 0,
                last_changed_day INTEGER,
                PRIMARY KEY (save_id, npc_id),
                {Cascade})",

            $@"CREATE TABLE gift_history (
                id TEXT PRIMARY KEY,
                save_id TEXT NOT NULL,
                npc_id TEXT NOT NULL,
                gift_id TEXT NOT NULL,
                relationship_gain INTEGER NOT NULL,
                game_day INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL,
                {Cascade})",
            "CREATE INDEX idx_gift_history_save_npc ON gift_history(save_id, npc_id)",

            $@"CREATE TABLE quest_progress (
                save_id TEXT NOT NULL,
                quest_id TEXT NOT NULL,
                status TEXT NOT NULL,
                progress_json TEXT NOT NULL,
                activated_day INTEGER,
                completed_day INTEGER,
                PRIMARY KEY (save_id, quest_id),
                {Cascade})",

            $@"CREATE TABLE company (
                save_id TEXT PRIMARY KEY,
                company_name TEXT NOT NULL,
                level INTEGER NOT NULL DEFAULT 1,
                reputation INTEGER NOT NULL DEFAULT 0,
                lifetime_revenue_cents INTEGER NOT NULL DEFAULT 0,
                {Cascade})",

            $@"CREATE TABLE employee (
                id TEXT PRIMARY KEY,
                save_id TEXT NOT NULL,
                employee_definition_id TEXT NOT NULL,
                level INTEGER NOT NULL DEFAULT 1,
                cleaning_skill INTEGER NOT NULL DEFAULT 1,
                speed_skill INTEGER NOT NULL DEFAULT 1,
                reliability_skill INTEGER NOT NULL DEFAULT 1,
                status TEXT NOT NULL DEFAULT 'Idle',
                hired_day INTEGER NOT NULL,
                {Cascade})",
            "CREATE INDEX idx_employee_save ON employee(save_id)",

            $@"CREATE TABLE employee_assignment (
                id TEXT PRIMARY KEY,
                save_id TEXT NOT NULL,
                employee_id TEXT NOT NULL,
                job_definition_id TEXT NOT NULL,
                status TEXT NOT NULL,
                started_day INTEGER NOT NULL,
                finish_day INTEGER NOT NULL,
                deterministic_seed INTEGER NOT NULL,
                resolved_quality REAL,
                resolved_reward_cents INTEGER,
                {Cascade},
                FOREIGN KEY (employee_id) REFERENCES employee(id) ON DELETE CASCADE)",
            "CREATE INDEX idx_employee_assignment_status ON employee_assignment(save_id, status)",

            $@"CREATE TABLE company_upgrade (
                save_id TEXT NOT NULL,
                upgrade_id TEXT NOT NULL,
                level INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (save_id, upgrade_id),
                {Cascade})",

            $@"CREATE TABLE economy_transaction (
                id TEXT PRIMARY KEY,
                save_id TEXT NOT NULL,
                amount_cents INTEGER NOT NULL,
                balance_after_cents INTEGER NOT NULL,
                reason TEXT NOT NULL,
                reference_id TEXT,
                game_day INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL,
                {Cascade})",
            "CREATE INDEX idx_economy_transaction_save_time ON economy_transaction(save_id, created_at_utc)",

            "CREATE TABLE game_setting (key TEXT PRIMARY KEY, value TEXT NOT NULL)",

            $@"CREATE TABLE story_flag (
                save_id TEXT NOT NULL,
                flag_key TEXT NOT NULL,
                flag_value TEXT NOT NULL,
                PRIMARY KEY (save_id, flag_key),
                {Cascade})",
        };
    }

    /// <summary>Opens the (single) save database. Implemented by the platform SQLite adapter.</summary>
    public interface IDbConnectionFactory
    {
        IDbConnection Open();
    }
}
