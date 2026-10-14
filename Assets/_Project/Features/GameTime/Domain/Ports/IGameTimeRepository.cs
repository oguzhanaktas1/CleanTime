using CleanTime.Shared.Kernel;

namespace CleanTime.GameTime.Domain
{
    public interface IGameTimeRepository
    {
        GameCalendar Get();

        Result Save(GameCalendar calendar);
    }
}
