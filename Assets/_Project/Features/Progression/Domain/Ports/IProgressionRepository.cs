using CleanTime.Shared.Kernel;

namespace CleanTime.Progression.Domain
{
    public interface IProgressionRepository
    {
        Result<PlayerProgression> Get();

        Result Save(PlayerProgression progression);
    }
}
