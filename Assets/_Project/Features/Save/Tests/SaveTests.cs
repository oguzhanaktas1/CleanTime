using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using CleanTime.Jobs.Domain;
using CleanTime.Save.Application;
using CleanTime.Save.Domain;
using CleanTime.Save.Infrastructure;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;

namespace CleanTime.Save.Tests
{
    internal sealed class ValueParticipant : ISaveParticipant
    {
        private readonly ISnapshotStore _store;

        public ValueParticipant(string key, ISnapshotStore store, int initial = 0)
        {
            Key = key;
            _store = store;
            Initial = initial;
            Value = initial;
        }

        public string Key { get; }
        public int Initial { get; }
        public int Value { get; set; }
        public bool FailOnFlush { get; set; }
        public bool Corrupt { get; set; }

        public Result Flush(SaveId saveId)
        {
            if (FailOnFlush)
                return Result.Failure(SaveErrors.Failed, "disk full");

            _store.Put(saveId, Key, Value.ToString());
            return Result.Success();
        }

        public Result Load(SaveId saveId)
        {
            if (!_store.TryGet(saveId, Key, out var payload))
                return Result.Failure(SaveErrors.MissingSection, "missing");

            if (Corrupt || !int.TryParse(payload, out var value))
                return Result.Failure(SaveErrors.InvalidSection, "bad");

            Value = value;
            return Result.Success();
        }

        public void Reset() => Value = Initial;
    }

    internal sealed class SaveFixture
    {
        public EventBus Bus { get; } = new EventBus();
        public InMemorySnapshotStore Store { get; } = new InMemorySnapshotStore();
        public FakeUtcClock Clock { get; } = new FakeUtcClock();
        public InMemorySaveSlotRepository Slots { get; }
        public ValueParticipant Wallet { get; }
        public ValueParticipant Progress { get; }
        public SaveDirtyTracker Dirty { get; }
        public SaveCoordinator Coordinator { get; }

        public SaveFixture()
        {
            Slots = new InMemorySaveSlotRepository(Store, Clock);
            Wallet = new ValueParticipant("wallet", Store, 100);
            Progress = new ValueParticipant("progress", Store, 0);
            Dirty = new SaveDirtyTracker(Bus);
            Coordinator = new SaveCoordinator(
                new InMemoryUnitOfWork(Store), Slots, new[] { Wallet, Progress }, Dirty, Clock, Bus, NullLogger.Instance);
        }
    }

    public sealed class SaveSlotTests
    {
        [Test]
        public void NewGame_CreatesASlotAndSavesIt()
        {
            var f = new SaveFixture();
            using var completed = f.Bus.Record<SaveCompletedEvent>();

            var result = f.Coordinator.StartNewGame("My game");

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(result.Value, f.Coordinator.CurrentSaveId);
            Assert.AreEqual(1, f.Slots.GetAll().Count);
            Assert.AreEqual(1, completed.Count);
        }

        [Test]
        public void SlotsGetTheLowestFreeNumber()
        {
            var f = new SaveFixture();
            var first = f.Slots.Create("a").Value;
            f.Slots.Create("b");
            f.Slots.Delete(first);

            f.Slots.Create("c");

            CollectionAssert.AreEqual(new[] { 1, 2 }, f.Slots.GetAll().Select(s => s.SlotNumber).ToArray());
        }

        [Test]
        public void NameIsRequired()
        {
            var f = new SaveFixture();

            Assert.AreEqual(SaveErrors.InvalidName, f.Coordinator.StartNewGame("  ").ErrorCode);
        }

        [Test]
        public void Rename_UpdatesTheSlot()
        {
            var f = new SaveFixture();
            var id = f.Slots.Create("old").Value;
            var rename = new RenameSaveSlotHandler(f.Slots);

            Assert.IsTrue(rename.Handle(new RenameSaveSlotCommand(id, " new ")).IsSuccess);
            Assert.AreEqual("new", f.Slots.Get(id).Value.DisplayName);
            Assert.AreEqual(SaveErrors.InvalidName, rename.Handle(new RenameSaveSlotCommand(id, "")).ErrorCode);
        }

