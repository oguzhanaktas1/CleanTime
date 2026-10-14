using CleanTime.Shared.Kernel;

namespace CleanTime.Settings.Domain
{
    public interface IGameSettingsRepository
    {
        /// <summary>Never fails: unreadable or missing settings fall back to defaults.</summary>
        GameSettings Load();

        Result Save(GameSettings settings);
    }
}
