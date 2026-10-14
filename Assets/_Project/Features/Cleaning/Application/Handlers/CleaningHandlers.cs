using System.Collections.Generic;
using System.Linq;
using CleanTime.Cleaning.Domain;
using CleanTime.Equipment.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Application
{
    internal sealed class StartCleaningSessionHandler : ICommandHandler<StartCleaningSessionCommand>
    {
        private readonly ICleaningTargetRepository _targets;
        private readonly ICleaningSessionRepository _sessions;
        private readonly ICleaningProgressCalculator _progress;
        private readonly IEventBus _events;

        public StartCleaningSessionHandler(
            ICleaningTargetRepository targets,
            ICleaningSessionRepository sessions,
            ICleaningProgressCalculator progress,
            IEventBus events)
        {
            _targets = targets;
            _sessions = sessions;
            _progress = progress;
            _events = events;
        }

        public Result Handle(StartCleaningSessionCommand command)
        {
            // A finished session lingers so its result can be read; only an unfinished one blocks a new start.
            var existing = _sessions.GetCurrent();
            if (existing.IsSuccess && !existing.Value.IsCompleted)
                return Result.Failure(CleaningErrors.SessionAlreadyActive, "A cleaning session is already active.");

            if (command.Targets == null || command.Targets.Count == 0)
                return Result.Failure(CleaningErrors.InvalidTargets, "A property needs at least one cleaning target.");

            var ids = new HashSet<string>();
            var targets = new List<CleaningTarget>(command.Targets.Count);

            foreach (var seed in command.Targets)
            {
                if (string.IsNullOrWhiteSpace(seed.InstanceId) || !ids.Add(seed.InstanceId))
                    return Result.Failure(CleaningErrors.InvalidTargets, $"Missing or duplicate target id '{seed.InstanceId}'.");

                if (seed.MaxHealth <= 0f || seed.Weight <= 0f)
                    return Result.Failure(CleaningErrors.InvalidTargets, $"Target '{seed.InstanceId}' has invalid health or weight.");

                targets.Add(new CleaningTarget(seed.InstanceId, seed.DirtTypeId, seed.MaxHealth, seed.Weight, seed.RoomId));
            }

            _targets.ReplaceAll(targets);
            _sessions.Save(new CleaningSession(command.LocationId, command.JobId, targets.Select(t => t.InstanceId).ToList()));

            _events.Publish(new CleaningSessionStartedEvent(command.LocationId, command.JobId, targets.Count));
            _events.Publish(new PropertyCleaningProgressChangedEvent(command.LocationId, _progress.Calculate(targets)));
            return Result.Success();
        }
    }

    internal sealed class StartCleaningTargetHandler : ICommandHandler<StartCleaningTargetCommand>
    {
        private readonly ICleaningTargetRepository _targets;
        private readonly IEquipmentRepository _equipment;
        private readonly IEquipmentCompatibilityPolicy _compatibility;
        private readonly IEventBus _events;

        public StartCleaningTargetHandler(
            ICleaningTargetRepository targets,
            IEquipmentRepository equipment,
            IEquipmentCompatibilityPolicy compatibility,
            IEventBus events)
        {
            _targets = targets;
            _equipment = equipment;
            _compatibility = compatibility;
            _events = events;
        }

        public Result Handle(StartCleaningTargetCommand command)
        {
            var target = _targets.Get(command.TargetInstanceId);
            if (target.IsFailure)
                return target;

            var equipment = _equipment.GetOwned(command.EquipmentId);
            if (equipment.IsFailure)
                return equipment;

            if (target.Value.IsCompleted)
                return Result.Failure(CleaningErrors.TargetAlreadyCompleted, "Cleaning target is already completed.");

            if (!_compatibility.CanHandle(equipment.Value.Definition, target.Value.TypeId))
                return Result.Failure(CleaningErrors.WrongEquipment, "Selected equipment cannot clean this target.");

            _events.Publish(new CleaningStartedEvent(command.TargetInstanceId, command.EquipmentId));
            return Result.Success();
        }
    }

    internal sealed class ApplyCleaningActionHandler
        : ICommandHandler<ApplyCleaningActionCommand>
    {
        private readonly ICleaningTargetRepository _targets;
        private readonly IEquipmentRepository _equipment;
        private readonly IEquipmentCompatibilityPolicy _compatibility;
        private readonly IEventBus _events;

        public ApplyCleaningActionHandler(
            ICleaningTargetRepository targets,
            IEquipmentRepository equipment,
            IEquipmentCompatibilityPolicy compatibility,
            IEventBus events)
        {
            _targets = targets;
            _equipment = equipment;
            _compatibility = compatibility;
            _events = events;
        }

        public Result Handle(ApplyCleaningActionCommand command)
        {
            var targetResult = _targets.Get(command.TargetInstanceId);
            if (!targetResult.IsSuccess)
                return Result.Failure(
                    CleaningErrors.TargetNotFound,
                    command.TargetInstanceId);

            var equipmentResult = _equipment.GetOwned(command.EquipmentId);
            if (!equipmentResult.IsSuccess)
                return Result.Failure(
                    EquipmentErrors.NotOwned,
                    command.EquipmentId.Value);

            var target = targetResult.Value;
            var equipment = equipmentResult.Value;

            if (!_compatibility.CanHandle(equipment.Definition, target.TypeId))
            {
                _events.Publish(
                    new WrongEquipmentUsedEvent(
                        command.TargetInstanceId,
                        command.EquipmentId,
                        target.TypeId));

                return Result.Failure(
                    CleaningErrors.WrongEquipment,
                    "Selected equipment cannot clean this target.");
            }

            bool wasCompleted = target.IsCompleted;

            var result = target.Apply(equipment.EffectiveCleaningPower);

            if (!result.IsSuccess)
                return result;

            _targets.Save(target);

            _events.Publish(
                new CleaningHitAppliedEvent(
                    target.InstanceId,
                    equipment.Id,
                    target.CurrentHealth,
                    target.MaxHealth));

            if (!wasCompleted && target.IsCompleted)
            {
                _events.Publish(
                    new CleaningTargetCompletedEvent(
                        target.InstanceId,
                        target.TypeId,
                        target.Weight));
            }

            return Result.Success();
        }
    }

    internal sealed class CancelCleaningHandler : ICommandHandler<CancelCleaningCommand>
    {
        private readonly ICleaningTargetRepository _targets;
        private readonly ICleaningSessionRepository _sessions;
        private readonly IEventBus _events;

        public CancelCleaningHandler(ICleaningTargetRepository targets, ICleaningSessionRepository sessions, IEventBus events)
        {
            _targets = targets;
            _sessions = sessions;
            _events = events;
        }

        public Result Handle(CancelCleaningCommand command)
        {
            var session = _sessions.GetCurrent();
            if (session.IsFailure)
                return Result.Failure(CleaningErrors.NoSession, "There is no cleaning session to cancel.");

            var location = session.Value.LocationId;
            _targets.Clear();
            _sessions.Clear();
            _events.Publish(new CleaningCancelledEvent(location));
            return Result.Success();
        }
    }

    internal sealed class GetPropertyCleaningProgressHandler : IQueryHandler<GetPropertyCleaningProgressQuery, float>
    {
        private readonly ICleaningTargetRepository _targets;
        private readonly ICleaningSessionRepository _sessions;
        private readonly ICleaningProgressCalculator _progress;

        public GetPropertyCleaningProgressHandler(
            ICleaningTargetRepository targets,
            ICleaningSessionRepository sessions,
            ICleaningProgressCalculator progress)
        {
            _targets = targets;
            _sessions = sessions;
            _progress = progress;
        }

        public float Handle(GetPropertyCleaningProgressQuery query) =>
            _sessions.GetCurrent().IsSuccess ? _progress.Calculate(_targets.GetAll()) : 0f;
    }

    internal sealed class CleaningFacade : ICleaningReadService, ICleaningCommandService
    {
        private readonly IQueryHandler<GetPropertyCleaningProgressQuery, float> _progress;
        private readonly ICommandHandler<ApplyCleaningActionCommand> _apply;

        public CleaningFacade(
            IQueryHandler<GetPropertyCleaningProgressQuery, float> progress,
            ICommandHandler<ApplyCleaningActionCommand> apply)
        {
            _progress = progress;
            _apply = apply;
        }

        public float GetCurrentPropertyProgress() => _progress.Handle(new GetPropertyCleaningProgressQuery());

        public Result ApplyAction(string targetInstanceId, EquipmentId equipmentId) =>
            _apply.Handle(new ApplyCleaningActionCommand(targetInstanceId, equipmentId));
    }
}