        [Test]
        public void DeleteSave_ShouldRemoveItsData()
        {
            var f = new SaveFixture();
            var id = f.Coordinator.StartNewGame("temp").Value;
            f.Coordinator.StartNewGame("other");
            var delete = new DeleteSaveSlotHandler(f.Slots, f.Coordinator);

            Assert.IsTrue(delete.Handle(new DeleteSaveSlotCommand(id)).IsSuccess);

            Assert.IsFalse(f.Store.TryGet(id, "wallet", out _));
            Assert.IsTrue(f.Slots.Get(id).IsFailure);
        }

        [Test]
        public void TheActiveSlot_CannotBeDeleted()
        {
            var f = new SaveFixture();
            var id = f.Coordinator.StartNewGame("current").Value;

            Assert.IsTrue(new DeleteSaveSlotHandler(f.Slots, f.Coordinator).Handle(new DeleteSaveSlotCommand(id)).IsFailure);
        }
    }

    public sealed class SaveLoadTests
    {
        [Test]
        public void SaveLoadRoundTrip_RestoresEveryParticipant()
        {
            var f = new SaveFixture();
            var id = f.Coordinator.StartNewGame("slot").Value;
            f.Wallet.Value = 777;
            f.Progress.Value = 5;
            Assert.IsTrue(f.Coordinator.SaveCurrentGame().IsSuccess);

            f.Wallet.Value = 1;
            f.Progress.Value = 1;
            using var loaded = f.Bus.Record<SaveLoadedEvent>();

            Assert.IsTrue(f.Coordinator.LoadGame(id).IsSuccess);

            Assert.AreEqual(777, f.Wallet.Value);
            Assert.AreEqual(5, f.Progress.Value);
            Assert.AreEqual(1, loaded.Count);
        }

        [Test]
        public void Saving_WithoutAnActiveSlot_Fails()
        {
            var f = new SaveFixture();

            Assert.AreEqual(SaveErrors.NoActiveSave, f.Coordinator.SaveCurrentGame().ErrorCode);
        }

        [Test]
        public void FailedSave_ShouldRollbackTransaction()
        {
            var f = new SaveFixture();
            var id = f.Coordinator.StartNewGame("slot").Value;
            f.Wallet.Value = 500;
            f.Coordinator.SaveCurrentGame();

            f.Wallet.Value = 999;        // flushes fine...
            f.Progress.FailOnFlush = true; // ...but the second participant fails
            using var failed = f.Bus.Record<SaveFailedEvent>();

            var result = f.Coordinator.SaveCurrentGame();

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(1, failed.Count);
            Assert.IsTrue(f.Store.TryGet(id, "wallet", out var stored));
            Assert.AreEqual("500", stored); // the first participant's write was rolled back
        }

        [Test]
        public void ADirtyFlag_IsClearedOnlyBySuccessfulSaves()
        {
            var f = new SaveFixture();
            f.Coordinator.StartNewGame("slot");
            f.Dirty.MarkDirty();

            f.Progress.FailOnFlush = true;
            f.Coordinator.SaveCurrentGame();
            Assert.IsTrue(f.Dirty.IsDirty);

            f.Progress.FailOnFlush = false;
            f.Coordinator.SaveCurrentGame();
            Assert.IsFalse(f.Dirty.IsDirty);
        }

        [Test]
        public void SectionsMissingFromOlderSaves_FallBackToDefaults()
        {
            var f = new SaveFixture();
            var id = f.Coordinator.StartNewGame("slot").Value;
            f.Wallet.Value = 321;
            f.Coordinator.SaveCurrentGame();
            f.Store.RemoveSlot(id); // simulate a save written before 'wallet' / 'progress' existed
            f.Store.Put(id, "progress", "9");

            Assert.IsTrue(f.Coordinator.LoadGame(id).IsSuccess);

            Assert.AreEqual(100, f.Wallet.Value); // default
            Assert.AreEqual(9, f.Progress.Value);
        }

