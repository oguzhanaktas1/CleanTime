using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Domain
{
    public sealed record CleaningSessionStartedEvent(LocationId LocationId, JobId JobId, int TargetCount) : IDomainEvent;

    public sealed record CleaningCancelledEvent(LocationId LocationId) : IDomainEvent;

    public sealed record CleaningStartedEvent(string TargetInstanceId, EquipmentId EquipmentId) : IDomainEvent;

    public sealed record CleaningHitAppliedEvent(
        string TargetInstanceId,
        EquipmentId EquipmentId,
        float CurrentHealth,
        float MaxHealth) : IDomainEvent;

    public sealed record CleaningTargetCompletedEvent(
        string TargetInstanceId,
        DirtTypeId DirtTypeId,
        float Weight) : IDomainEvent;

    public sealed record WrongEquipmentUsedEvent(
        string TargetInstanceId,
        EquipmentId EquipmentId,
        DirtTypeId DirtTypeId) : IDomainEvent;

    public sealed record RoomCleaningProgressChangedEvent(string RoomId, float Progress) : IDomainEvent;

    public sealed record RoomCompletedEvent(string RoomId) : IDomainEvent;

    public sealed record PropertyCleaningProgressChangedEvent(LocationId LocationId, float Progress) : IDomainEvent;

    public sealed record PropertyCompletedEvent(LocationId LocationId, CleaningResult Result) : IDomainEvent;
}
