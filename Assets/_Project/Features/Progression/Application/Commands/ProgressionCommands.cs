using CleanTime.Shared.Kernel;

namespace CleanTime.Progression.Application
{
    public sealed record UnlockLocationCommand(LocationId LocationId) : ICommand;

    public sealed record UnlockCompanyCommand : ICommand;

    public sealed record ApplyStoryMilestoneCommand(string MilestoneId) : ICommand;

    /// <summary>Re-checks every rule and location requirement. Safe to call at any time.</summary>
    public sealed record EvaluateProgressionCommand : ICommand;
}