        [Test]
        public void CorruptedSections_AbortTheLoad_AndLeaveCleanState()
        {
            var f = new SaveFixture();
            var id = f.Coordinator.StartNewGame("slot").Value;
            f.Wallet.Value = 321;
            f.Coordinator.SaveCurrentGame();
            f.Progress.Corrupt = true;

            var result = f.Coordinator.LoadGame(id);

            Assert.AreEqual(SaveErrors.InvalidSection, result.ErrorCode);
            Assert.AreEqual(100, f.Wallet.Value);
            Assert.IsTrue(f.Coordinator.CurrentSaveId.IsEmpty);
        }

        [Test]
        public void CorruptedSlots_AreNotLoaded()
        {
            var f = new SaveFixture();
            var id = f.Slots.Create("broken").Value;
            var slot = f.Slots.Get(id).Value;
            slot.MarkCorrupted();
            f.Slots.Update(slot);

            Assert.AreEqual(SaveErrors.SlotCorrupted, f.Coordinator.LoadGame(id).ErrorCode);
        }

        [Test]
        public void LoadingAnUnknownSlot_Fails()
        {
            var f = new SaveFixture();

            Assert.AreEqual(SaveErrors.SlotNotFound, f.Coordinator.LoadGame(SaveId.New()).ErrorCode);
        }
    }

    public sealed class AutosaveTests
    {
        [Test]
        public void Checkpoints_SaveOnlyWhenSomethingChanged()
        {
            var f = new SaveFixture();
            f.Coordinator.StartNewGame("slot");
            using var autosave = new AutosaveService(f.Bus, f.Coordinator, f.Dirty, NullLogger.Instance);
            using var completed = f.Bus.Record<SaveCompletedEvent>();

            f.Bus.Publish(new JobCompletedEvent(new JobId("j"), new JobDefinitionId("d"), new LocationId("l"), Money.Zero, 1f,
                new CustomerEvaluation(1, 1, 1, 1, 1f), 1f, 1));
            Assert.AreEqual(1, completed.Count); // JobCompleted is both a change and a checkpoint

            f.Bus.Publish(new CleanTime.GameTime.Domain.GameDayStartedEvent(2));
            Assert.AreEqual(2, completed.Count);

            Assert.IsTrue(autosave.SaveIfDirty().IsSuccess); // nothing changed since: no extra save
            Assert.AreEqual(2, completed.Count);
        }

        [Test]
        public void Checkpoints_DoNothing_WithoutAnActiveSlot()
        {
            var f = new SaveFixture();
            using var autosave = new AutosaveService(f.Bus, f.Coordinator, f.Dirty, NullLogger.Instance);
            using var requested = f.Bus.Record<SaveRequestedEvent>();

            f.Dirty.MarkDirty();
            Assert.IsTrue(autosave.SaveIfDirty().IsSuccess);

            Assert.AreEqual(0, requested.Count);
        }
    }

    public sealed class MigrationTests
    {
        private sealed class StubMigration : ISaveMigration
        {
            private readonly List<int> _log;

            public StubMigration(int from, int to, List<int> log)
            {
                FromVersion = from;
                ToVersion = to;
                _log = log;
            }

            public int FromVersion { get; }
            public int ToVersion { get; }
            public bool Fail { get; set; }

            public Result Migrate(IDbConnection connection)
            {
                _log.Add(FromVersion);
                return Fail ? Result.Failure("boom", "failed") : Result.Success();
            }
        }

        [Test]
        public void Migration_ShouldUpgradeOldSchema_InOrder()
        {
            var log = new List<int>();
            var runner = new SaveMigrationRunner(new ISaveMigration[]
            {
                new StubMigration(2, 3, log), new StubMigration(1, 2, log), new StubMigration(3, 4, log),
            });

            Assert.IsTrue(runner.Upgrade(null, 1, 4).IsSuccess);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, log);
        }

