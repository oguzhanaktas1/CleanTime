using CleanTime.Shared.Kernel;

namespace CleanTime.Settings.Domain
{
    public sealed record SettingsChangedEvent(GameSettings Settings) : IDomainEvent;
}
