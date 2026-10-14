using System;
using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Locations.Domain
{
    public sealed class LocationProgress
    {
        public LocationProgress(
            LocationId locationId,
            bool unlocked = false,
            int completionCount = 0,
            float? bestScore = null,
            int? bestCompletionSeconds = null,
            int? lastCompletedDay = null)
        {
            LocationId = locationId;
            Unlocked = unlocked;
            CompletionCount = Guard.NotNegative(completionCount, nameof(completionCount));
            BestScore = bestScore;
            BestCompletionSeconds = bestCompletionSeconds;
            LastCompletedDay = lastCompletedDay;
        }

        public LocationId LocationId { get; }

        public bool Unlocked { get; private set; }

        public int CompletionCount { get; private set; }

        public float? BestScore { get; private set; }

        public int? BestCompletionSeconds { get; private set; }

        public int? LastCompletedDay { get; private set; }

        /// <summary>Returns true when the state changed.</summary>
        public bool Unlock()
        {
            if (Unlocked)
                return false;

            Unlocked = true;
            return true;
        }

        public void RegisterCompletion(float score, float durationSeconds, int gameDay)
        {
            CompletionCount++;
            LastCompletedDay = gameDay;

            if (!BestScore.HasValue || score > BestScore.Value)
                BestScore = score;

            var seconds = (int)Math.Round(durationSeconds);
            if (!BestCompletionSeconds.HasValue || seconds < BestCompletionSeconds.Value)
                BestCompletionSeconds = seconds;
        }
    }

    /// <summary>Read-only view of completion counts used to evaluate unlock requirements.</summary>
    public sealed class ProgressionSnapshot
    {
        private readonly IReadOnlyDictionary<LocationId, int> _completions;

        public ProgressionSnapshot(IReadOnlyDictionary<LocationId, int> completions)
        {
            _completions = completions ?? new Dictionary<LocationId, int>();
        }

        public int GetCompletionCount(LocationId locationId) =>
            _completions.TryGetValue(locationId, out var count) ? count : 0;
    }
}