        [Test]
        public void MissingLinks_AreReported_NotSkipped()
        {
            var runner = new SaveMigrationRunner(new ISaveMigration[] { new StubMigration(1, 2, new List<int>()) });

            Assert.AreEqual(SaveErrors.MigrationMissing, runner.Upgrade(null, 1, 3).ErrorCode);
        }

        [Test]
        public void AFailingStep_StopsTheChain()
        {
            var log = new List<int>();
            var runner = new SaveMigrationRunner(new ISaveMigration[]
            {
                new StubMigration(1, 2, log) { Fail = true }, new StubMigration(2, 3, log),
            });

            Assert.AreEqual("boom", runner.Upgrade(null, 1, 3).ErrorCode);
            CollectionAssert.AreEqual(new[] { 1 }, log);
        }

        [Test]
        public void AlreadyCurrentDatabases_NeedNoMigration()
        {
            var runner = new SaveMigrationRunner(new ISaveMigration[0]);

            Assert.IsTrue(runner.Upgrade(null, 2, 2).IsSuccess);
        }
    }

    public sealed class SqlPersistenceTests
    {
        private sealed class RecordingConnection : IDbConnection
        {
            public List<string> Commands { get; } = new List<string>();
            public Func<string, object> Scalar { get; set; } = _ => throw new InvalidOperationException("no such table");
            public Func<string, bool> FailOn { get; set; } = _ => false;

            public string ConnectionString { get; set; }
            public int ConnectionTimeout => 0;
            public string Database => "test";
            public ConnectionState State => ConnectionState.Open;
            public IDbTransaction BeginTransaction() => throw new NotSupportedException();
            public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();
            public void ChangeDatabase(string databaseName) { }
            public void Close() { }
            public IDbCommand CreateCommand() => new RecordingCommand(this);
            public void Open() { }
            public void Dispose() { }
        }

        private sealed class RecordingCommand : IDbCommand
        {
            private readonly RecordingConnection _connection;

            public RecordingCommand(RecordingConnection connection)
            {
                _connection = connection;
            }

            public string CommandText { get; set; }
            public int CommandTimeout { get; set; }
            public CommandType CommandType { get; set; }
            public IDbConnection Connection { get; set; }
            public IDataParameterCollection Parameters => null;
            public IDbTransaction Transaction { get; set; }
            public UpdateRowSource UpdatedRowSource { get; set; }
            public void Cancel() { }
            public IDbDataParameter CreateParameter() => null;
            public int ExecuteNonQuery()
            {
                _connection.Commands.Add(CommandText);
                if (_connection.FailOn(CommandText)) throw new InvalidOperationException("sql failure");
                return 0;
            }
            public IDataReader ExecuteReader() => throw new NotSupportedException();
            public IDataReader ExecuteReader(CommandBehavior behavior) => throw new NotSupportedException();
            public object ExecuteScalar() => _connection.Scalar(CommandText);
            public void Prepare() { }
            public void Dispose() { }
        }

        [Test]
        public void UnitOfWork_CommitsSuccessfulWork()
        {
            var connection = new RecordingConnection();
            var unit = new AdoUnitOfWork(connection, NullLogger.Instance);

            var result = unit.ExecuteInTransaction(() =>
            {
                connection.Execute("UPDATE x SET y = 1");
                return Result.Success();
            });

            Assert.IsTrue(result.IsSuccess);
            CollectionAssert.AreEqual(new[] { "BEGIN", "UPDATE x SET y = 1", "COMMIT" }, connection.Commands);
        }

        [Test]
        public void UnitOfWork_RollsBackFailedResults()
        {
            var connection = new RecordingConnection();
            var unit = new AdoUnitOfWork(connection, NullLogger.Instance);

            var result = unit.ExecuteInTransaction(() => Result.Failure("nope", "failed"));

            Assert.IsTrue(result.IsFailure);
            CollectionAssert.AreEqual(new[] { "BEGIN", "ROLLBACK" }, connection.Commands);
        }

        [Test]
        public void UnitOfWork_RollsBackAndRethrowsExceptions()
        {
            var connection = new RecordingConnection();
            var unit = new AdoUnitOfWork(connection, NullLogger.Instance);

            Assert.Throws<InvalidOperationException>(() => unit.ExecuteInTransaction(() => throw new InvalidOperationException("crash")));

            CollectionAssert.AreEqual(new[] { "BEGIN", "ROLLBACK" }, connection.Commands);
        }

        [Test]
        public void SchemaV1_CreatesEveryTableFromTheArchitecture()
        {
            var connection = new RecordingConnection();

            var result = new SaveSchemaV1Migration().Migrate(connection);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(SaveSchemaV1Migration.Statements.Count, connection.Commands.Count);

            var tables = connection.Commands
                .Where(c => c.StartsWith("CREATE TABLE", StringComparison.Ordinal))
                .Select(c => c.Split(' ')[2])
                .ToList();

            foreach (var expected in new[]
            {
                "schema_version", "save_slot", "player_state", "location_progress", "inventory_item", "equipment_progress",
                "job_history", "active_job", "relationship", "gift_history", "quest_progress", "company", "employee",
                "employee_assignment", "company_upgrade", "economy_transaction", "game_setting", "story_flag",
            })
            {
                CollectionAssert.Contains(tables, expected);
            }
        }

        [Test]
        public void SchemaV1_ChildTablesCascadeFromTheSlot()
        {
            var perSaveTables = SaveSchemaV1Migration.Statements
                .Where(s => s.Contains("save_id TEXT") && !s.Contains("CREATE TABLE save_slot"))
                .ToList();

            Assert.IsNotEmpty(perSaveTables);
            Assert.IsTrue(perSaveTables.All(s => s.Contains("REFERENCES save_slot(id) ON DELETE CASCADE")));
        }

        [Test]
        public void Initializer_MigratesAFreshDatabase_InsideATransaction()
        {
            var connection = new RecordingConnection();
            var bus = new EventBus();
            var runner = new SaveMigrationRunner(new ISaveMigration[] { new SaveSchemaV1Migration() });
            using var migrated = bus.Record<SaveMigratedEvent>();

            var result = new SaveDatabaseInitializer(connection, runner, bus, NullLogger.Instance).Initialize();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("BEGIN", connection.Commands[SqlStatements.Pragmas.Count]);
            Assert.AreEqual("COMMIT", connection.Commands.Last());
            Assert.IsTrue(connection.Commands.Any(c => c.StartsWith("INSERT INTO schema_version", StringComparison.Ordinal)));
            Assert.AreEqual(1, migrated.Count);
        }

        [Test]
        public void Initializer_RollsBackWhenAMigrationFails()
        {
            var connection = new RecordingConnection { FailOn = sql => sql.StartsWith("CREATE TABLE player_state", StringComparison.Ordinal) };
            var runner = new SaveMigrationRunner(new ISaveMigration[] { new SaveSchemaV1Migration() });

            var result = new SaveDatabaseInitializer(connection, runner, new EventBus(), NullLogger.Instance).Initialize();

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual("ROLLBACK", connection.Commands.Last());
        }

        [Test]
        public void Initializer_LeavesCurrentDatabasesAlone()
        {
            var connection = new RecordingConnection { Scalar = _ => 1 };
            var runner = new SaveMigrationRunner(new ISaveMigration[] { new SaveSchemaV1Migration() });

            var result = new SaveDatabaseInitializer(connection, runner, new EventBus(), NullLogger.Instance).Initialize();

            Assert.IsTrue(result.IsSuccess);
            CollectionAssert.DoesNotContain(connection.Commands, "BEGIN");
        }

        [Test]
        public void Initializer_RefusesDatabasesFromTheFuture()
        {
            var connection = new RecordingConnection { Scalar = _ => 99 };
            var runner = new SaveMigrationRunner(new ISaveMigration[0]);

            var result = new SaveDatabaseInitializer(connection, runner, new EventBus(), NullLogger.Instance).Initialize();

            Assert.AreEqual("save.schema_too_new", result.ErrorCode);
        }
    }
}
