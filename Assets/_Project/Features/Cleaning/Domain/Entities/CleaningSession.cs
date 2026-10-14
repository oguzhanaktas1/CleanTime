using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Domain
{
    /// <summary>Summary of a finished (or in-flight) property cleaning, consumed by job evaluation.</summary>
    public sealed class CleaningResult
    {
        public CleaningResult(float progress, int totalTargets, int completedTargets, int wrongEquipmentAttempts, int hitsApplied)
        {
            Progress = progress;
            TotalTargets = totalTargets;
            CompletedTargets = completedTargets;
            WrongEquipmentAttempts = wrongEquipmentAttempts;
            HitsApplied = hitsApplied;
        }

        /// <summary>Weighted progress, 0..1.</summary>
        public float Progress { get; }

        public int TotalTargets { get; }

        public int CompletedTargets { get; }

        public int WrongEquipmentAttempts { get; }

        public int HitsApplied { get; }
    }

    /// <summary>The cleaning of one property: which targets belong to it and how the player is doing.</summary>
    public sealed class CleaningSession
    {
        public CleaningSession(LocationId locationId, JobId jobId, IReadOnlyList<string> targetIds)
        {
            LocationId = locationId;
            JobId = jobId;
            TargetIds = Guard.NotNull(targetIds, nameof(targetIds));
        }

        public LocationId LocationId { get; }

        /// <summary>Empty for free-roam cleaning outside a job.</summary>
        public JobId JobId { get; }

        public IReadOnlyList<string> TargetIds { get; }

        public int WrongEquipmentAttempts { get; private set; }

        public int HitsApplied { get; private set; }

        public bool IsCompleted { get; private set; }

        public void RegisterMistake() => WrongEquipmentAttempts++;

        public void RegisterHit() => HitsApplied++;

        /// <summary>Returns true only the first time, so completion is announced exactly once.</summary>
        public bool MarkCompleted()
        {
            if (IsCompleted)
                return false;

            IsCompleted = true;
            return true;
        }

        public CleaningResult ToResult(float progress, int completedTargets) =>
            new CleaningResult(progress, TargetIds.Count, completedTargets, WrongEquipmentAttempts, HitsApplied);
    }

    /// <summary>Normalised cleaning progress; presentation decides whether to show "Clean" or "Dirt Remaining".</summary>
    public readonly struct CleaningProgress
    {
        public CleaningProgress(float completed)
        {
            Completed = completed < 0f ? 0f : (completed > 1f ? 1f : completed);
        }

        public float Completed { get; }

        public float Remaining => 1f - Completed;

        public bool IsComplete => Completed >= 1f;

        public int CompletedPercent => (int)System.Math.Round(Completed * 100f);

        public int RemainingPercent => 100 - CompletedPercent;
    }
}
