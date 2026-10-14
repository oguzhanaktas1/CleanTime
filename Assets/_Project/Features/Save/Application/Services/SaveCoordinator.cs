using System;
using System.Collections.Generic;
using CleanTime.Save.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Save.Application
{
    public interface ISaveCoordinator
    {
        /// <summary>The slot being played, or an empty id when no slot is active.</summary>
        SaveId CurrentSaveId { get; }

        Result SaveCurrentGame();

        Result LoadGame(SaveId saveId);

        /// <summary>Creates a slot, resets every module to fresh-game state and makes the slot current.</summary>
        Result<SaveId> StartNewGame(string displayName);
    }

    internal sealed class SaveCoordinator : ISaveCoordinator
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISaveSlotRepository _slots;
        private readonly IReadOnlyList<ISaveParticipant> _participants;
        private readonly SaveDirtyTracker _dirty;
        private readonly IUtcClock _clock;
        private readonly IEventBus _events;
        private readonly ILogger _logger;

        public SaveCoordinator(
            IUnitOfWork unitOfWork,
            ISaveSlotRepository slots,
            IReadOnlyList<ISaveParticipant> participants,
            SaveDirtyTracker dirty,
            IUtcClock clock,
            IEventBus events,
            ILogger logger)
        {
            _unitOfWork = unitOfWork;
            _slots = slots;
            _participants = participants;
            _dirty = dirty;
            _clock = clock;
            _events = events;
            _logger = logger;
        }

        public SaveId CurrentSaveId { get; private set; }

        public Result SaveCurrentGame()
        {
            if (CurrentSaveId.IsEmpty)
                return Result.Failure(SaveErrors.NoActiveSave, "There is no active save slot.");

            var saveId = CurrentSaveId;
            _events.Publish(new SaveRequestedEvent(saveId));

            Result result;

            try
            {
                result = _unitOfWork.ExecuteInTransaction(() => FlushAll(saveId));
            }
            catch (Exception ex)
            {
                _logger.Error("Save crashed.", ex);
                result = Result.Failure(SaveErrors.Failed, ex.Message);
            }

            if (result.IsFailure)
            {
                _events.Publish(new SaveFailedEvent(saveId, result.ErrorCode, result.Error.HasValue ? result.Error.Value.Message : string.Empty));
                return result;
            }

            _dirty.Clear();
            _events.Publish(new SaveCompletedEvent(saveId));
            return Result.Success();
        }

        public Result LoadGame(SaveId saveId)
        {
            var slot = _slots.Get(saveId);
            if (slot.IsFailure)
                return slot;

            if (slot.Value.IsCorrupted)
                return Result.Failure(SaveErrors.SlotCorrupted, $"Save slot '{slot.Value.DisplayName}' is marked corrupted.");

            foreach (var participant in _participants)
            {
                participant.Reset();
                var loaded = participant.Load(saveId);

                // A section added by a newer build is simply absent from an older save: keep defaults.
                if (loaded.IsFailure && loaded.ErrorCode != SaveErrors.MissingSection)
                {
                    ResetAll();
                    CurrentSaveId = default;
                    _logger.Error($"Loading '{participant.Key}' failed: {loaded.Error}");
                    return loaded;
                }
            }

            CurrentSaveId = saveId;
            _dirty.Clear();
            _events.Publish(new SaveLoadedEvent(saveId));
            return Result.Success();
        }

        public Result<SaveId> StartNewGame(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return Result<SaveId>.Failure(SaveErrors.InvalidName, "A save slot needs a name.");

            var created = _slots.Create(displayName);
            if (created.IsFailure)
                return created;

            ResetAll();
            CurrentSaveId = created.Value;

            var saved = SaveCurrentGame();
            if (saved.IsFailure)
            {
                _slots.Delete(created.Value);
                CurrentSaveId = default;
                return Result<SaveId>.FromFailure(saved);
            }

            return created;
        }

        private Result FlushAll(SaveId saveId)
        {
            foreach (var participant in _participants)
            {
                var flushed = participant.Flush(saveId);
                if (flushed.IsFailure)
                    return flushed;
            }

            var slot = _slots.Get(saveId);
            if (slot.IsFailure)
                return slot;

            slot.Value.Touch(_clock.UtcNow, SaveSchema.CurrentVersion);
            return _slots.Update(slot.Value);
        }

        private void ResetAll()
        {
            foreach (var participant in _participants)
                participant.Reset();
        }
    }
}
