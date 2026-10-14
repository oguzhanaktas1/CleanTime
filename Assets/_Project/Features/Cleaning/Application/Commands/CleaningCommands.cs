using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Application
{
    /// <summary>Authoring data for one target, gathered from the scene when a property is entered.</summary>
    public sealed record CleaningTargetSeed(
        string InstanceId,
        DirtTypeId DirtTypeId,
        float MaxHealth,
        float Weight,
        string RoomId = "");

    public sealed record StartCleaningSessionCommand(
        LocationId LocationId,
        JobId JobId,
        IReadOnlyList<CleaningTargetSeed> Targets) : ICommand;

    public sealed record StartCleaningTargetCommand(string TargetInstanceId, EquipmentId EquipmentId) : ICommand;

    public sealed record ApplyCleaningActionCommand(
        string TargetInstanceId,
        EquipmentId EquipmentId)
        : ICommand;

    public sealed record CancelCleaningCommand : ICommand;

    public sealed record GetPropertyCleaningProgressQuery : IQuery<float>;

    /// <summary>Public read contract for other modules (see architecture §170).</summary>
    public interface ICleaningReadService
    {
        float GetCurrentPropertyProgress();
    }

    /// <summary>Public command contract for other modules (see architecture §170).</summary>
    public interface ICleaningCommandService
    {
        Result ApplyAction(string targetInstanceId, EquipmentId equipmentId);
    }
}
