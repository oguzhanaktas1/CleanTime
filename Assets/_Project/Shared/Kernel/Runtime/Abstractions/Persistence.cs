using System.Collections.Generic;
using System.Linq;

namespace CleanTime.Shared.Kernel
{
    /// <summary>
    /// Implemented by repositories that own part of a save slot. The save coordinator flushes and
    /// loads every registered participant, so Save never needs to know about other modules' tables.
    /// </summary>
    public interface ISaveParticipant
    {
        string Key { get; }

        /// <summary>Persist the in-memory state into the given slot (called inside the save transaction).</summary>
        Result Flush(SaveId saveId);

        /// <summary>Replace the in-memory state with the contents of the given slot.</summary>
        Result Load(SaveId saveId);

        /// <summary>Reset to a fresh-game state.</summary>
        void Reset();
    }

    /// <summary>Key/value payload storage per save slot, used by lightweight (non-SQL) participants.</summary>
    public interface ISnapshotStore
    {
        void Put(SaveId saveId, string key, string payload);

        bool TryGet(SaveId saveId, string key, out string payload);

        void RemoveSlot(SaveId saveId);
    }

    public sealed class InMemorySnapshotStore : ISnapshotStore
    {
        private Dictionary<SaveId, Dictionary<string, string>> _slots =
            new Dictionary<SaveId, Dictionary<string, string>>();

        private Dictionary<SaveId, Dictionary<string, string>> _backup;

        public bool InTransaction => _backup != null;

        public void Put(SaveId saveId, string key, string payload)
        {
            if (!_slots.TryGetValue(saveId, out var slot))
            {
                slot = new Dictionary<string, string>();
                _slots[saveId] = slot;
            }

            slot[key] = payload;
        }

        public bool TryGet(SaveId saveId, string key, out string payload)
        {
            payload = null;
            return _slots.TryGetValue(saveId, out var slot) && slot.TryGetValue(key, out payload);
        }

        public void RemoveSlot(SaveId saveId) => _slots.Remove(saveId);

        /// <summary>Starts buffering writes so they can be undone with <see cref="Rollback"/>.</summary>
        public void BeginTransaction()
        {
            _backup = _slots.ToDictionary(
                pair => pair.Key,
                pair => new Dictionary<string, string>(pair.Value));
        }

        public void Commit() => _backup = null;

        public void Rollback()
        {
            if (_backup == null)
                return;

            _slots = _backup;
            _backup = null;
        }
    }
}
