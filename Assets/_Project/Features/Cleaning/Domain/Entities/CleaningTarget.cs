using System;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Domain
{
    public static class CleaningErrors
    {
        public const string TargetNotFound = "cleaning.target_not_found";
        public const string TargetAlreadyCompleted = "cleaning.target_already_completed";
        public const string InvalidPower = "cleaning.invalid_power";
        public const string WrongEquipment = "cleaning.wrong_equipment";
        public const string NoSession = "cleaning.no_session";
        public const string SessionAlreadyActive = "cleaning.session_already_active";
        public const string InvalidTargets = "cleaning.invalid_targets";
    }

    /// <summary>One piece of dirt / damage with hit points. Completed when its health reaches zero.</summary>
    public sealed class CleaningTarget
    {
        public CleaningTarget(
            string instanceId,
            DirtTypeId typeId,
            float maxHealth,
            float weight,
            string roomId = "")
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                throw new ArgumentException("Instance id must not be empty.", nameof(instanceId));

            if (maxHealth <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxHealth));

            if (weight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(weight));

            InstanceId = instanceId;
            TypeId = typeId;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            Weight = weight;
            RoomId = roomId ?? string.Empty;
        }

        public string InstanceId { get; }

        public DirtTypeId TypeId { get; }

        public float MaxHealth { get; }

        public float CurrentHealth { get; private set; }

        public float Weight { get; }

        /// <summary>Optional grouping used for per-room progress. Empty means "no room".</summary>
        public string RoomId { get; }

        public bool IsCompleted => CurrentHealth <= 0f;

        /// <summary>Fraction cleaned, 0..1.</summary>
        public float Progress => 1f - CurrentHealth / MaxHealth;

        public Result Apply(float cleaningPower)
        {
            if (IsCompleted)
                return Result.Failure(
                    CleaningErrors.TargetAlreadyCompleted,
                    "Cleaning target is already completed.");

            if (cleaningPower <= 0f)
                return Result.Failure(
                    CleaningErrors.InvalidPower,
                    "Cleaning power must be greater than zero.");

            CurrentHealth = Math.Max(0f, CurrentHealth - cleaningPower);

            return Result.Success();
        }

        /// <summary>Restores persisted health when loading a mid-job save.</summary>
        public void RestoreHealth(float currentHealth)
        {
            CurrentHealth = Math.Max(0f, Math.Min(MaxHealth, currentHealth));
        }
    }
}
