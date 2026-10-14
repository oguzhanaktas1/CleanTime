using System;
using System.Collections.Generic;
using System.Linq;
using CleanTime.Save.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Save.Infrastructure
{
    /// <summary>Slot storage used until the SQLite provider is wired in (Phase 3).</summary>
    public sealed class InMemorySaveSlotRepository : ISaveSlotRepository
    {
        private readonly Dictionary<SaveId, SaveSlot> _slots = new Dictionary<SaveId, SaveSlot>();
        private readonly InMemorySnapshotStore _snapshots;
        private readonly IUtcClock _clock;

        public InMemorySaveSlotRepository(InMemorySnapshotStore snapshots, IUtcClock clock)
        {
            _snapshots = snapshots;
            _clock = clock;
        }

        public IReadOnlyList<SaveSlotSummary> GetAll() =>
            _slots.Values.OrderBy(s => s.SlotNumber).Select(s => s.ToSummary()).ToList();

        public Result<SaveSlot> Get(SaveId saveId) => _slots.TryGetValue(saveId, out var slot)
            ? Result<SaveSlot>.Success(slot)
            : Result<SaveSlot>.Failure(SaveErrors.SlotNotFound, $"Unknown save slot '{saveId}'.");

        public Result<SaveId> Create(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return Result<SaveId>.Failure(SaveErrors.InvalidName, "A save slot needs a name.");

            var number = 1;
            while (_slots.Values.Any(s => s.SlotNumber == number))
                number++;

            var id = SaveId.New();
            var now = _clock.UtcNow;
            _slots[id] = new SaveSlot(id, number, displayName, SaveSchema.CurrentVersion, now, now);
            return Result<SaveId>.Success(id);
        }

        public Result Update(SaveSlot slot)
        {
            if (!_slots.ContainsKey(slot.Id))
                return Result.Failure(SaveErrors.SlotNotFound, $"Unknown save slot '{slot.Id}'.");

            _slots[slot.Id] = slot;
            return Result.Success();
        }

        public Result Delete(SaveId saveId)
        {
            if (!_slots.Remove(saveId))
                return Result.Failure(SaveErrors.SlotNotFound, $"Unknown save slot '{saveId}'.");

            _snapshots.RemoveSlot(saveId);
            return Result.Success();
        }
    }

    /// <summary>Atomic saves over the in-memory snapshot store: a failure restores the previous contents.</summary>
    public sealed class InMemoryUnitOfWork : IUnitOfWork
    {
        private readonly InMemorySnapshotStore _snapshots;

        public InMemoryUnitOfWork(InMemorySnapshotStore snapshots)
        {
            _snapshots = snapshots;
        }

        public Result ExecuteInTransaction(Func<Result> action)
        {
            _snapshots.BeginTransaction();

            try
            {
                var result = action();

                if (result.IsSuccess)
                    _snapshots.Commit();
                else
                    _snapshots.Rollback();

                return result;
            }
            catch
            {
                _snapshots.Rollback();
                throw;
            }
        }
    }
}
