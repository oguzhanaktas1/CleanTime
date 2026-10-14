using System;
using System.Collections.Generic;
using System.Linq;
using CleanTime.Economy.Application;
using CleanTime.Economy.Domain;
using CleanTime.GameTime.Application;
using CleanTime.Progression.Application;
using CleanTime.Save.Application;
using CleanTime.Shared.Kernel;

namespace CleanTime.DebugTools.Application
{
    /// <summary>A named cheat / inspection command. Only exists in editor and development builds.</summary>
    public interface IDebugCommand
    {
        string Name { get; }

        string Usage { get; }

        Result Execute(string[] args);
    }

    public sealed class DebugCommandRegistry
    {
        private readonly Dictionary<string, IDebugCommand> _commands = new Dictionary<string, IDebugCommand>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<IDebugCommand> Commands => _commands.Values.OrderBy(c => c.Name).ToList();

        public void Add(IDebugCommand command) => _commands[command.Name] = command;

        public Result Execute(string name, string[] args) =>
            _commands.TryGetValue(name, out var command)
                ? command.Execute(args)
                : Result.Failure("debug.unknown_command", $"Unknown debug command '{name}'.");
    }

#if UNITY_EDITOR || DEBUG || CLEANTIME_DEBUG
    internal sealed class AddMoneyCommand : IDebugCommand
    {
        private readonly ICommandHandler<CreditMoneyCommand> _credit;

        public AddMoneyCommand(ICommandHandler<CreditMoneyCommand> credit)
        {
            _credit = credit;
        }

        public string Name => "money.add";

        public string Usage => "money.add <units>";

        public Result Execute(string[] args)
        {
            if (args.Length < 1 || !decimal.TryParse(args[0], System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var units))
                return Result.Failure("debug.usage", Usage);

            return _credit.Handle(new CreditMoneyCommand(Money.FromUnits(units), TransactionReason.DebugGrant, "debug"));
        }
    }

    internal sealed class UnlockLocationDebugCommand : IDebugCommand
    {
        private readonly ICommandHandler<UnlockLocationCommand> _unlock;

        public UnlockLocationDebugCommand(ICommandHandler<UnlockLocationCommand> unlock)
        {
            _unlock = unlock;
        }

        public string Name => "location.unlock";

        public string Usage => "location.unlock <locationId>";

        public Result Execute(string[] args) =>
            args.Length < 1
                ? Result.Failure("debug.usage", Usage)
                : _unlock.Handle(new UnlockLocationCommand(new LocationId(args[0])));
    }

    internal sealed class EndDayDebugCommand : IDebugCommand
    {
        private readonly ICommandHandler<EndGameDayCommand> _endDay;

        public EndDayDebugCommand(ICommandHandler<EndGameDayCommand> endDay)
        {
            _endDay = endDay;
        }

        public string Name => "day.end";

        public string Usage => "day.end";

        public Result Execute(string[] args) => _endDay.Handle(new EndGameDayCommand());
    }

    internal sealed class ForceSaveDebugCommand : IDebugCommand
    {
        private readonly ISaveCoordinator _save;

        public ForceSaveDebugCommand(ISaveCoordinator save)
        {
            _save = save;
        }

        public string Name => "save.force";

        public string Usage => "save.force";

        public Result Execute(string[] args) => _save.SaveCurrentGame();
    }
#endif

    public sealed class DebugToolsModule : FeatureModule
    {
        public override string Name => "DebugTools";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton(r =>
            {
                var registry = new DebugCommandRegistry();
#if UNITY_EDITOR || DEBUG || CLEANTIME_DEBUG
                registry.Add(new AddMoneyCommand(r.Resolve<ICommandHandler<CreditMoneyCommand>>()));
                registry.Add(new UnlockLocationDebugCommand(r.Resolve<ICommandHandler<UnlockLocationCommand>>()));
                registry.Add(new EndDayDebugCommand(r.Resolve<ICommandHandler<EndGameDayCommand>>()));
                registry.Add(new ForceSaveDebugCommand(r.Resolve<ISaveCoordinator>()));
#endif
                return registry;
            });
        }
    }
}
