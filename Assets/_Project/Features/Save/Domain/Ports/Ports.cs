using System.Collections.Generic;
using System.Data;
using CleanTime.Shared.Kernel;

namespace CleanTime.Save.Domain
{
    public interface ISaveSlotRepository
    {
        IReadOnlyList<SaveSlotSummary> GetAll();

        Result<SaveSlot> Get(SaveId saveId);

        Result<SaveId> Create(string displayName);

        Result Update(SaveSlot slot);

        /// <summary>Deletes the slot; implementations must cascade to all of the slot's data.</summary>
        Result Delete(SaveId saveId);
    }

    /// <summary>One step of the schema upgrade chain (v1 -> v2 -> v3 ...).</summary>
    public interface ISaveMigration
    {
        int FromVersion { get; }

        int ToVersion { get; }

        Result Migrate(IDbConnection connection);
    }
}
