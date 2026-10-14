using System;
using System.Globalization;
using CleanTime.GameTime.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.GameTime.Infrastructure
{
    public sealed class InMemoryGameTimeRepository : IGameTimeRepository, ISaveParticipant
    {
        private readonly ISnapshotStore _snapshots;
        private GameCalendar _calendar = new GameCalendar();

        public InMemoryGameTimeRepository(ISnapshotStore snapshots)
        {
            _snapshots = snapshots;
        }

        public string Key => "time.calendar";

        public GameCalendar Get() => _calendar;

        public Result Save(GameCalendar calendar)
        {
            _calendar = calendar;
            return Result.Success();
        }

        public Result Flush(SaveId saveId)
        {
            _snapshots.Put(saveId, Key, FormattableString.Invariant($"{_calendar.Now.Day}|{(int)_calendar.Now.Phase}|{_calendar.ActionsInPhase}"));
            return Result.Success();
        }

        public Result Load(SaveId saveId)
        {
            if (!_snapshots.TryGet(saveId, Key, out var payload))
                return Result.Failure("save.missing_section", $"No '{Key}' section in save {saveId}.");

            var parts = payload.Split('|');
            if (parts.Length != 3 ||
                !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var day) || day < 1 ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var phase) ||
                !Enum.IsDefined(typeof(DayPhase), phase) ||
                !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var actions) || actions < 0)
                return Result.Failure("save.invalid_section", $"Invalid '{Key}' payload.");

            _calendar = new GameCalendar(new GameDate(day, (DayPhase)phase), actions);
            return Result.Success();
        }

        public void Reset() => _calendar = new GameCalendar();
    }

    public sealed class GameTimeInfrastructureModule : FeatureModule
    {
        public override string Name => "GameTime.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton(r => new InMemoryGameTimeRepository(r.Resolve<ISnapshotStore>()));
            services.Forward<IGameTimeRepository, InMemoryGameTimeRepository>();
            services.Forward<ISaveParticipant, InMemoryGameTimeRepository>();
        }
    }
}
