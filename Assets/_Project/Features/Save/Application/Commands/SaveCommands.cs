using System.Collections.Generic;
using CleanTime.Save.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Save.Application
{
    /// <summary>Starts a brand-new game in a new slot.</summary>
    public sealed record CreateSaveSlotCommand(string DisplayName) : ICommand;

    public sealed record SaveGameCommand : ICommand;

    public sealed record LoadSaveCommand(SaveId SaveId) : ICommand;

    public sealed record DeleteSaveSlotCommand(SaveId SaveId) : ICommand;

    public sealed record RenameSaveSlotCommand(SaveId SaveId, string DisplayName) : ICommand;

    public sealed record GetSaveSlotsQuery : IQuery<IReadOnlyList<SaveSlotSummary>>;
}
