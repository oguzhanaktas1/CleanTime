using CleanTime.GameTime.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.GameTime.Application
{
    public sealed record ConsumeGameActionCommand(GameActionKind Kind) : ICommand;

    /// <summary>Ends the current day immediately (e.g. the player goes to sleep).</summary>
    public sealed record EndGameDayCommand : ICommand;

    public sealed record GetCurrentGameDateQuery : IQuery<GameDate>;
}
