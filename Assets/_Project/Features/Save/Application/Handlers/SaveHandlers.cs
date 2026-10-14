using System.Collections.Generic;
using CleanTime.Save.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Save.Application
{
    internal sealed class CreateSaveSlotHandler : ICommandHandler<CreateSaveSlotCommand>
    {
        private readonly ISaveCoordinator _coordinator;

        public CreateSaveSlotHandler(ISaveCoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        public Result Handle(CreateSaveSlotCommand command) => _coordinator.StartNewGame(command.DisplayName);
    }

    internal sealed class SaveGameHandler : ICommandHandler<SaveGameCommand>
    {
        private readonly ISaveCoordinator _coordinator;

        public SaveGameHandler(ISaveCoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        public Result Handle(SaveGameCommand command) => _coordinator.SaveCurrentGame();
    }

    internal sealed class LoadSaveHandler : ICommandHandler<LoadSaveCommand>
    {
        private readonly ISaveCoordinator _coordinator;

        public LoadSaveHandler(ISaveCoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        public Result Handle(LoadSaveCommand command) => _coordinator.LoadGame(command.SaveId);
    }

    internal sealed class DeleteSaveSlotHandler : ICommandHandler<DeleteSaveSlotCommand>
    {
        private readonly ISaveSlotRepository _slots;
        private readonly ISaveCoordinator _coordinator;

        public DeleteSaveSlotHandler(ISaveSlotRepository slots, ISaveCoordinator coordinator)
        {
            _slots = slots;
            _coordinator = coordinator;
        }

        public Result Handle(DeleteSaveSlotCommand command)
        {
            if (_coordinator.CurrentSaveId == command.SaveId)
                return Result.Failure(SaveErrors.Failed, "The active save slot cannot be deleted.");

            return _slots.Delete(command.SaveId);
        }
    }

    internal sealed class RenameSaveSlotHandler : ICommandHandler<RenameSaveSlotCommand>
    {
        private readonly ISaveSlotRepository _slots;

        public RenameSaveSlotHandler(ISaveSlotRepository slots)
        {
            _slots = slots;
        }

        public Result Handle(RenameSaveSlotCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.DisplayName))
                return Result.Failure(SaveErrors.InvalidName, "A save slot needs a name.");

            var slot = _slots.Get(command.SaveId);
            if (slot.IsFailure)
                return slot;

            slot.Value.Rename(command.DisplayName);
            return _slots.Update(slot.Value);
        }
    }

    internal sealed class GetSaveSlotsHandler : IQueryHandler<GetSaveSlotsQuery, IReadOnlyList<SaveSlotSummary>>
    {
        private readonly ISaveSlotRepository _slots;

        public GetSaveSlotsHandler(ISaveSlotRepository slots)
        {
            _slots = slots;
        }

        public IReadOnlyList<SaveSlotSummary> Handle(GetSaveSlotsQuery query) => _slots.GetAll();
    }
}
