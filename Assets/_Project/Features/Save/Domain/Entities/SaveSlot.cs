using System;
using CleanTime.Shared.Kernel;

namespace CleanTime.Save.Domain
{
    public static class SaveErrors
    {
        public const string SlotNotFound = "save.slot_not_found";
        public const string SlotCorrupted = "save.slot_corrupted";
        public const string NoActiveSave = "save.no_active_save";
        public const string InvalidName = "save.invalid_name";
        public const string MigrationMissing = "save.migration_missing";
        public const string MissingSection = "save.missing_section";
        public const string InvalidSection = "save.invalid_section";
        public const string Failed = "save.failed";
    }

    public static class SaveSchema
    {
        /// <summary>Highest schema version this build understands. Independent from the app version.</summary>
        public const int CurrentVersion = 1;
    }

    public sealed class SaveSlot
    {
        public SaveSlot(
            SaveId id,
            int slotNumber,
            string displayName,
            int saveVersion,
            DateTime createdAtUtc,
            DateTime updatedAtUtc,
            int playTimeSeconds = 0,
            string lastSceneKey = "",
            bool isCorrupted = false)
        {
            Id = id;
            SlotNumber = slotNumber;
            DisplayName = Guard.NotEmpty(displayName, nameof(displayName)).Trim();
            SaveVersion = saveVersion;
            CreatedAtUtc = createdAtUtc;
            UpdatedAtUtc = updatedAtUtc;
            PlayTimeSeconds = playTimeSeconds;
            LastSceneKey = lastSceneKey ?? string.Empty;
            IsCorrupted = isCorrupted;
        }

        public SaveId Id { get; }

        public int SlotNumber { get; }

        public string DisplayName { get; private set; }

        public int SaveVersion { get; private set; }

        public DateTime CreatedAtUtc { get; }

        public DateTime UpdatedAtUtc { get; private set; }

        public int PlayTimeSeconds { get; private set; }

        public string LastSceneKey { get; private set; }

        public bool IsCorrupted { get; private set; }

        public void Rename(string displayName) => DisplayName = Guard.NotEmpty(displayName, nameof(displayName)).Trim();

        public void Touch(DateTime utcNow, int saveVersion, int addedPlaySeconds = 0, string lastSceneKey = null)
        {
            UpdatedAtUtc = utcNow;
            SaveVersion = saveVersion;
            PlayTimeSeconds += Math.Max(0, addedPlaySeconds);

            if (lastSceneKey != null)
                LastSceneKey = lastSceneKey;
        }

        public void MarkCorrupted() => IsCorrupted = true;

        public SaveSlotSummary ToSummary() =>
            new SaveSlotSummary(Id, SlotNumber, DisplayName, UpdatedAtUtc, PlayTimeSeconds, IsCorrupted);
    }

    /// <summary>Everything the slot picker needs, without loading the slot.</summary>
    public sealed class SaveSlotSummary
    {
        public SaveSlotSummary(SaveId id, int slotNumber, string displayName, DateTime updatedAtUtc, int playTimeSeconds, bool isCorrupted)
        {
            Id = id;
            SlotNumber = slotNumber;
            DisplayName = displayName;
            UpdatedAtUtc = updatedAtUtc;
            PlayTimeSeconds = playTimeSeconds;
            IsCorrupted = isCorrupted;
        }

        public SaveId Id { get; }

        public int SlotNumber { get; }

        public string DisplayName { get; }

        public DateTime UpdatedAtUtc { get; }

        public int PlayTimeSeconds { get; }

        public bool IsCorrupted { get; }
    }
}
